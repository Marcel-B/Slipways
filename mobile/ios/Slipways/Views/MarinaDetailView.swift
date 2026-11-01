import MapKit
import SlipwaysKit
import SwiftUI

/// A marina with its slipways, details from its website and users, and the nearest gauge; shown as a sheet over the map.
struct MarinaDetailView: View {
    let marina: MarinaRef

    @Environment(\.api) private var api
    @Environment(\.dismiss) private var dismiss
    @State private var details: MarinaDetails?
    @State private var water: WaterDetails?
    @State private var failed = false
    @State private var suggest: SuggestMode?
    @State private var pending: Set<String> = []
    @Environment(\.suggestions) private var store
    @Environment(Favorites.self) private var favorites

    /// Known once the marina has loaded; the sheet opens with only its name.
    private var favorite: Favorite? {
        details.map {
            Favorite(type: .marina, placeId: $0.id, name: $0.name, latitude: $0.latitude, longitude: $0.longitude, source: $0.source)
        }
    }

    var body: some View {
        content
            .navigationTitle(marina.name)
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .confirmationAction) { Button("Fertig") { dismiss() } }
                ToolbarItem(placement: .topBarLeading) { FavoriteButton(favorite: favorite) }
            }
            .task { await load() }
            .sheet(item: $suggest, onDismiss: refreshPending) { mode in
                if let details {
                    SuggestSheet(place: place(details), mode: mode, current: values(details))
                }
            }
    }

    private func place(_ details: MarinaDetails) -> SuggestPlace {
        SuggestPlace(type: .marina, id: details.id, name: details.name, source: details.source)
    }

    private static let fields = ["fee", "operator", "access", "opening_hours", "phone", "email"]

    private func values(_ details: MarinaDetails) -> [String: String] {
        Dictionary(uniqueKeysWithValues: Self.fields.compactMap { field in details.value(field).map { (field, $0.value) } })
    }

    private func refreshPending() {
        pending = store.pendingFields(.marina, marina.id, changed: details?.lastChanged)
    }

    private func sourced(_ label: String, _ systemImage: String, _ field: String, _ details: MarinaDetails,
                         display: @escaping (String) -> String = { $0 }, link: ((String) -> URL?)? = nil) -> SourcedFact {
        SourcedFact(label: label, systemImage: systemImage, value: details.value(field), display: display, link: link,
                    pending: pending.contains(field), onEdit: { suggest = .field(field) })
    }

    @ViewBuilder
    private var content: some View {
        if let details {
            List {
                Section {
                    map(details)
                        .frame(height: 200)
                        .osmAttribution(details.usesOpenStreetMap)
                        .listRowInsets(EdgeInsets())
                }
                facts(details)
                if !details.slipways.isEmpty {
                    Section(Format.count(details.slipways.count, "Slipanlage", "Slipanlagen")) {
                        ForEach(details.slipways) { slipway in
                            NavigationLink(value: slipway) {
                                VStack(alignment: .leading) {
                                    Text(slipway.name)
                                    if let place = Format.place(slipway.postalCode, slipway.city) {
                                        Text(place).font(.subheadline).foregroundStyle(.secondary)
                                    }
                                }
                            }
                        }
                    }
                }
                TipsSection(tips: details.tips)
                ContributeSections(
                    place: place(details), values: values(details), osmElement: details.osmElement, pending: pending,
                    suggest: { suggest = $0 })
                NearestGaugeSection(water: water, latitude: details.latitude, longitude: details.longitude)
                Section {
                    Button("Route planen", systemImage: "car") {
                        MapsApp.open(name: details.name, latitude: details.latitude, longitude: details.longitude, route: true)
                    }
                    Button("In Karten öffnen", systemImage: "map") {
                        MapsApp.open(name: details.name, latitude: details.latitude, longitude: details.longitude)
                    }
                    if let element = details.osmElement, let url = Links.osm(element) {
                        Link(destination: url) { Label("In OpenStreetMap ansehen", systemImage: "globe") }
                    }
                }
            }
        } else if failed {
            LoadFailedView(title: "Marina konnte nicht geladen werden") { await load() }
        } else {
            ProgressView()
        }
    }

    private func map(_ details: MarinaDetails) -> some View {
        let center = CLLocationCoordinate2D(latitude: details.latitude, longitude: details.longitude)
        return DetailMap(region: MKCoordinateRegion(center: center, latitudinalMeters: 1500, longitudinalMeters: 1500)) {
            Marker(details.name, systemImage: "ferry", coordinate: center).tint(.teal)
            ForEach(details.slipways) { slipway in
                Marker(slipway.name, systemImage: "sailboat", coordinate: CLLocationCoordinate2D(latitude: slipway.latitude, longitude: slipway.longitude))
                    .tint(.red)
            }
        }
    }

    @ViewBuilder
    private func facts(_ details: MarinaDetails) -> some View {
        Section {
            if let water = details.water {
                NavigationLink(value: water) {
                    Label { Text(water.name) } icon: { Image(systemName: "water.waves") }
                }
            }
            Fact(label: "Adresse", systemImage: "mappin.and.ellipse", value: details.address)
            sourced("Gebühr", "eurosign.circle", "fee", details, display: Labels.fee)
            sourced("Betreiber", "building.2", "operator", details)
            sourced("Zugang", "door.left.hand.open", "access", details, display: Labels.access)
            sourced("Öffnungszeiten", "clock", "opening_hours", details)
            sourced("Telefon", "phone", "phone", details, link: Links.phone)
            sourced("E-Mail", "envelope", "email", details, link: Links.mail)
            if let text = details.url, let url = Links.web(text) {
                Link(destination: url) { Label(text, systemImage: "link") }
            }
            Fact(label: "Koordinaten", systemImage: "scope", value: String(format: "%.5f, %.5f", details.latitude, details.longitude))
        } footer: {
            if details.usesOpenStreetMap { OSMAttribution() } else { Text("Quelle: eigene Erfassung") }
        }
    }

    private func load() async {
        do {
            let loaded = try await api.marina(id: marina.id)
            details = loaded
            failed = false
            refreshPending()
            if let favorite { favorites.refresh(favorite) }
            if let waterId = loaded.water?.id {
                water = try? await api.water(id: waterId)
            }
        } catch {
            if details == nil { failed = !error.isCancellation }
        }
    }
}

extension View {
    /// The marina sheet as every tab opens it.
    func marinaSheet(_ marina: Binding<MarinaRef?>) -> some View {
        sheet(item: marina) { marina in
            NavigationStack { MarinaDetailView(marina: marina).slipwaysDestinations() }
                .presentationDetents([.medium, .large])
        }
    }
}

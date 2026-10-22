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

    var body: some View {
        content
            .navigationTitle(marina.name)
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .confirmationAction) { Button("Fertig") { dismiss() } }
            }
            .task { await load() }
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
            SourcedFact(label: "Gebühr", systemImage: "eurosign.circle", value: details.value("fee"), display: Labels.fee)
            SourcedFact(label: "Betreiber", systemImage: "building.2", value: details.value("operator"))
            SourcedFact(label: "Zugang", systemImage: "door.left.hand.open", value: details.value("access"), display: Labels.access)
            SourcedFact(label: "Öffnungszeiten", systemImage: "clock", value: details.value("opening_hours"))
            SourcedFact(label: "Telefon", systemImage: "phone", value: details.value("phone"), link: Links.phone)
            SourcedFact(label: "E-Mail", systemImage: "envelope", value: details.value("email"), link: Links.mail)
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

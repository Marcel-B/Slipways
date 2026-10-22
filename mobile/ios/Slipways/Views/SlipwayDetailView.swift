import MapKit
import SlipwaysKit
import SwiftUI

struct SlipwayDetailView: View {
    let slipway: SlipwaySummary

    @Environment(\.api) private var api
    @State private var details: SlipwayDetails?
    @State private var water: WaterDetails?
    @State private var failed = false
    @State private var marina: MarinaRef?
    @State private var suggest: SuggestMode?
    @State private var pending: Set<String> = []
    @Environment(\.suggestions) private var store

    private var coordinate: CLLocationCoordinate2D {
        CLLocationCoordinate2D(latitude: slipway.latitude, longitude: slipway.longitude)
    }

    var body: some View {
        List {
            Section {
                DetailMap(region: MKCoordinateRegion(center: coordinate, latitudinalMeters: 1500, longitudinalMeters: 1500)) {
                    Marker(slipway.name, systemImage: "sailboat", coordinate: coordinate)
                }
                .frame(height: 240)
                .osmAttribution(slipway.source == .osm)
                .listRowInsets(EdgeInsets())
            }

            if let details {
                facts(details)
                if let comment = details.comment {
                    Section("Hinweise") { Text(comment) }
                }
                if details.pro != nil || details.contra != nil {
                    Section("Bewertung") {
                        if let pro = details.pro { Label(pro, systemImage: "hand.thumbsup") }
                        if let contra = details.contra { Label(contra, systemImage: "hand.thumbsdown") }
                    }
                }
                TipsSection(tips: details.tips)
                ContributeSections(
                    place: place(details), values: values(details), ticked: details.ticked, osmElement: details.osmElement,
                    pending: pending, suggest: { suggest = $0 })
            } else if failed {
                Section { Text("Details konnten nicht geladen werden.").foregroundStyle(.secondary) }
            } else {
                Section { ProgressView() }
            }

            NearestGaugeSection(water: water, latitude: slipway.latitude, longitude: slipway.longitude)

            Section {
                Button("Route planen", systemImage: "car") {
                    MapsApp.open(name: slipway.name, latitude: slipway.latitude, longitude: slipway.longitude, route: true)
                }
                Button("In Karten öffnen", systemImage: "map") {
                    MapsApp.open(name: slipway.name, latitude: slipway.latitude, longitude: slipway.longitude)
                }
                if let element = details?.osmElement, let url = Links.osm(element) {
                    Link(destination: url) { Label("In OpenStreetMap ansehen", systemImage: "globe") }
                }
            }
        }
        .navigationTitle(slipway.name)
        .navigationBarTitleDisplayMode(.inline)
        .task { await load() }
        .marinaSheet($marina)
        .sheet(item: $suggest, onDismiss: refreshPending) { mode in
            if let details {
                SuggestSheet(place: place(details), mode: mode, current: values(details))
            }
        }
    }

    private func place(_ details: SlipwayDetails) -> SuggestPlace {
        SuggestPlace(type: .slipway, id: details.id, name: details.name, source: details.source)
    }

    /// What the page shows now, from any source, to fill in the form; facts only as users ticked them.
    private func values(_ details: SlipwayDetails) -> [String: String] {
        let fields = ["fee", "costs", "operator", "access", "opening_hours", "surface"]
        var values = Dictionary(uniqueKeysWithValues: fields.compactMap { field in details.value(field).map { (field, $0.value) } })
        for fact in details.ticked { values[fact.field] = fact.present ? "yes" : "no" }
        return values
    }

    private func refreshPending() {
        pending = store.pendingFields(.slipway, slipway.id, changed: details?.lastChanged)
    }

    @ViewBuilder
    private func facts(_ details: SlipwayDetails) -> some View {
        Section {
            if let water = details.water {
                NavigationLink(value: water) {
                    Label { Text(water.name) } icon: { Image(systemName: "water.waves") }
                }
            }
            LabeledContent {
                VStack(alignment: .trailing, spacing: 2) {
                    Text(details.costsLabel)
                    if let note = (details.value("costs") ?? details.value("fee"))?.note {
                        Text(note).font(.caption).foregroundStyle(.secondary)
                    }
                    if !pending.isDisjoint(with: ["fee", "costs"]) {
                        Text("Dein Vorschlag wird geprüft").font(.caption).foregroundStyle(.orange)
                    }
                }
                EditButton(label: "Kosten") { suggest = .edit(fields: ["fee", "costs"], title: "Kosten korrigieren") }
            } label: {
                Label("Kosten", systemImage: "eurosign.circle")
            }
            Fact(label: "Adresse", systemImage: "mappin.and.ellipse", value: details.address)
            SourcedFact(label: "Betreiber", systemImage: "building.2", value: details.value("operator"), pending: pending.contains("operator"), onEdit: { suggest = .field("operator") })
            SourcedFact(label: "Zugang", systemImage: "door.left.hand.open", value: details.value("access"), display: Labels.access, pending: pending.contains("access"), onEdit: { suggest = .field("access") })
            SourcedFact(label: "Öffnungszeiten", systemImage: "clock", value: details.value("opening_hours"), pending: pending.contains("opening_hours"), onEdit: { suggest = .field("opening_hours") })
            SourcedFact(label: "Untergrund", systemImage: "square.3.layers.3d", value: details.value("surface"), display: Labels.surface, pending: pending.contains("surface"), onEdit: { suggest = .field("surface") })
            if let text = details.url, let url = Links.web(text) {
                Link(destination: url) { Label(text, systemImage: "link") }
            }
            if let summary = details.marina {
                Button { marina = MarinaRef(id: summary.id, name: summary.name) } label: {
                    LabeledContent {
                        Text(summary.name)
                    } label: {
                        Label("Marina", systemImage: "ferry")
                    }
                }
                .foregroundStyle(.primary)
            }
            Fact(label: "Extras", systemImage: "sparkles", value: details.extras.joined(separator: ", "))
            Fact(
                label: "Koordinaten",
                systemImage: "scope",
                value: String(format: "%.5f, %.5f", details.latitude, details.longitude)
            )
        } footer: {
            if details.source == .osm { OSMAttribution() } else { Text("Quelle: eigene Erfassung") }
        }
    }

    private func load() async {
        do {
            let loaded = try await api.slipway(id: slipway.id)
            details = loaded
            refreshPending()
            if let waterId = loaded.water?.id {
                water = try? await api.water(id: waterId)
            }
        } catch {
            failed = !error.isCancellation
        }
    }
}

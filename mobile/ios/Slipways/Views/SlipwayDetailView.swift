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
                ticked(details)
                TipsSection(tips: details.tips)
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
                }
            } label: {
                Label("Kosten", systemImage: "eurosign.circle")
            }
            Fact(label: "Adresse", systemImage: "mappin.and.ellipse", value: details.address)
            SourcedFact(label: "Betreiber", systemImage: "building.2", value: details.value("operator"))
            SourcedFact(label: "Zugang", systemImage: "door.left.hand.open", value: details.value("access"), display: Labels.access)
            SourcedFact(label: "Öffnungszeiten", systemImage: "clock", value: details.value("opening_hours"))
            SourcedFact(label: "Untergrund", systemImage: "square.3.layers.3d", value: details.value("surface"), display: Labels.surface)
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

    /// What users ticked, such as a jetty or a crane.
    @ViewBuilder
    private func ticked(_ details: SlipwayDetails) -> some View {
        let facts = details.ticked
        if let first = facts.first {
            Section {
                ForEach(facts, id: \.field) { fact in
                    Label(fact.label, systemImage: fact.present ? "checkmark" : "xmark")
                        .foregroundStyle(fact.present ? Color.primary : Color.secondary)
                }
            } header: {
                Text("Was gibt es dort?")
            } footer: {
                Text("von Nutzern ergänzt, Stand \(Format.monthYear(first.retrieved))")
            }
        }
    }

    private func load() async {
        do {
            let loaded = try await api.slipway(id: slipway.id)
            details = loaded
            if let waterId = loaded.water?.id {
                water = try? await api.water(id: waterId)
            }
        } catch {
            failed = !error.isCancellation
        }
    }
}

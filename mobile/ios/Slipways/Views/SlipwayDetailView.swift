import MapKit
import SlipwaysKit
import SwiftUI

struct SlipwayDetailView: View {
    let slipway: SlipwaySummary

    @Environment(\.api) private var api
    @State private var details: SlipwayDetails?
    @State private var water: WaterDetails?
    @State private var failed = false

    private var coordinate: CLLocationCoordinate2D {
        CLLocationCoordinate2D(latitude: slipway.latitude, longitude: slipway.longitude)
    }

    var body: some View {
        List {
            Section {
                Map(initialPosition: .region(MKCoordinateRegion(center: coordinate, latitudinalMeters: 1500, longitudinalMeters: 1500))) {
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
            } else if failed {
                Section { Text("Details konnten nicht geladen werden.").foregroundStyle(.secondary) }
            } else {
                Section { ProgressView() }
            }

            nearestGauge

            Section {
                Button("In Karten öffnen", systemImage: "map") {
                    let item = MKMapItem(placemark: MKPlacemark(coordinate: coordinate))
                    item.name = slipway.name
                    item.openInMaps()
                }
                if let element = details?.osmElement, let url = Links.osm(element) {
                    Link(destination: url) { Label("In OpenStreetMap ansehen", systemImage: "globe") }
                }
            }
        }
        .navigationTitle(slipway.name)
        .navigationBarTitleDisplayMode(.inline)
        .task { await load() }
    }

    @ViewBuilder
    private func facts(_ details: SlipwayDetails) -> some View {
        Section {
            if let water = details.water {
                NavigationLink(value: water) {
                    Label { Text(water.name) } icon: { Image(systemName: "water.waves") }
                }
            }
            Fact(label: "Kosten", systemImage: "eurosign.circle", value: details.costsLabel)
            Fact(label: "Adresse", systemImage: "mappin.and.ellipse", value: details.address)
            Fact(label: "Betreiber", systemImage: "building.2", value: details.operator)
            Fact(label: "Zugang", systemImage: "door.left.hand.open", value: details.access.map(Labels.access))
            Fact(label: "Öffnungszeiten", systemImage: "clock", value: details.openingHours)
            Fact(label: "Untergrund", systemImage: "square.3.layers.3d", value: details.surface.map(Labels.surface))
            if let text = details.url, let url = Links.web(text) {
                Link(destination: url) { Label(text, systemImage: "link") }
            }
            if let marina = details.marina {
                if let text = marina.url, let url = Links.web(text) {
                    Link(destination: url) { Label(marina.name, systemImage: "ferry") }
                } else {
                    Fact(label: "Marina", systemImage: "ferry", value: marina.name)
                }
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

    /// The gauge with a current reading closest to the slipway, from the water the slipway is on.
    @ViewBuilder
    private var nearestGauge: some View {
        if let water, let nearest = water.nearestGauge(latitude: slipway.latitude, longitude: slipway.longitude) {
            Section("Nächster Pegel") {
                NavigationLink(value: Water(id: water.id, name: water.name)) {
                    VStack(alignment: .leading, spacing: 2) {
                        Text(nearest.station.name)
                        WaterLevelText(level: nearest.level).font(.headline)
                        Text("\(Format.kilometres(nearest.km)) entfernt · gemessen \(nearest.level.measuredAt.formatted(date: .numeric, time: .shortened))")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                }
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

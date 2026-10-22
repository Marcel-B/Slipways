import MapKit
import SlipwaysKit
import SwiftUI

struct WaterDetailView: View {
    let water: Water

    @Environment(\.api) private var api
    @State private var details: WaterDetails?
    @State private var failed = false
    @State private var marina: MarinaRef?

    var body: some View {
        content
            .navigationTitle(water.name)
            .navigationBarTitleDisplayMode(.inline)
            .task { await load() }
            .refreshable { await load() }
            .marinaSheet($marina)
    }

    @ViewBuilder
    private var content: some View {
        if let details {
            List {
                if let region = region(details.bounds) {
                    Section {
                        WaterMap(details: details, region: region)
                            .frame(height: 280)
                            .listRowInsets(EdgeInsets())
                    }
                }

                if !details.stations.isEmpty {
                    Section(Format.count(details.stations.count, "Pegel", "Pegel")) {
                        ForEach(details.stations) { station in
                            LabeledContent {
                                if let level = station.waterLevel {
                                    WaterLevelText(level: level)
                                } else {
                                    Text("kein Messwert").foregroundStyle(.secondary)
                                }
                            } label: {
                                Text(station.name)
                                if let level = station.waterLevel {
                                    Text("gemessen \(level.measuredAt.formatted(date: .numeric, time: .shortened))")
                                }
                            }
                        }
                    }
                }

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

                if !details.marinas.isEmpty {
                    Section(Format.count(details.marinas.count, "Marina", "Marinas")) {
                        ForEach(details.marinas) { marina in
                            Button { self.marina = MarinaRef(id: marina.id, name: marina.name) } label: { MarinaRow(marina: marina) }
                                .foregroundStyle(.primary)
                        }
                    }
                }

                if details.usesOpenStreetMap {
                    Section { } footer: { OSMAttribution() }
                }
            }
        } else if failed {
            LoadFailedView(title: "Gewässer konnte nicht geladen werden") { await load() }
        } else {
            ProgressView()
        }
    }

    /// Fits the map to the water with a little margin.
    private func region(_ bounds: Bounds?) -> MKCoordinateRegion? {
        guard let bounds else { return nil }
        let center = CLLocationCoordinate2D(latitude: (bounds.south + bounds.north) / 2, longitude: (bounds.west + bounds.east) / 2)
        let span = MKCoordinateSpan(
            latitudeDelta: max((bounds.north - bounds.south) * 1.2, 0.01),
            longitudeDelta: max((bounds.east - bounds.west) * 1.2, 0.01)
        )
        return MKCoordinateRegion(center: center, span: span)
    }

    private func load() async {
        do {
            details = try await api.water(id: water.id)
            failed = false
        } catch {
            if details == nil { failed = !error.isCancellation }
        }
    }
}

private struct WaterMap: View {
    let details: WaterDetails
    let region: MKCoordinateRegion

    var body: some View {
        DetailMap(region: region) {
            if let shape = details.shape {
                ForEach(shape.polygons.indices, id: \.self) { index in
                    if let outer = shape.polygons[index].first {
                        MapPolygon(coordinates: outer.map { CLLocationCoordinate2D($0) })
                            .foregroundStyle(.blue.opacity(0.2))
                            .stroke(.blue, lineWidth: 2)
                    }
                }
                ForEach(shape.lines.indices, id: \.self) { index in
                    MapPolyline(coordinates: shape.lines[index].map { CLLocationCoordinate2D($0) })
                        .stroke(.blue, lineWidth: 3)
                }
            }
            ForEach(details.slipways) { slipway in
                Marker(slipway.name, systemImage: "sailboat", coordinate: CLLocationCoordinate2D(latitude: slipway.latitude, longitude: slipway.longitude))
                    .tint(.red)
            }
            ForEach(details.stations) { station in
                Marker(station.name, systemImage: "ruler", coordinate: CLLocationCoordinate2D(latitude: station.latitude, longitude: station.longitude))
                    .tint(.orange)
            }
            ForEach(details.marinas) { marina in
                Marker(marina.name, systemImage: "ferry", coordinate: CLLocationCoordinate2D(latitude: marina.latitude, longitude: marina.longitude))
                    .tint(.teal)
            }
        }
        .osmAttribution(details.usesOpenStreetMap)
    }
}

private struct MarinaRow: View {
    let marina: Marina

    var body: some View {
        VStack(alignment: .leading) {
            Text(marina.name)
            if let place = Format.place(marina.postalCode, marina.city) {
                Text(place).font(.subheadline).foregroundStyle(.secondary)
            }
        }
    }
}

import MapKit
import SlipwaysKit
import SwiftUI

/// Waters on a map as their courses and areas, with search and choice of country; the list below shows the waters in
/// the visible area or the search hits. Thousands of shapes are too much to load at once, so the map loads those in
/// its view, further out only the large ones, or those of the search hits.
struct WaterOverviewView: View {
    @Environment(\.api) private var api

    @State private var text = ""
    /// The search term once typing has paused.
    @State private var query = ""
    @State private var waters: [WaterSummary]?
    @State private var results: [WaterSummary]?
    @State private var error: Error?
    @State private var countries: [CountryCount] = []
    @State private var country: String?
    @State private var shapes: [WaterShape] = []
    @State private var selectedId: String?
    @State private var area = MapArea.start
    @State private var mapWidth = 390.0
    @State private var position = MapCameraPosition.region(MKCoordinateRegion(MapArea.start))

    private var searching: Bool { !query.isEmpty }

    private var candidates: [WaterSummary] {
        let all = searching ? results ?? [] : waters ?? []
        guard let country else { return all }
        return all.filter { $0.countries.contains(country) }
    }

    /// Nil when the area holds too many waters to list.
    private var listed: [WaterSummary]? {
        if searching { return candidates }
        return Overview.visible(candidates.filter { $0.position != nil }, in: area) { ($0.position!.latitude, $0.position!.longitude) }
    }

    private var shapeFilter: ShapeFilter {
        searching ? .search(query) : .area(ShapeArea(area, zoom: area.zoom(width: mapWidth)))
    }

    var body: some View {
        VStack(spacing: 0) {
            map
            list
        }
        .navigationTitle("Gewässer")
        .navigationBarTitleDisplayMode(.inline)
        .searchable(text: $text, placement: .navigationBarDrawer(displayMode: .always), prompt: "Gewässer suchen")
        .toolbar {
            ToolbarItem(placement: .topBarTrailing) { CountryMenu(options: countries, selection: $country) }
        }
        .task { await reload() }
        .task(id: text) { await find() }
        .task(id: shapeFilter) { await loadShapes(shapeFilter) }
        .onChange(of: country) { _, _ in focus() }
    }

    private var map: some View {
        let shown = Set(candidates.map(\.id))
        // Shapes and list are fetched separately; draw only what the list shows, too.
        let drawn = shapes.filter { shown.contains($0.id) }
        return MapReader { proxy in
            Map(position: $position) {
                ForEach(drawn) { shape in
                    let color: Color = shape.id == selectedId ? .orange : .blue
                    ForEach(shape.geometry.polygons.indices, id: \.self) { index in
                        if let outer = shape.geometry.polygons[index].first {
                            MapPolygon(coordinates: outer.map { CLLocationCoordinate2D($0) })
                                .foregroundStyle(color.opacity(0.2))
                                .stroke(color, lineWidth: 1.5)
                        }
                    }
                    ForEach(shape.geometry.lines.indices, id: \.self) { index in
                        MapPolyline(coordinates: shape.geometry.lines[index].map { CLLocationCoordinate2D($0) })
                            .stroke(color, lineWidth: shape.id == selectedId ? 4 : 2.5)
                    }
                }
            }
            .onTapGesture { location in
                guard let coordinate = proxy.convert(location, from: .local) else { return }
                // About a fingertip around the tap.
                let tolerance = area.latitudeDelta / max(mapWidth, 1) * 22
                selectedId = WaterShape.hit(drawn, latitude: coordinate.latitude, longitude: coordinate.longitude, tolerance: tolerance)?.id
            }
        }
        .onMapCameraChange(frequency: .onEnd) { context in
            area = MapArea(context.region)
        }
        .onGeometryChange(for: Double.self) { $0.size.width } action: { mapWidth = $0 }
        .overlay(alignment: .bottom) { selectedCard }
        .osmAttribution(true)
        .frame(maxHeight: .infinity)
    }

    /// The water whose shape was tapped, as the website's popup.
    @ViewBuilder
    private var selectedCard: some View {
        if let id = selectedId, let water = candidates.first(where: { $0.id == id }) {
            NavigationLink(value: water.water) {
                HStack {
                    VStack(alignment: .leading, spacing: 2) {
                        Text(water.name).font(.headline)
                        Text(contents(water)).font(.subheadline).foregroundStyle(.secondary)
                    }
                    Spacer()
                    Image(systemName: "chevron.right").foregroundStyle(.secondary)
                }
                .padding(12)
                .background(.regularMaterial, in: .rect(cornerRadius: 12))
            }
            .buttonStyle(.plain)
            .padding(.horizontal, 12)
            .padding(.bottom, 32)
        }
    }

    @ViewBuilder
    private var list: some View {
        if waters == nil && error != nil {
            LoadFailedView(title: "Gewässer konnten nicht geladen werden") { await reload() }
                .frame(maxHeight: .infinity)
        } else if waters == nil || (searching && results == nil) {
            ProgressView().frame(maxHeight: .infinity)
        } else if let listed {
            if listed.isEmpty {
                Group {
                    if searching {
                        ContentUnavailableView.search(text: query)
                    } else {
                        ContentUnavailableView("Hier liegt kein Gewässer", systemImage: "map", description: Text("Verschiebe die Karte oder suche."))
                    }
                }
                .frame(maxHeight: .infinity)
            } else {
                List {
                    Section(searching ? Format.count(listed.count, "Treffer", "Treffer") : "\(Format.count(listed.count, "Gewässer", "Gewässer")) im Kartenausschnitt") {
                        ForEach(listed) { water in
                            NavigationLink(value: water.water) { WaterRow(water: water, contents: contents(water)) }
                        }
                    }
                    Section { } footer: { OSMAttribution() }
                }
                .listStyle(.plain)
                .frame(maxHeight: .infinity)
            }
        } else {
            ContentUnavailableView(
                "Zoome hinein oder suche",
                systemImage: "magnifyingglass",
                description: Text("\(Format.count(candidates.count, "Gewässer", "Gewässer")), zu viele für eine Liste.")
            )
            .frame(maxHeight: .infinity)
        }
    }

    private func contents(_ water: WaterSummary) -> String {
        let parts = [
            water.slipwayCount > 0 ? Format.count(water.slipwayCount, "Slipanlage", "Slipanlagen") : nil,
            water.stationCount > 0 ? Format.count(water.stationCount, "Pegel", "Pegel") : nil,
        ].compactMap { $0 }
        return parts.isEmpty ? "Keine Slipanlagen oder Pegel" : parts.joined(separator: " · ")
    }

    /// A search moves the map to its hits, a chosen country to that country.
    private func focus() {
        let points = candidates.compactMap(\.position).map { (latitude: $0.latitude, longitude: $0.longitude) }
        let target = searching ? MapArea.fitting(points) : (country == nil ? MapArea.start : Countries.coreArea(points))
        if let target { withAnimation { position = .region(MKCoordinateRegion(target)) } }
    }

    private func reload() async {
        do {
            let loaded = try await api.waters()
            waters = loaded
            countries = Countries.counts(loaded.map(\.countries))
            error = nil
        } catch let error where error.isCancellation {
        } catch {
            self.error = error
        }
    }

    private func find() async {
        let term = text.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !term.isEmpty else {
            query = ""
            results = nil
            return
        }
        try? await Task.sleep(for: .milliseconds(350))
        if Task.isCancelled || term == query { return }
        do {
            let found = try await api.waters(search: term)
            if Task.isCancelled { return }
            query = term
            results = found
            focus()
        } catch let error where error.isCancellation {
        } catch {
            query = term
            results = []
        }
    }

    private func loadShapes(_ filter: ShapeFilter) async {
        do {
            // Old shapes stay until the new ones arrive, so nothing flickers.
            shapes = switch filter {
            case .search(let term): try await api.waterShapes(search: term)
            case .area(let area): try await api.waterShapes(in: area)
            }
        } catch {
            // Without shapes the list still works.
        }
    }
}

private enum ShapeFilter: Hashable {
    case search(String)
    case area(ShapeArea)
}

private struct WaterRow: View {
    let water: WaterSummary
    let contents: String

    var body: some View {
        VStack(alignment: .leading, spacing: 2) {
            Text(water.name)
            Text(contents).font(.subheadline).foregroundStyle(.secondary)
            if let gauge = water.gauge {
                HStack(spacing: 4) {
                    Image(systemName: "ruler")
                    Text("Pegel \(gauge.stationName):")
                    WaterLevelText(level: gauge.waterLevel)
                }
                .font(.subheadline)
            }
        }
    }
}

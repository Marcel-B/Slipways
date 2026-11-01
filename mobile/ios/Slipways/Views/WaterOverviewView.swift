import MapKit
import SlipwaysKit
import SwiftUI

/// Waters on a full-screen map as their courses and areas, with a search field and a choice of country floating above
/// it; there is no list. Thousands of shapes are too much to load at once, so the map loads those in its view, further
/// out only the large ones, or those of the search hits, which also get a pin each. A tap on a course or pin shows the
/// water. The map moves by itself only for a settled search term or a chosen country, as `PlaceOverview` does.
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
    @State private var area = LaunchState.area
    @State private var mapWidth = 390.0
    @State private var position = MapCameraPosition.region(MKCoordinateRegion(LaunchState.area))
    /// The water the screenshot test asked to open (`LaunchState.open`).
    @State private var opened: Water?
    @State private var launchOpened = false
    @FocusState private var searchFocused: Bool

    private var searching: Bool { !query.isEmpty }

    private var candidates: [WaterSummary] {
        let all = searching ? results ?? [] : waters ?? []
        guard let country else { return all }
        return all.filter { $0.countries.contains(country) }
    }

    /// Where the hits lie; waters with nothing on them that has coordinates have no point.
    private var points: [(latitude: Double, longitude: Double)] {
        candidates.compactMap(\.position).map { (latitude: $0.latitude, longitude: $0.longitude) }
    }

    /// Search hits with a point get a pin.
    private var pinned: [WaterSummary] {
        searching ? candidates.filter { $0.position != nil } : []
    }

    private var selected: WaterSummary? {
        selectedId.flatMap { id in candidates.first { $0.id == id } }
    }

    private var shapeFilter: ShapeFilter {
        searching ? .search(query) : .area(ShapeArea(area, zoom: area.zoom(width: mapWidth)))
    }

    var body: some View {
        ZStack {
            map.ignoresSafeArea(edges: [.top, .leading, .trailing])
        }
        .osmAttribution(true)
        .overlay(alignment: .top) {
            MapSearchBar(text: $text, prompt: "Gewässer suchen", countries: countries, country: $country, focused: $searchFocused)
        }
        .overlay(alignment: .bottom) {
            if selected != nil { selectedCard } else { notice }
        }
        .navigationTitle("Gewässer")
        .toolbar(.hidden, for: .navigationBar)
        .task { await reload() }
        .task(id: text) { await find() }
        .task(id: shapeFilter) { await loadShapes(shapeFilter) }
        .onChange(of: country) { _, _ in focus() }
        .navigationDestination(item: $opened) { WaterDetailView(water: $0) }
    }

    private var map: some View {
        let shown = Set(candidates.map(\.id))
        // Shapes and waters are fetched separately; draw only the waters searched for or in the chosen country. Only the
        // parts in and around the view, at about a point's precision: MapKit would otherwise draw whole rivers at street
        // level and run out of memory.
        let clip = area.expanded(by: 0.5)
        let resolution = area.longitudeDelta / max(mapWidth, 1)
        let drawn = shapes
            .filter { shown.contains($0.id) }
            .map { $0.clipped(to: clip, resolution: resolution) }
            .filter { !$0.geometry.isEmpty }
        return MapReader { proxy in
            // North stays up: a compass would sit under the search field.
            Map(position: $position, interactionModes: [.pan, .zoom]) {
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
                // Without a list, search hits need something to tap even where their course is small or missing.
                // The pins only show; the tap below finds them, so it cannot compete with buttons on the map.
                ForEach(pinned) { water in
                    Annotation("", coordinate: CLLocationCoordinate2D(water.position!), anchor: .center) {
                        Image(systemName: "drop.circle.fill")
                            .font(.title2)
                            .foregroundStyle(.white, water.id == selectedId ? Color.orange : Color.blue)
                            .shadow(radius: 2)
                            .allowsHitTesting(false)
                            .accessibilityLabel(water.name)
                    }
                    .annotationTitles(.hidden)
                }
            }
            .onTapGesture { location in
                guard let coordinate = proxy.convert(location, from: .local) else { return }
                // About a fingertip around the tap.
                let tolerance = area.latitudeDelta / max(mapWidth, 1) * 22
                searchFocused = false
                // A pin is a fingertip wide; in degrees of longitude that follows the map width, in latitude the
                // Mercator scale at the tap.
                let pinWidth = area.longitudeDelta / max(mapWidth, 1) * 22
                let pinHeight = pinWidth * cos(coordinate.latitude * .pi / 180)
                let pin = pinned.first { water in
                    guard let position = water.position else { return false }
                    return abs(position.longitude - coordinate.longitude) < pinWidth
                        && abs(position.latitude - coordinate.latitude) < pinHeight
                }
                selectedId = pin?.id
                    ?? WaterShape.hit(drawn, latitude: coordinate.latitude, longitude: coordinate.longitude, tolerance: tolerance)?.id
            }
        }
        .onMapCameraChange(frequency: .onEnd) { context in
            area = MapArea(context.region)
        }
        .onGeometryChange(for: Double.self) { $0.size.width } action: { mapWidth = $0 }
        // Moving the map puts the keyboard away, so the whole screen is map again.
        .simultaneousGesture(DragGesture(minimumDistance: 10).onChanged { _ in searchFocused = false })
    }

    /// The water whose shape was tapped, as the website's popup.
    @ViewBuilder
    private var selectedCard: some View {
        if let water = selected {
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

    /// Loading, a failure, or how many waters the search found; nothing while the map simply shows everything.
    @ViewBuilder
    private var notice: some View {
        let term = text.trimmingCharacters(in: .whitespacesAndNewlines)
        if waters == nil, error != nil {
            MapNotice(text: "Gewässer konnten nicht geladen werden", systemImage: "wifi.exclamationmark", actionTitle: "Erneut versuchen") {
                Task { await reload() }
            }
        } else if waters == nil {
            MapNotice(text: "Gewässer werden geladen", loading: true)
        } else if !term.isEmpty && term != query {
            MapNotice(text: "Suche „\(term)“", loading: true)
        } else if searching {
            if candidates.isEmpty {
                MapNotice(text: country.map { "Keine Treffer in \(Countries.name($0))" } ?? "Keine Treffer", systemImage: "magnifyingglass")
            } else if Overview.anyOutside(points, of: area) {
                MapNotice(text: Format.count(candidates.count, "Treffer", "Treffer"), actionTitle: "Alle zeigen") { focus() }
            } else {
                MapNotice(text: Format.count(candidates.count, "Treffer", "Treffer"))
            }
        }
    }

    private func contents(_ water: WaterSummary) -> String {
        let parts = [
            water.slipwayCount > 0 ? Format.count(water.slipwayCount, "Slipanlage", "Slipanlagen") : nil,
            water.stationCount > 0 ? Format.count(water.stationCount, "Pegel", "Pegel") : nil,
        ].compactMap { $0 }
        return parts.isEmpty ? "Keine Slipanlagen oder Pegel" : parts.joined(separator: " · ")
    }

    /// Fits the map to the search hits or the chosen country (`Overview.focus`).
    private func focus() {
        guard let target = Overview.focus(points, searching: searching, country: country) else { return }
        withAnimation { position = .region(MKCoordinateRegion(target)) }
    }

    private func reload() async {
        do {
            let loaded = try await api.waters()
            waters = loaded
            countries = Countries.counts(loaded.map(\.countries))
            error = nil
            if !launchOpened {
                launchOpened = true
                selectedId = LaunchState.select
                opened = LaunchState.open.flatMap { id in loaded.first { $0.id == id } }?.water
            }
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

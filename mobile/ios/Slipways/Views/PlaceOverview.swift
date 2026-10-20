import MapKit
import SlipwaysKit
import SwiftUI

/// Map with search for one kind of place; the list below shows only what lies in the visible area or matches the search.
/// Crowded areas show bubbles with counts instead of pins, which keeps the map fast.
struct PlaceOverview<Item: Place, Row: View>: View {
    let title: String
    let singular: String
    let plural: String
    let systemImage: String
    let tint: Color
    let load: @MainActor () async throws -> [Item]
    /// Gets the search term and everything loaded, for places that are searched locally.
    let search: @MainActor (String, [Item]) async throws -> [Item]
    let onSelect: (Item) -> Void
    @ViewBuilder let row: (Item) -> Row

    /// More pins than this in the visible area are grouped into bubbles.
    private static var pinLimit: Int { 100 }

    @State private var text = ""
    /// The search term once typing has paused; everything shown follows this, not every keystroke.
    @State private var query = ""
    @State private var items: [Item]?
    @State private var results: [Item]?
    @State private var error: Error?
    @State private var area = MapArea.start
    @State private var position = MapCameraPosition.region(MKCoordinateRegion(MapArea.start))
    @State private var selection: String?

    private var searching: Bool { !query.isEmpty }

    /// What the map may show: the search results or everything.
    private var candidates: [Item] { searching ? results ?? [] : items ?? [] }

    /// Nil when the area holds too many entries to list.
    private var listed: [Item]? {
        if searching { return results ?? [] }
        return Overview.visible(items ?? [], in: area) { ($0.latitude, $0.longitude) }
    }

    var body: some View {
        VStack(spacing: 0) {
            map
            list
        }
        .navigationTitle(title)
        .navigationBarTitleDisplayMode(.inline)
        .searchable(text: $text, placement: .navigationBarDrawer(displayMode: .always), prompt: "Name, Ort oder Gewässer")
        .task { await reload() }
        .task(id: text) { await find() }
        .onChange(of: selection) { _, id in
            guard let id else { return }
            selection = nil
            if let item = candidates.first(where: { $0.id == id }) { onSelect(item) }
        }
    }

    private var map: some View {
        let pins = Overview.visible(candidates, in: area, limit: Self.pinLimit) { ($0.latitude, $0.longitude) }
        let clusters = pins == nil ? Overview.clusters(candidates, in: area) { ($0.latitude, $0.longitude) } : []
        return Map(position: $position, selection: $selection) {
            ForEach(pins ?? []) { item in
                Marker(item.name, systemImage: systemImage, coordinate: CLLocationCoordinate2D(latitude: item.latitude, longitude: item.longitude))
                    .tint(tint)
                    .tag(item.id)
            }
            ForEach(clusters) { cluster in
                Annotation("", coordinate: CLLocationCoordinate2D(latitude: cluster.latitude, longitude: cluster.longitude), anchor: .center) {
                    Button { zoom(to: cluster) } label: { ClusterBubble(count: cluster.items.count, tint: tint) }
                        .buttonStyle(.plain)
                        .accessibilityLabel(Format.count(cluster.items.count, singular, plural))
                }
                .annotationTitles(.hidden)
            }
        }
        .onMapCameraChange(frequency: .onEnd) { context in
            area = MapArea(context.region)
        }
        .osmAttribution(true)
        .frame(maxHeight: .infinity)
    }

    @ViewBuilder
    private var list: some View {
        if items == nil && error != nil {
            LoadFailedView(title: "\(plural) konnten nicht geladen werden") { await reload() }
                .frame(maxHeight: .infinity)
        } else if items == nil || (searching && results == nil) {
            ProgressView().frame(maxHeight: .infinity)
        } else if let listed {
            if listed.isEmpty {
                Group {
                    if searching {
                        ContentUnavailableView.search(text: query)
                    } else {
                        ContentUnavailableView("Hier gibt es keine \(plural)", systemImage: "map", description: Text("Verschiebe die Karte oder suche."))
                    }
                }
                .frame(maxHeight: .infinity)
            } else {
                List {
                    Section(Format.count(listed.count, singular, plural)) {
                        ForEach(listed) { row($0) }
                    }
                    Section { } footer: { OSMAttribution() }
                }
                .listStyle(.plain)
                .frame(maxHeight: .infinity)
            }
        } else {
            ContentUnavailableView("Zoome hinein oder suche", systemImage: "magnifyingglass", description: Text("Im Kartenausschnitt liegen zu viele \(plural) für eine Liste."))
                .frame(maxHeight: .infinity)
        }
    }

    private func zoom(to cluster: Cluster<Item>) {
        guard let fitted = MapArea.fitting(cluster.items.map { (latitude: $0.latitude, longitude: $0.longitude) }) else { return }
        withAnimation { position = .region(MKCoordinateRegion(fitted)) }
    }

    private func reload() async {
        do {
            items = try await load()
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
        // Debounce typing; a newer keystroke cancels this task. Old results stay until new ones arrive.
        try? await Task.sleep(for: .milliseconds(350))
        if Task.isCancelled || term == query { return }
        do {
            let found = try await search(term, items ?? [])
            if Task.isCancelled { return }
            query = term
            results = found
            if let fitted = MapArea.fitting(found.map { (latitude: $0.latitude, longitude: $0.longitude) }) {
                withAnimation { position = .region(MKCoordinateRegion(fitted)) }
            }
        } catch let error where error.isCancellation {
        } catch {
            query = term
            results = []
        }
    }
}

private struct ClusterBubble: View {
    let count: Int
    let tint: Color

    var body: some View {
        Text(count, format: .number)
            .font(.caption.bold())
            .foregroundStyle(.white)
            .padding(6)
            .frame(minWidth: 34, minHeight: 34)
            .background(tint, in: .circle)
            .overlay(Circle().stroke(.white, lineWidth: 2))
            .shadow(radius: 2)
    }
}

extension MKCoordinateRegion {
    init(_ area: MapArea) {
        self.init(
            center: CLLocationCoordinate2D(latitude: area.centerLatitude, longitude: area.centerLongitude),
            span: MKCoordinateSpan(latitudeDelta: area.latitudeDelta, longitudeDelta: area.longitudeDelta)
        )
    }
}

extension MapArea {
    init(_ region: MKCoordinateRegion) {
        self.init(
            centerLatitude: region.center.latitude,
            centerLongitude: region.center.longitude,
            latitudeDelta: region.span.latitudeDelta,
            longitudeDelta: region.span.longitudeDelta
        )
    }
}

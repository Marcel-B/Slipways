import MapKit
import SlipwaysKit
import SwiftUI

/// A full-screen map of one kind of place, with a search field and a choice of country floating above it; there is
/// no list. Zoomed out, the map shows one bubble per country, further in bubbles with counts for crowded areas instead
/// of pins, which keeps the map fast. A tap on a pin opens the place.
///
/// The map moves by itself only when someone asks for something: when the search term settles it fits the hits, when
/// a country is chosen it shows that country. Panning or zooming never moves it, and clearing the search leaves it
/// where it is. When hits lie outside the view, the line at the bottom offers to show them all.
///
/// Favourites always show as pins with a star, even where the rest is grouped into bubbles; the star next to the search
/// field shows only them.
struct PlaceOverview<Item: Place>: View {
    let title: String
    let singular: String
    let plural: String
    let systemImage: String
    let tint: Color
    /// For the favourites of this kind.
    let type: PlaceType
    let load: @MainActor () async throws -> [Item]
    /// Gets the search term and everything loaded, for places that are searched locally.
    let search: @MainActor (String, [Item]) async throws -> [Item]
    let onSelect: (Item) -> Void
    /// Shows a plus button that suggests a new entry in the visible area.
    var onAdd: ((MapArea) -> Void)? = nil

    /// More pins than this in the visible area are grouped into bubbles.
    private static var pinLimit: Int { 100 }

    @State private var text = ""
    /// The search term once typing has paused; everything shown follows this, not every keystroke.
    @State private var query = ""
    @State private var items: [Item]?
    @State private var results: [Item]?
    @State private var error: Error?
    /// Counted over everything, so the choice keeps its numbers while a search narrows the map.
    @State private var countries: [CountryCount] = []
    @State private var country: String?
    @State private var area = MapArea.start
    @State private var mapWidth = 390.0
    @State private var position = MapCameraPosition.region(MKCoordinateRegion(MapArea.start))
    @State private var selection: String?
    @State private var favoritesOnly = false
    @Environment(Favorites.self) private var favorites
    @FocusState private var searchFocused: Bool

    private var searching: Bool { !query.isEmpty }

    /// What the map may show: the search results or everything, in the chosen country, perhaps only favourites.
    private var candidates: [Item] {
        let all = searching ? results ?? [] : items ?? []
        return all.filter { item in
            (country == nil || item.country == country) && (!favoritesOnly || favorites.contains(type, item.id))
        }
    }

    private var points: [(latitude: Double, longitude: Double)] {
        candidates.map { (latitude: $0.latitude, longitude: $0.longitude) }
    }

    var body: some View {
        ZStack {
            map.ignoresSafeArea(edges: [.top, .leading, .trailing])
        }
        .osmAttribution(true)
        .overlay(alignment: .top) {
            MapSearchBar(
                text: $text,
                prompt: "Name, Ort oder Gewässer",
                countries: countries,
                country: $country,
                favoritesOnly: $favoritesOnly,
                focused: $searchFocused,
                addLabel: onAdd == nil ? nil : "\(singular) fehlt?",
                onAdd: onAdd == nil ? nil : { onAdd?(area) }
            )
        }
        .overlay(alignment: .bottom) { notice }
        .navigationTitle(title)
        .toolbar(.hidden, for: .navigationBar)
        .task { await reload() }
        .task(id: text) { await find() }
        .onChange(of: selection) { _, id in
            guard let id else { return }
            selection = nil
            searchFocused = false
            if let item = candidates.first(where: { $0.id == id }) { onSelect(item) }
        }
        .onChange(of: country) { _, _ in focus() }
        // Like a search: turning it on shows all favourites, turning it off leaves the map where it is.
        .onChange(of: favoritesOnly) { _, only in if only { focus() } }
    }

    private var map: some View {
        let shown = candidates
        let starred = shown.filter { favorites.contains(type, $0.id) }
        // Favourites are drawn on their own; with only them there is nothing to group.
        let rest = favoritesOnly ? [] : shown.filter { !favorites.contains(type, $0.id) }
        let groups = !rest.isEmpty && Overview.showsCountries(zoom: area.zoom(width: mapWidth)) && shown.allSatisfy { $0.country != nil }
            ? Countries.group(shown, country: \.country) { ($0.latitude, $0.longitude) }
            : []
        let byCountry = groups.count > 1
        let pins = byCountry ? [] : Overview.visible(rest, in: area, limit: Self.pinLimit) { ($0.latitude, $0.longitude) }
        let clusters = byCountry || pins != nil ? [] : Overview.clusters(rest, in: area) { ($0.latitude, $0.longitude) }
        // North stays up: a compass would sit under the search field.
        return Map(position: $position, interactionModes: [.pan, .zoom], selection: $selection) {
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
            ForEach(starred) { item in
                Marker(item.name, systemImage: "star.fill", coordinate: CLLocationCoordinate2D(latitude: item.latitude, longitude: item.longitude))
                    .tint(tint)
                    .tag(item.id)
            }
            if byCountry {
                ForEach(groups) { group in
                    Annotation("", coordinate: CLLocationCoordinate2D(latitude: group.latitude, longitude: group.longitude), anchor: .center) {
                        Button { withAnimation { position = .region(MKCoordinateRegion(group.area)) } } label: {
                            CountryBubble(code: group.code, count: group.items.count, tint: tint)
                        }
                        .buttonStyle(.plain)
                        .accessibilityLabel("\(group.name): \(Format.count(group.items.count, singular, plural))")
                    }
                    .annotationTitles(.hidden)
                }
            }
        }
        .onMapCameraChange(frequency: .onEnd) { context in
            area = MapArea(context.region)
        }
        .onGeometryChange(for: Double.self) { $0.size.width } action: { mapWidth = $0 }
        // Moving the map puts the keyboard away, so the whole screen is map again.
        .simultaneousGesture(DragGesture(minimumDistance: 10).onChanged { _ in
            if searchFocused { searchFocused = false }
        })
    }

    /// Loading, a failure, or how many hits the search found; nothing while the map simply shows everything.
    @ViewBuilder
    private var notice: some View {
        let term = text.trimmingCharacters(in: .whitespacesAndNewlines)
        if items == nil, error != nil {
            MapNotice(text: "\(plural) konnten nicht geladen werden", systemImage: "wifi.exclamationmark", actionTitle: "Erneut versuchen") {
                Task { await reload() }
            }
        } else if items == nil {
            MapNotice(text: "\(plural) werden geladen", loading: true)
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
        } else if favoritesOnly {
            if candidates.isEmpty {
                MapNotice(
                    text: country.map { "Keine Favoriten in \(Countries.name($0))" } ?? "Noch keine Favoriten: Stern auf der Seite antippen",
                    systemImage: "star")
            } else if Overview.anyOutside(points, of: area) {
                MapNotice(text: Format.count(candidates.count, "Favorit", "Favoriten"), actionTitle: "Alle zeigen") { focus() }
            }
        }
    }

    /// Fits the map to the search hits or favourites, else the chosen country (`Overview.focus`).
    private func focus() {
        guard let target = Overview.focus(points, searching: searching || favoritesOnly, country: country) else { return }
        withAnimation { position = .region(MKCoordinateRegion(target)) }
    }

    private func zoom(to cluster: Cluster<Item>) {
        guard let fitted = MapArea.fitting(cluster.items.map { (latitude: $0.latitude, longitude: $0.longitude) }) else { return }
        withAnimation { position = .region(MKCoordinateRegion(fitted)) }
    }

    private func reload() async {
        do {
            let loaded = try await load()
            items = loaded
            countries = Countries.group(loaded, country: \.country) { ($0.latitude, $0.longitude) }
                .map { CountryCount(code: $0.code, count: $0.items.count) }
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
            focus()
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

/// One bubble per country with its flag and count, as the website shows them zoomed out.
struct CountryBubble: View {
    let code: String
    let count: Int
    let tint: Color

    var body: some View {
        VStack(spacing: 0) {
            if let flag = Countries.flag(code) {
                Text(flag).font(.title3)
            } else {
                Text(code).font(.caption.bold())
            }
            Text(Format.number(count)).font(.caption2)
        }
        .foregroundStyle(.white)
        .padding(8)
        .frame(minWidth: 50, minHeight: 50)
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

import MapKit
import SlipwaysKit
import SwiftUI

/// Map with search and choice of country for one kind of place; the list below shows only what lies in the visible
/// area or matches the search. Zoomed out, the map shows one bubble per country, further in bubbles with counts for
/// crowded areas instead of pins, which keeps the map fast.
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
    /// Shows a plus button that suggests a new entry in the visible area.
    var onAdd: ((MapArea) -> Void)? = nil
    @ViewBuilder let row: (Item) -> Row

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

    private var searching: Bool { !query.isEmpty }

    /// What the map may show: the search results or everything, in the chosen country.
    private var candidates: [Item] {
        let all = searching ? results ?? [] : items ?? []
        guard let country else { return all }
        return all.filter { $0.country == country }
    }

    /// Nil when the area holds too many entries to list.
    private var listed: [Item]? {
        if searching { return candidates }
        return Overview.visible(candidates, in: area) { ($0.latitude, $0.longitude) }
    }

    var body: some View {
        VStack(spacing: 0) {
            map
            list
        }
        .navigationTitle(title)
        .navigationBarTitleDisplayMode(.inline)
        .searchable(text: $text, placement: .navigationBarDrawer(displayMode: .always), prompt: "Name, Ort oder Gewässer")
        .toolbar {
            ToolbarItem(placement: .topBarTrailing) { CountryMenu(options: countries, selection: $country) }
            if let onAdd {
                ToolbarItem(placement: .topBarLeading) {
                    Button("\(singular) fehlt?", systemImage: "plus") { onAdd(area) }
                }
            }
        }
        .task { await reload() }
        .task(id: text) { await find() }
        .onChange(of: selection) { _, id in
            guard let id else { return }
            selection = nil
            if let item = candidates.first(where: { $0.id == id }) { onSelect(item) }
        }
        .onChange(of: country) { _, _ in
            let points = candidates.map { (latitude: $0.latitude, longitude: $0.longitude) }
            let target = searching ? MapArea.fitting(points) : (country == nil ? MapArea.start : Countries.coreArea(points))
            if let target { withAnimation { position = .region(MKCoordinateRegion(target)) } }
        }
    }

    private var map: some View {
        let shown = candidates
        let groups = Overview.showsCountries(zoom: area.zoom(width: mapWidth)) && shown.allSatisfy { $0.country != nil }
            ? Countries.group(shown, country: \.country) { ($0.latitude, $0.longitude) }
            : []
        let byCountry = groups.count > 1
        let pins = byCountry ? [] : Overview.visible(shown, in: area, limit: Self.pinLimit) { ($0.latitude, $0.longitude) }
        let clusters = byCountry || pins != nil ? [] : Overview.clusters(shown, in: area) { ($0.latitude, $0.longitude) }
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
                    Section(searching ? Format.count(listed.count, "Treffer", "Treffer") : "\(Format.count(listed.count, singular, plural)) im Kartenausschnitt") {
                        ForEach(listed) { row($0) }
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
                description: Text("\(Format.count(candidates.count, singular, plural)), zu viele für eine Liste.")
            )
            .frame(maxHeight: .infinity)
        }
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
            let points = candidates.map { (latitude: $0.latitude, longitude: $0.longitude) }
            if let fitted = MapArea.fitting(points) {
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

/// Chooses one country or all; hidden while there is nothing to choose between.
struct CountryMenu: View {
    let options: [CountryCount]
    @Binding var selection: String?

    var body: some View {
        if options.count >= 2 {
            Menu {
                Picker("Land", selection: $selection) {
                    Text("Alle Länder").tag(String?.none)
                    ForEach(options) { option in
                        Text(option.label).tag(String?.some(option.code))
                    }
                }
            } label: {
                Label(selection.map(Countries.name) ?? "Alle Länder", systemImage: selection == nil ? "globe.europe.africa" : "globe.europe.africa.fill")
            }
            .accessibilityLabel("Land")
        }
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

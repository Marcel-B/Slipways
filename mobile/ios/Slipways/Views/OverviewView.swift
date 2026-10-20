import MapKit
import SlipwaysKit
import SwiftUI

/// Map with search; the list below shows only what lies in the visible area or matches the search.
struct OverviewView: View {
    @Environment(\.api) private var api
    @Environment(\.openURL) private var openURL
    @State private var search = ""
    @State private var slipways: [SlipwaySummary]?
    @State private var marinas: [Marina] = []
    @State private var results: [SlipwaySummary]?
    @State private var error: Error?
    @State private var area = MapArea.start
    @State private var position = MapCameraPosition.region(MKCoordinateRegion(MapArea.start))
    @State private var selection: String?
    @State private var selectedSlipway: SlipwaySummary?

    private var searching: Bool { !search.trimmingCharacters(in: .whitespaces).isEmpty }

    /// Nil when the area holds too many entries to list.
    private var shownSlipways: [SlipwaySummary]? {
        if searching { return results ?? [] }
        return Overview.visible(slipways ?? [], in: area) { ($0.latitude, $0.longitude) }
    }

    private var shownMarinas: [Marina]? {
        if searching { return marinas.filter { Overview.matches($0, search: search) } }
        return Overview.visible(marinas, in: area) { ($0.latitude, $0.longitude) }
    }

    var body: some View {
        VStack(spacing: 0) {
            map
            list
        }
        .navigationTitle("Slipanlagen")
        .navigationBarTitleDisplayMode(.inline)
        .searchable(text: $search, placement: .navigationBarDrawer(displayMode: .always), prompt: "Name, Ort oder Gewässer")
        .navigationDestination(item: $selectedSlipway) { SlipwayDetailView(slipway: $0) }
        .task { await load() }
        .task(id: search) { await find() }
        .onChange(of: selection) { _, tag in open(tag) }
    }

    private var map: some View {
        let slipways = shownSlipways ?? []
        let marinas = shownMarinas ?? []
        return Map(position: $position, selection: $selection) {
            ForEach(slipways) { slipway in
                Marker(slipway.name, systemImage: "sailboat", coordinate: CLLocationCoordinate2D(latitude: slipway.latitude, longitude: slipway.longitude))
                    .tag("s:" + slipway.id)
            }
            ForEach(marinas) { marina in
                Marker(marina.name, systemImage: "ferry", coordinate: CLLocationCoordinate2D(latitude: marina.latitude, longitude: marina.longitude))
                    .tint(.teal)
                    .tag("m:" + marina.id)
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
        if slipways == nil && error != nil {
            LoadFailedView(title: "Slipanlagen konnten nicht geladen werden") { await load() }
                .frame(maxHeight: .infinity)
        } else if slipways == nil || (searching && results == nil) {
            ProgressView().frame(maxHeight: .infinity)
        } else if let shownSlipways, let shownMarinas {
            if shownSlipways.isEmpty && shownMarinas.isEmpty {
                Group {
                    if searching {
                        ContentUnavailableView.search(text: search)
                    } else {
                        ContentUnavailableView("Hier gibt es keine Einträge", systemImage: "map", description: Text("Verschiebe die Karte oder suche."))
                    }
                }
                .frame(maxHeight: .infinity)
            } else {
                List {
                    if !shownSlipways.isEmpty {
                        Section(Format.count(shownSlipways.count, "Slipanlage", "Slipanlagen")) {
                            ForEach(shownSlipways) { slipway in
                                NavigationLink(value: slipway) {
                                    VStack(alignment: .leading) {
                                        Text(slipway.name)
                                        if !slipway.subtitle.isEmpty {
                                            Text(slipway.subtitle).font(.subheadline).foregroundStyle(.secondary)
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if !shownMarinas.isEmpty {
                        Section(Format.count(shownMarinas.count, "Marina", "Marinas")) {
                            ForEach(shownMarinas) { marina in
                                if let text = marina.url, let url = Links.web(text) {
                                    Link(destination: url) { MarinaLabel(marina: marina) }
                                } else {
                                    MarinaLabel(marina: marina)
                                }
                            }
                        }
                    }
                    Section { } footer: { OSMAttribution() }
                }
                .listStyle(.plain)
                .frame(maxHeight: .infinity)
            }
        } else {
            ContentUnavailableView("Zoome hinein oder suche", systemImage: "magnifyingglass", description: Text("Im Kartenausschnitt liegen zu viele Slipanlagen und Marinas für eine Liste."))
                .frame(maxHeight: .infinity)
        }
    }

    private func open(_ tag: String?) {
        guard let tag else { return }
        selection = nil
        let id = String(tag.dropFirst(2))
        if tag.hasPrefix("s:") {
            selectedSlipway = (shownSlipways ?? []).first { $0.id == id }
        } else if let text = marinas.first(where: { $0.id == id })?.url, let url = Links.web(text) {
            openURL(url)
        }
    }

    private func load() async {
        do {
            async let loadedSlipways = api.slipways()
            async let loadedMarinas = try? api.marinas()
            slipways = try await loadedSlipways
            marinas = await loadedMarinas ?? []
            error = nil
        } catch let error where error.isCancellation {
        } catch {
            self.error = error
        }
    }

    private func find() async {
        guard searching else {
            results = nil
            return
        }
        // Debounce typing; a newer search cancels this task.
        try? await Task.sleep(for: .milliseconds(300))
        if Task.isCancelled { return }
        do {
            let found = try await api.slipways(search: search)
            results = found
            let points = found.map { (latitude: $0.latitude, longitude: $0.longitude) }
                + (shownMarinas ?? []).map { (latitude: $0.latitude, longitude: $0.longitude) }
            if let fitted = MapArea.fitting(points) {
                withAnimation { position = .region(MKCoordinateRegion(fitted)) }
            }
        } catch let error where error.isCancellation {
        } catch {
            results = []
        }
    }
}

private struct MarinaLabel: View {
    let marina: Marina

    var body: some View {
        Label {
            VStack(alignment: .leading) {
                Text(marina.name)
                if let place = Format.place(marina.postalCode, marina.city) {
                    Text(place).font(.subheadline).foregroundStyle(.secondary)
                }
            }
        } icon: {
            Image(systemName: "ferry").foregroundStyle(.teal)
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

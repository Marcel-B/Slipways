import MapKit
import SlipwaysKit
import SwiftUI

/// The ODbL attribution for entries and shapes from OpenStreetMap; MapKit's own "Legal" link does not cover them.
struct OSMAttribution: View {
    var body: some View {
        Link(Links.osmAttribution, destination: Links.osmCopyright)
            .font(.caption2)
    }
}

extension View {
    /// Puts the OpenStreetMap attribution in the bottom corner of a map.
    @ViewBuilder
    func osmAttribution(_ shown: Bool) -> some View {
        if shown {
            overlay(alignment: .bottomTrailing) {
                // Plain text: a link here sits where people touch the map and opened the browser by mistake.
                // Detail pages and the info tab link to the licence.
                Text(Links.osmAttribution)
                    .font(.caption2)
                    .foregroundStyle(.secondary)
                    .padding(.horizontal, 6)
                    .padding(.vertical, 3)
                    .background(.regularMaterial, in: .rect(cornerRadius: 4))
                    .padding(6)
                    .allowsHitTesting(false)
            }
        } else {
            self
        }
    }
}

struct WaterLevelText: View {
    let level: WaterLevel

    var body: some View {
        Text(Format.waterLevel(level))
            .foregroundStyle(color)
    }

    private var color: Color {
        switch level.state {
        case "low": .orange
        case "high": .red
        case "normal": .green
        default: .secondary
        }
    }
}

/// Label and value in a list row; nothing when the value is missing.
struct Fact: View {
    let label: String
    let systemImage: String
    let value: String?

    var body: some View {
        if let value, !value.isEmpty {
            LabeledContent {
                Text(value).multilineTextAlignment(.trailing)
            } label: {
                Label(label, systemImage: systemImage)
            }
        }
    }
}

struct LoadFailedView: View {
    let title: String
    let retry: () async -> Void

    var body: some View {
        ContentUnavailableView {
            Label(title, systemImage: "wifi.exclamationmark")
        } actions: {
            Button("Erneut versuchen") { Task { await retry() } }
        }
    }
}

extension CLLocationCoordinate2D {
    init(_ position: Position) {
        self.init(latitude: position.latitude, longitude: position.longitude)
    }
}

extension Error {
    /// A newer search or leaving the screen cancels a request; that is no failure to show.
    var isCancellation: Bool {
        self is CancellationError || (self as? URLError)?.code == .cancelled
    }
}

/// Like `Fact`, with where the value comes from when users or the place's website sent it, a note when this device
/// sent a value that waits, and a pencil to suggest another one.
struct SourcedFact: View {
    let label: String
    let systemImage: String
    let value: SourcedValue?
    var display: (String) -> String = { $0 }
    var link: ((String) -> URL?)? = nil
    var pending = false
    var onEdit: (() -> Void)? = nil

    var body: some View {
        if let value, !value.value.isEmpty {
            LabeledContent {
                HStack(spacing: 8) {
                    VStack(alignment: .trailing, spacing: 2) {
                        if let url = link?(value.value) {
                            Link(display(value.value), destination: url)
                        } else {
                            Text(display(value.value)).multilineTextAlignment(.trailing)
                        }
                        if let note = value.note {
                            Text(note).font(.caption).foregroundStyle(.secondary)
                        }
                        if pending {
                            Text("Dein Vorschlag wird geprüft").font(.caption).foregroundStyle(.orange)
                        }
                    }
                    if let onEdit { EditButton(label: label, action: onEdit) }
                }
            } label: {
                Label(label, systemImage: systemImage)
            }
        }
    }
}

/// Tips from users below a slipway or marina.
struct TipsSection: View {
    let tips: [PlaceInfo]

    var body: some View {
        if !tips.isEmpty {
            Section("Tipps") {
                ForEach(tips, id: \.self) { tip in
                    VStack(alignment: .leading, spacing: 2) {
                        Text(tip.value)
                        Text(Format.monthYear(tip.retrieved)).font(.caption).foregroundStyle(.secondary)
                    }
                }
            }
        }
    }
}

/// The gauge with a current reading closest to a place, from the water the place is on.
struct NearestGaugeSection: View {
    let water: WaterDetails?
    let latitude: Double
    let longitude: Double

    var body: some View {
        if let water, let nearest = water.nearestGauge(latitude: latitude, longitude: longitude) {
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
}

/// A map on a details page that zooms in when the page opens, like the website's.
struct DetailMap<Content: MapContent>: View {
    let region: MKCoordinateRegion
    var onAreaChange: ((MapArea) -> Void)? = nil
    @MapContentBuilder let content: () -> Content

    @Environment(\.accessibilityReduceMotion) private var reduceMotion
    @State private var position: MapCameraPosition

    init(region: MKCoordinateRegion, onAreaChange: ((MapArea) -> Void)? = nil, @MapContentBuilder content: @escaping () -> Content) {
        self.region = region
        self.onAreaChange = onAreaChange
        self.content = content
        // Starts further out; the task below flies in.
        let wide = MKCoordinateSpan(latitudeDelta: min(region.span.latitudeDelta * 8, 80), longitudeDelta: min(region.span.longitudeDelta * 8, 160))
        _position = State(initialValue: .region(MKCoordinateRegion(center: region.center, span: wide)))
    }

    var body: some View {
        Map(position: $position) { content() }
            .onMapCameraChange(frequency: .onEnd) { context in onAreaChange?(MapArea(context.region)) }
            .task {
                if reduceMotion {
                    position = .region(region)
                    return
                }
                try? await Task.sleep(for: .milliseconds(150))
                withAnimation(.easeInOut(duration: 1)) { position = .region(region) }
            }
    }
}

/// Opens a place in Apple Maps, with a route there when asked.
enum MapsApp {
    static func open(name: String, latitude: Double, longitude: Double, route: Bool = false) {
        let item = MKMapItem(placemark: MKPlacemark(coordinate: CLLocationCoordinate2D(latitude: latitude, longitude: longitude)))
        item.name = name
        item.openInMaps(launchOptions: route ? [MKLaunchOptionsDirectionsModeKey: MKLaunchOptionsDirectionsModeDriving] : nil)
    }
}

/// A marina to open in the sheet, from a list, a slipway or a water.
struct MarinaRef: Identifiable, Hashable {
    let id: String
    let name: String
}

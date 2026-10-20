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
                OSMAttribution()
                    .padding(.horizontal, 6)
                    .padding(.vertical, 3)
                    .background(.regularMaterial, in: .rect(cornerRadius: 4))
                    .padding(6)
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

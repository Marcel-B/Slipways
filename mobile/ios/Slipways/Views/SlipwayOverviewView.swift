import SlipwaysKit
import SwiftUI

struct SlipwayOverviewView: View {
    @Environment(\.api) private var api
    @State private var selected: SlipwaySummary?

    var body: some View {
        let api = api
        PlaceOverview(
            title: "Slipanlagen",
            singular: "Slipanlage",
            plural: "Slipanlagen",
            systemImage: "sailboat",
            tint: .red,
            load: { try await api.slipways() },
            search: { term, _ in try await api.slipways(search: term) },
            onSelect: { selected = $0 }
        ) { slipway in
            NavigationLink(value: slipway) {
                VStack(alignment: .leading) {
                    Text(slipway.name)
                    if !slipway.subtitle.isEmpty {
                        Text(slipway.subtitle).font(.subheadline).foregroundStyle(.secondary)
                    }
                }
            }
        }
        .navigationDestination(item: $selected) { SlipwayDetailView(slipway: $0) }
    }
}

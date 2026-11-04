import SlipwaysKit
import SwiftUI

struct MarinaOverviewView: View {
    @Environment(\.api) private var api
    @State private var selected: MarinaRef?

    var body: some View {
        let api = api
        PlaceOverview(
            title: "Marinas",
            singular: "Marina",
            plural: "Marinas",
            systemImage: "ferry",
            tint: .teal,
            type: .marina,
            load: { try await api.marinas() },
            // The API has no marina search; a few thousand marinas are quick to filter here.
            search: { term, all in all.filter { Overview.matches($0, search: term) } },
            onSelect: { selected = MarinaRef(id: $0.id, name: $0.name) }
        )
        .marinaSheet($selected)
    }
}

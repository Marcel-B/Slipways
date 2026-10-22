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
            load: { try await api.marinas() },
            // The API has no marina search; a few thousand marinas are quick to filter here.
            search: { term, all in all.filter { Overview.matches($0, search: term) } },
            onSelect: { selected = MarinaRef(id: $0.id, name: $0.name) }
        ) { marina in
            Button { selected = MarinaRef(id: marina.id, name: marina.name) } label: { MarinaLabel(marina: marina) }
                .foregroundStyle(.primary)
        }
        .marinaSheet($selected)
    }
}

struct MarinaLabel: View {
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

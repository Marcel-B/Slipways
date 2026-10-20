import SlipwaysKit
import SwiftUI

struct MarinaOverviewView: View {
    @Environment(\.api) private var api
    @State private var selected: Marina?

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
            onSelect: { selected = $0 }
        ) { marina in
            Button { selected = marina } label: { MarinaLabel(marina: marina) }
                .foregroundStyle(.primary)
        }
        .sheet(item: $selected) { marina in
            NavigationStack { MarinaDetailView(marina: marina).slipwaysDestinations() }
                .presentationDetents([.medium, .large])
        }
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

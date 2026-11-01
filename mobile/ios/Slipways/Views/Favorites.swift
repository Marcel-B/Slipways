import Observation
import SlipwaysKit
import SwiftUI

/// The favourites of this device for the views: kept in `FavoriteStore`, mirrored here so the map, the pages and the
/// list update together when one is added or removed.
@MainActor @Observable
final class Favorites {
    private let store: FavoriteStore
    private(set) var all: [Favorite]
    private var ids: Set<String>

    init(store: FavoriteStore = FavoriteStore()) {
        let loaded = store.all()
        self.store = store
        all = loaded
        ids = Set(loaded.map(\.id))
    }

    func contains(_ type: PlaceType, _ id: String) -> Bool {
        ids.contains("\(type.rawValue)/\(id)")
    }

    func toggle(_ favorite: Favorite) {
        if contains(favorite.type, favorite.placeId) {
            store.remove(favorite.type, favorite.placeId)
        } else {
            store.add(favorite)
        }
        reload()
    }

    func remove(_ favorite: Favorite) {
        store.remove(favorite.type, favorite.placeId)
        reload()
    }

    /// Keeps name and position of a favourite up to date when its page loads; does nothing for other places.
    func refresh(_ favorite: Favorite) {
        guard let old = all.first(where: { $0.id == favorite.id }),
              old.name != favorite.name || old.latitude != favorite.latitude || old.longitude != favorite.longitude
        else { return }
        store.add(favorite)
        reload()
    }

    private func reload() {
        all = store.all()
        ids = Set(all.map(\.id))
    }
}

/// The star in the toolbar of a slipway or marina; disabled until the page knows enough to remember the place.
struct FavoriteButton: View {
    let favorite: Favorite?
    @Environment(Favorites.self) private var favorites

    var body: some View {
        let marked = favorite.map { favorites.contains($0.type, $0.placeId) } ?? false
        Button {
            if let favorite { favorites.toggle(favorite) }
        } label: {
            Image(systemName: marked ? "star.fill" : "star")
                .foregroundStyle(marked ? Color.yellow : Color.accentColor)
        }
        .disabled(favorite == nil)
        .accessibilityLabel(marked ? "Aus Favoriten entfernen" : "Zu Favoriten hinzufügen")
        .sensoryFeedback(.selection, trigger: marked)
    }
}

/// All favourites as a list, without loading anything; a tap opens the place, swiping removes it.
struct FavoritesView: View {
    @Environment(Favorites.self) private var favorites
    @State private var marina: MarinaRef?

    var body: some View {
        let slipways = favorites.all.filter { $0.type == .slipway }
        let marinas = favorites.all.filter { $0.type == .marina }
        Group {
            if favorites.all.isEmpty {
                ContentUnavailableView {
                    Label("Noch keine Favoriten", systemImage: "star")
                } description: {
                    Text("Tippe auf der Seite einer Slipanlage oder Marina auf den Stern. Favoriten bleiben auf diesem Gerät, ein Konto ist nicht nötig.")
                }
            } else {
                List {
                    if !slipways.isEmpty {
                        Section("Slipanlagen") {
                            ForEach(slipways) { favorite in
                                NavigationLink(value: SlipwaySummary(favorite)) {
                                    Label(favorite.name, systemImage: "sailboat")
                                }
                            }
                            .onDelete { remove(slipways, at: $0) }
                        }
                    }
                    if !marinas.isEmpty {
                        Section("Marinas") {
                            ForEach(marinas) { favorite in
                                Button { marina = MarinaRef(id: favorite.placeId, name: favorite.name) } label: {
                                    Label(favorite.name, systemImage: "ferry")
                                }
                                .foregroundStyle(.primary)
                            }
                            .onDelete { remove(marinas, at: $0) }
                        }
                    }
                }
            }
        }
        .navigationTitle("Favoriten")
        .marinaSheet($marina)
    }

    private func remove(_ list: [Favorite], at offsets: IndexSet) {
        for index in offsets { favorites.remove(list[index]) }
    }
}

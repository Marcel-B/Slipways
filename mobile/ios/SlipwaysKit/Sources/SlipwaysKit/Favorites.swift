import Foundation

/// A slipway or marina marked as a favourite on this device. Name and position are kept so the list of favourites
/// shows without loading anything.
public struct Favorite: Codable, Hashable, Identifiable, Sendable {
    public let type: PlaceType
    public let placeId: String
    public let name: String
    public let latitude: Double
    public let longitude: Double
    /// Of a slipway, for the OpenStreetMap notice when it opens from the list.
    public let source: Source
    public let added: Date

    public var id: String { "\(type.rawValue)/\(placeId)" }

    public init(
        type: PlaceType, placeId: String, name: String, latitude: Double, longitude: Double, source: Source = .manual,
        added: Date = .now
    ) {
        self.type = type
        self.placeId = placeId
        self.name = name
        self.latitude = latitude
        self.longitude = longitude
        self.source = source
        self.added = added
    }
}

/// Favourites in the app's defaults. There are no accounts, so they stay on the device and leave it with nothing.
public final class FavoriteStore: @unchecked Sendable {
    private static let key = "slipways-favorites"
    private let defaults: UserDefaults
    private let lock = NSLock()

    public init(defaults: UserDefaults = .standard) {
        self.defaults = defaults
    }

    /// Sorted by name.
    public func all() -> [Favorite] {
        lock.withLock { load() }.sorted { $0.name.localizedStandardCompare($1.name) == .orderedAscending }
    }

    public func contains(_ type: PlaceType, _ id: String) -> Bool {
        lock.withLock { load() }.contains { $0.type == type && $0.placeId == id }
    }

    /// Adds the place, or replaces its entry when it is there already (e.g. after it was renamed).
    public func add(_ favorite: Favorite) {
        lock.withLock {
            var all = load()
            if let index = all.firstIndex(where: { $0.id == favorite.id }) {
                all[index] = Favorite(
                    type: favorite.type, placeId: favorite.placeId, name: favorite.name, latitude: favorite.latitude,
                    longitude: favorite.longitude, source: favorite.source, added: all[index].added)
            } else {
                all.append(favorite)
            }
            save(all)
        }
    }

    public func remove(_ type: PlaceType, _ id: String) {
        lock.withLock { save(load().filter { !($0.type == type && $0.placeId == id) }) }
    }

    private func load() -> [Favorite] {
        guard let data = defaults.data(forKey: Self.key),
              let all = try? JSONDecoder().decode([Favorite].self, from: data) else { return [] }
        return all
    }

    private func save(_ favorites: [Favorite]) {
        if let data = try? JSONEncoder().encode(favorites) { defaults.set(data, forKey: Self.key) }
    }
}

extension SlipwaySummary {
    /// Enough of a slipway to open its page from the list of favourites; the page loads the rest.
    public init(_ favorite: Favorite) {
        self.init(
            id: favorite.placeId, name: favorite.name, latitude: favorite.latitude, longitude: favorite.longitude,
            postalCode: nil, city: nil, country: nil, water: nil, source: favorite.source)
    }
}

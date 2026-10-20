import Foundation

/// A rectangle on the map, as the visible part of the overview map.
public struct MapArea: Hashable, Sendable {
    public let south: Double
    public let west: Double
    public let north: Double
    public let east: Double

    public init(south: Double, west: Double, north: Double, east: Double) {
        self.south = south
        self.west = west
        self.north = north
        self.east = east
    }

    public init(centerLatitude: Double, centerLongitude: Double, latitudeDelta: Double, longitudeDelta: Double) {
        self.init(
            south: centerLatitude - latitudeDelta / 2,
            west: centerLongitude - longitudeDelta / 2,
            north: centerLatitude + latitudeDelta / 2,
            east: centerLongitude + longitudeDelta / 2
        )
    }

    /// Germany and its neighbours as the map shows them at start.
    public static let start = MapArea(south: 46.0, west: 4.5, north: 55.5, east: 16.5)

    public var centerLatitude: Double { (south + north) / 2 }
    public var centerLongitude: Double { (west + east) / 2 }
    public var latitudeDelta: Double { north - south }
    public var longitudeDelta: Double { east - west }

    public func contains(latitude: Double, longitude: Double) -> Bool {
        (south...north).contains(latitude) && (west...east).contains(longitude)
    }

    /// The smallest area around the points with a margin, at least about 2 km wide; nil without points.
    public static func fitting(_ points: [(latitude: Double, longitude: Double)]) -> MapArea? {
        guard let first = points.first else { return nil }
        var area = MapArea(south: first.latitude, west: first.longitude, north: first.latitude, east: first.longitude)
        for point in points.dropFirst() {
            area = MapArea(
                south: min(area.south, point.latitude),
                west: min(area.west, point.longitude),
                north: max(area.north, point.latitude),
                east: max(area.east, point.longitude)
            )
        }
        return MapArea(
            centerLatitude: area.centerLatitude,
            centerLongitude: area.centerLongitude,
            latitudeDelta: max(area.latitudeDelta * 1.3, 0.02),
            longitudeDelta: max(area.longitudeDelta * 1.3, 0.03)
        )
    }
}

/// What the overview shows: thousands of pins help nobody, so only a zoomed-in area lists its entries.
public enum Overview {
    /// More entries than this in the visible area ask the user to zoom in or search.
    public static let limit = 250

    /// The entries inside the area, or nil when there are more than `limit`.
    public static func visible<T>(
        _ items: [T],
        in area: MapArea,
        limit: Int = Overview.limit,
        position: (T) -> (latitude: Double, longitude: Double)
    ) -> [T]? {
        var result: [T] = []
        for item in items {
            let point = position(item)
            guard area.contains(latitude: point.latitude, longitude: point.longitude) else { continue }
            result.append(item)
            if result.count > limit { return nil }
        }
        return result
    }

    /// Case- and accent-insensitive match on name, city or postal code, as the API search does for slipways.
    public static func matches(_ marina: Marina, search: String) -> Bool {
        let term = search.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !term.isEmpty else { return true }
        return [marina.name, marina.city, marina.postalCode, marina.water?.name].compactMap { $0 }.contains {
            $0.range(of: term, options: [.caseInsensitive, .diacriticInsensitive]) != nil
        }
    }
}

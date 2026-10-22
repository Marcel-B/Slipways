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

    /// Europe as the map shows it at start, one bubble per country like on the website.
    public static let start = MapArea(south: 36.0, west: -10.0, north: 64.0, east: 25.0)

    public var centerLatitude: Double { (south + north) / 2 }
    public var centerLongitude: Double { (west + east) / 2 }
    public var latitudeDelta: Double { north - south }
    public var longitudeDelta: Double { east - west }

    public func contains(latitude: Double, longitude: Double) -> Bool {
        (south...north).contains(latitude) && (west...east).contains(longitude)
    }

    /// The zoom level of a map `width` points wide showing this area, counted like the website's map (tiles of 256
    /// points, zoom 0 shows the whole world), so the apps switch at the same levels.
    public func zoom(width: Double) -> Double {
        guard longitudeDelta > 0, width > 0 else { return 0 }
        return log2(360 * width / (256 * longitudeDelta))
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

    /// Up to this zoom the map shows one bubble per country instead of clusters (`COUNTRY_MAX_ZOOM` on the website).
    public static let countryMaxZoom = 5

    /// True when the map shows a continent rather than a region, so countries are the bubbles to show.
    public static func showsCountries(zoom: Double) -> Bool {
        Int(zoom.rounded(.down)) <= countryMaxZoom
    }

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

    /// Groups the entries inside the area into a grid about `columns` cells wide, so a map shows a bubble with a count
    /// instead of hundreds of pins. Cells are fixed to the coordinates, so the bubbles stay put while the map pans.
    public static func clusters<T>(
        _ items: [T],
        in area: MapArea,
        columns: Int = 6,
        position: (T) -> (latitude: Double, longitude: Double)
    ) -> [Cluster<T>] {
        let width = area.longitudeDelta / Double(columns)
        guard width > 0 else { return [] }
        // A degree of longitude is shorter than one of latitude; this keeps the cells square on the map.
        let height = width * cos(area.centerLatitude * .pi / 180)
        var cells: [String: [T]] = [:]
        for item in items {
            let point = position(item)
            guard area.contains(latitude: point.latitude, longitude: point.longitude) else { continue }
            let key = "\(Int((point.latitude / height).rounded(.down))):\(Int((point.longitude / width).rounded(.down)))"
            cells[key, default: []].append(item)
        }
        return cells.map { key, members in
            let points = members.map(position)
            return Cluster(
                id: key,
                latitude: points.map(\.latitude).reduce(0, +) / Double(points.count),
                longitude: points.map(\.longitude).reduce(0, +) / Double(points.count),
                items: members
            )
        }
        .sorted { $0.id < $1.id }
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

/// Which water shapes to load for a map view (`shapeArea` on the website): those reaching into it, widened to a grid so
/// small moves reuse what was loaded, and further out only those large enough to see, about 20 points.
public struct ShapeArea: Hashable, Sendable {
    /// "west,south,east,north"
    public let bbox: String
    /// In degrees; smaller waters would be a dot.
    public let minExtent: Double

    /// From this zoom on every water is drawn, however small.
    public static let allZoom = 12

    public init(_ area: MapArea, zoom: Double) {
        let level = max(0, Int(zoom.rounded(.down)))
        // About one tile of 256 points at this zoom.
        let step = 360 / pow(2, Double(level))
        func snap(_ value: Double, _ rule: FloatingPointRoundingRule, _ low: Double, _ high: Double) -> Double {
            min(high, max(low, (value / step).rounded(rule) * step))
        }
        bbox = [
            snap(area.west - step, .down, -180, 180),
            snap(area.south - step, .down, -90, 90),
            snap(area.east + step, .up, -180, 180),
            snap(area.north + step, .up, -90, 90),
        ]
        .map { Self.text($0) }
        .joined(separator: ",")
        minExtent = level >= Self.allZoom ? 0 : (28 / pow(2, Double(level)) * 10_000).rounded() / 10_000
    }

    /// Up to four decimals and no trailing zeros, like the website, so both ask for the same, cacheable URLs.
    static func text(_ value: Double) -> String {
        let rounded = (value * 10_000).rounded() / 10_000
        return rounded == rounded.rounded() ? String(Int(rounded)) : String(rounded)
    }
}

/// Entries close together on the map, drawn as one bubble with their count.
public struct Cluster<T>: Identifiable {
    public let id: String
    public let latitude: Double
    public let longitude: Double
    public let items: [T]
}

/// Something with a pin on the overview map.
public protocol Place: Identifiable, Hashable, Sendable where ID == String {
    var name: String { get }
    var latitude: Double { get }
    var longitude: Double { get }
    /// ISO code such as "DE", for the country bubbles and the choice of country.
    var country: String? { get }
}

extension SlipwaySummary: Place {}
extension Marina: Place {}

extension WaterShape {
    /// The shape a tap at the point hits: inside a lake, or within `tolerance` degrees (of latitude) of a line or shore;
    /// the closest one when several do.
    public static func hit(_ shapes: [WaterShape], latitude: Double, longitude: Double, tolerance: Double) -> WaterShape? {
        // Longitude degrees shrink towards the poles; scaling them keeps distances true on the map.
        let scale = cos(latitude * .pi / 180)
        func point(_ p: Position) -> (x: Double, y: Double) { (p.longitude * scale, p.latitude) }
        let tap = (x: longitude * scale, y: latitude)

        func distance(to line: [Position]) -> Double {
            guard let first = line.first else { return .infinity }
            if line.count == 1 { return hypot(point(first).x - tap.x, point(first).y - tap.y) }
            var best = Double.infinity
            for (a, b) in zip(line, line.dropFirst()).map({ (point($0), point($1)) }) {
                let dx = b.x - a.x, dy = b.y - a.y
                let length = dx * dx + dy * dy
                let t = length == 0 ? 0 : max(0, min(1, ((tap.x - a.x) * dx + (tap.y - a.y) * dy) / length))
                best = min(best, hypot(a.x + t * dx - tap.x, a.y + t * dy - tap.y))
            }
            return best
        }

        func inside(_ ring: [Position]) -> Bool {
            var result = false
            for (a, b) in zip(ring, ring.dropFirst() + ring.prefix(1)) {
                if (a.latitude > latitude) != (b.latitude > latitude),
                   longitude < (b.longitude - a.longitude) * (latitude - a.latitude) / (b.latitude - a.latitude) + a.longitude {
                    result.toggle()
                }
            }
            return result
        }

        func distance(to shape: WaterShape) -> Double {
            let geometry = shape.geometry
            if geometry.polygons.contains(where: { $0.first.map(inside) == true }) { return 0 }
            let lines = geometry.lines + geometry.polygons.flatMap { $0 }
            return lines.map(distance(to:)).min() ?? .infinity
        }

        return shapes
            .map { ($0, distance(to: $0)) }
            .filter { $0.1 <= tolerance }
            .min { $0.1 < $1.1 }?
            .0
    }
}

extension MapArea: Identifiable {
    public var id: Self { self }
}

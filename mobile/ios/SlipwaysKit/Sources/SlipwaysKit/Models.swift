import Foundation

// Mirrors backend/src/Slipways.Api/Endpoints/Contracts.cs.

public struct Water: Codable, Hashable, Identifiable, Sendable {
    public let id: String
    public let name: String

    public init(id: String, name: String) {
        self.id = id
        self.name = name
    }
}

public struct Position: Codable, Hashable, Sendable {
    public let latitude: Double
    public let longitude: Double
}

public struct Bounds: Codable, Hashable, Sendable {
    public let south: Double
    public let west: Double
    public let north: Double
    public let east: Double
}

/// Where an entry comes from: our own data or OpenStreetMap (ODbL, needs attribution).
public enum Source: String, Codable, Hashable, Sendable {
    case manual
    case osm

    /// Unknown future sources count as our own data, so the list still loads.
    public init(from decoder: Decoder) throws {
        self = Source(rawValue: try decoder.singleValueContainer().decode(String.self)) ?? .manual
    }
}

/// Latest reading at a gauge from PEGELONLINE.
public struct WaterLevel: Codable, Hashable, Sendable {
    /// Centimetres.
    public let value: Double
    public let measuredAt: Date
    /// PEGELONLINE compares the level with mean low and mean high water, e.g. "low", "normal", "high".
    public let state: String?
}

public struct GaugeReading: Codable, Hashable, Sendable {
    public let stationId: String
    public let stationName: String
    public let waterLevel: WaterLevel
}

/// A water for the overview; `gauge` is set only when the water has exactly one gauge with a reading.
public struct WaterSummary: Codable, Hashable, Identifiable, Sendable {
    public let id: String
    public let name: String
    public let shortname: String?
    public let slipwayCount: Int
    public let stationCount: Int
    /// A slipway, gauge or marina near the middle of the water; nil when nothing on it has coordinates.
    public let position: Position?
    public let gauge: GaugeReading?
    /// ISO codes of the countries its slipways, marinas and gauges lie in, the one with the most first.
    public let countries: [String]

    public var water: Water { Water(id: id, name: name) }
}

public struct SlipwaySummary: Codable, Hashable, Identifiable, Sendable {
    public let id: String
    public let name: String
    public let latitude: Double
    public let longitude: Double
    public let postalCode: String?
    public let city: String?
    public let country: String?
    /// Nil when the slipway lies on none of the known waters (most imported ones).
    public let water: Water?
    public let source: Source

    /// "Donau · 93309 Kelheim"
    public var subtitle: String {
        [water?.name, Format.place(postalCode, city)].compactMap { $0 }.joined(separator: " · ")
    }
}

public struct MarinaSummary: Codable, Hashable, Sendable {
    public let id: String
    public let name: String
    public let url: String?
}

public struct SlipwayDetails: Codable, Hashable, Identifiable, Sendable {
    public let id: String
    public let name: String
    public let latitude: Double
    public let longitude: Double
    public let street: String?
    public let postalCode: String?
    public let city: String?
    public let country: String?
    public let url: String?
    public let comment: String?
    public let rating: Int
    /// Our own launch fee in euros; nil when unknown.
    public let costs: Decimal?
    public let pro: String?
    public let contra: String?
    /// `operator` to `surface` are OpenStreetMap tags as they are, e.g. fee "yes".
    public let `operator`: String?
    public let fee: String?
    public let access: String?
    public let openingHours: String?
    public let surface: String?
    public let water: Water?
    public let marina: MarinaSummary?
    public let extras: [String]
    public let source: Source
    /// Such as "node/123"; also set for our own slipways that were matched to OpenStreetMap.
    public let osmElement: String?
    public let updated: Date?
    /// Values from users, such as facts and tips; they win over the slipway's own.
    public let infos: [PlaceInfo]

    public var address: String {
        [street, Format.place(postalCode, city), country].compactMap { $0 }.joined(separator: ", ")
    }

    /// The value users sent in for a field, else the slipway's own (for imports from OpenStreetMap).
    public func value(_ field: String) -> SourcedValue? {
        if let info = Contributions.fromUsers(infos, field) { return SourcedValue(value: info.value, info: info) }
        let own: String? = switch field {
        case "fee": fee
        case "costs": costs.map { "\($0)" }
        case "operator": `operator`
        case "access": access
        case "opening_hours": openingHours
        case "surface": surface
        default: nil
        }
        return own.map { SourcedValue(value: $0, info: nil) }
    }

    /// Costs win over the fee; values from users over our own and those from OpenStreetMap.
    public var costsLabel: String {
        let costs = value("costs").flatMap { Decimal(string: $0.value, locale: Locale(identifier: "en_US_POSIX")) }
        let fee = value("fee")?.value
        if costs == 0 || (costs == nil && fee == "no") { return "kostenlos" }
        if let costs { return Format.euros(costs) }
        if let fee { return Labels.fee(fee) }
        return "Kosten unbekannt"
    }

    /// Facts users ticked, such as a jetty or a crane.
    public var ticked: [TickedFact] {
        Contributions.facts.compactMap { field in
            Contributions.fromUsers(infos, field).map { TickedFact(field: field, present: $0.value == "yes", retrieved: $0.retrieved) }
        }
    }

    public var tips: [PlaceInfo] { Contributions.tips(infos) }

    /// When the slipway last changed, for `SuggestionStore.pendingFields`.
    public var lastChanged: Date? {
        ([updated] + infos.map(\.retrieved)).compactMap { $0 }.max()
    }
}

public struct Marina: Codable, Hashable, Identifiable, Sendable {
    public let id: String
    public let name: String
    public let latitude: Double
    public let longitude: Double
    public let url: String?
    public let street: String?
    public let postalCode: String?
    public let city: String?
    public let phone: String?
    public let email: String?
    public let `operator`: String?
    public let fee: String?
    public let access: String?
    public let openingHours: String?
    public let water: Water?
    public let source: Source
    public let osmElement: String?
    /// ISO code of the country the import found it in, for our own marinas that of their slipways.
    public let country: String?

    /// "Hafenstraße 1, 93309 Kelheim"
    public var address: String {
        [street, Format.place(postalCode, city)].compactMap { $0 }.joined(separator: ", ")
    }
}

/// A marina with its slipways and details from other sources, such as its website or users.
public struct MarinaDetails: Codable, Hashable, Identifiable, Sendable {
    public let id: String
    public let name: String
    public let latitude: Double
    public let longitude: Double
    public let url: String?
    public let street: String?
    public let postalCode: String?
    public let city: String?
    public let phone: String?
    public let email: String?
    public let `operator`: String?
    public let fee: String?
    public let access: String?
    public let openingHours: String?
    public let water: Water?
    public let source: Source
    public let osmElement: String?
    public let slipways: [SlipwaySummary]
    public let infos: [PlaceInfo]

    /// "Hafenstraße 1, 93309 Kelheim"
    public var address: String {
        [street, Format.place(postalCode, city)].compactMap { $0 }.joined(separator: ", ")
    }

    /// Values from users win; details from the marina's website only fill gaps after its own.
    public func value(_ field: String) -> SourcedValue? {
        if let info = Contributions.fromUsers(infos, field) { return SourcedValue(value: info.value, info: info) }
        let own: String? = switch field {
        case "phone": phone
        case "email": email
        case "operator": `operator`
        case "fee": fee
        case "access": access
        case "opening_hours": openingHours
        default: nil
        }
        if let own { return SourcedValue(value: own, info: nil) }
        return infos.first { $0.field == field && $0.source == "website" }.map { SourcedValue(value: $0.value, info: $0) }
    }

    public var tips: [PlaceInfo] { Contributions.tips(infos) }

    /// When users last changed the marina, for `SuggestionStore.pendingFields`.
    public var lastChanged: Date? {
        infos.filter { $0.source == "user" }.map(\.retrieved).max()
    }

    /// True when the page shows anything from OpenStreetMap and so needs the attribution.
    public var usesOpenStreetMap: Bool {
        source == .osm || slipways.contains { $0.source == .osm }
    }
}

/// A detail such as a phone number from `source` ("website": read from `sourceUrl`, "user": an accepted suggestion),
/// as it was on `retrieved`. `field` is e.g. "phone", "opening_hours", "jetty" or "tip".
public struct PlaceInfo: Codable, Hashable, Sendable {
    public let field: String
    public let value: String
    public let source: String
    public let sourceUrl: String?
    public let retrieved: Date
}

/// A value a place shows, with where it comes from when it is not the place's own.
public struct SourcedValue: Hashable, Sendable {
    public let value: String
    /// Nil for the place's own value.
    public let info: PlaceInfo?

    /// "von Nutzern ergänzt, Stand 10.2026" or "laut Webseite, Stand 20.10.2026"; nil for the place's own value.
    public var note: String? {
        guard let info else { return nil }
        if info.source == "website" { return "laut Webseite, Stand \(Format.date(info.retrieved))" }
        return "von Nutzern ergänzt, Stand \(Format.monthYear(info.retrieved))"
    }
}

/// A fact users ticked for a slipway; `present` is false for an explicit "no".
public struct TickedFact: Hashable, Sendable {
    public let field: String
    public let present: Bool
    public let retrieved: Date

    /// "Steg" or "Steg: nein"
    public var label: String {
        let name = Contributions.label(field)
        return present ? name : "\(name): nein"
    }
}

public struct Station: Codable, Hashable, Identifiable, Sendable {
    public let id: String
    public let name: String
    public let number: String?
    public let latitude: Double
    public let longitude: Double
    public let km: Double
    public let agency: String?
    public let water: Water
    public let waterLevel: WaterLevel?
}

public struct WaterDetails: Codable, Hashable, Identifiable, Sendable {
    public let id: String
    public let name: String
    public let shortname: String?
    /// Covers the shape and everything on the water; nil when nothing has coordinates.
    public let bounds: Bounds?
    /// Simplified course or area from OpenStreetMap (ODbL); nil when the water has none.
    public let shape: Geometry?
    public let osmElements: [String]
    public let slipways: [SlipwaySummary]
    public let stations: [Station]
    public let marinas: [Marina]

    /// True when the page shows anything from OpenStreetMap and so needs the attribution.
    public var usesOpenStreetMap: Bool {
        shape != nil || slipways.contains { $0.source == .osm } || marinas.contains { $0.source == .osm }
    }

    /// The gauge with a reading closest to the given point.
    public func nearestGauge(latitude: Double, longitude: Double) -> (station: Station, level: WaterLevel, km: Double)? {
        stations
            .compactMap { station in
                station.waterLevel.map {
                    (station, $0, Distance.km(latitude, longitude, station.latitude, station.longitude))
                }
            }
            .min { $0.2 < $1.2 }
    }
}

/// A GeoJSON geometry reduced to what a map draws: lines for rivers and canals, polygons (outer ring first) for lakes.
public struct Geometry: Codable, Hashable, Sendable {
    public let lines: [[Position]]
    public let polygons: [[[Position]]]

    private enum CodingKeys: String, CodingKey { case type, coordinates, geometries }

    public init(lines: [[Position]], polygons: [[[Position]]]) {
        self.lines = lines
        self.polygons = polygons
    }

    public init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        switch try container.decode(String.self, forKey: .type) {
        case "LineString":
            lines = [Self.ring(try container.decode([[Double]].self, forKey: .coordinates))]
            polygons = []
        case "MultiLineString":
            lines = try container.decode([[[Double]]].self, forKey: .coordinates).map(Self.ring)
            polygons = []
        case "Polygon":
            lines = []
            polygons = [try container.decode([[[Double]]].self, forKey: .coordinates).map(Self.ring)]
        case "MultiPolygon":
            lines = []
            polygons = try container.decode([[[[Double]]]].self, forKey: .coordinates).map { $0.map(Self.ring) }
        case "GeometryCollection":
            let parts = try container.decode([Geometry].self, forKey: .geometries)
            lines = parts.flatMap(\.lines)
            polygons = parts.flatMap(\.polygons)
        default:
            // Points and unknown types have nothing to draw.
            lines = []
            polygons = []
        }
    }

    public func encode(to encoder: Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)
        try container.encode("GeometryCollection", forKey: .type)
        var geometries = container.nestedUnkeyedContainer(forKey: .geometries)
        try geometries.encode(Part(type: "MultiLineString", coordinates: lines.map { $0.map { [$0.longitude, $0.latitude] } }))
        try geometries.encode(Part(type: "MultiPolygon", coordinates: polygons.map { $0.map { $0.map { [$0.longitude, $0.latitude] } } }))
    }

    private struct Part<C: Encodable>: Encodable {
        let type: String
        let coordinates: C
    }

    /// GeoJSON positions are [longitude, latitude].
    private static func ring(_ positions: [[Double]]) -> [Position] {
        positions.compactMap { $0.count >= 2 ? Position(latitude: $0[1], longitude: $0[0]) : nil }
    }
}

/// Water shapes for an overview map (OpenStreetMap, ODbL), from a GeoJSON FeatureCollection whose feature ids are water ids.
public struct WaterShape: Decodable, Hashable, Identifiable, Sendable {
    public let id: String
    public let name: String
    public let geometry: Geometry

    private enum CodingKeys: String, CodingKey { case id, properties, geometry }
    private struct Properties: Decodable { let name: String }

    init(id: String, name: String, geometry: Geometry) {
        self.id = id
        self.name = name
        self.geometry = geometry
    }

    /// The shape with only its parts in `area`; see `Geometry.clipped(to:resolution:)`.
    public func clipped(to area: MapArea, resolution: Double = 0) -> WaterShape {
        WaterShape(id: id, name: name, geometry: geometry.clipped(to: area, resolution: resolution))
    }

    public init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        id = try container.decode(String.self, forKey: .id)
        name = try container.decode(Properties.self, forKey: .properties).name
        geometry = try container.decode(Geometry.self, forKey: .geometry)
    }
}

struct WaterShapeCollection: Decodable {
    let features: [WaterShape]
}

enum Distance {
    /// Haversine distance in kilometres.
    static func km(_ lat1: Double, _ lon1: Double, _ lat2: Double, _ lon2: Double) -> Double {
        let rad = Double.pi / 180
        let dLat = (lat2 - lat1) * rad
        let dLon = (lon2 - lon1) * rad
        let a = sin(dLat / 2) * sin(dLat / 2) + cos(lat1 * rad) * cos(lat2 * rad) * sin(dLon / 2) * sin(dLon / 2)
        return 6371 * 2 * atan2(sqrt(a), sqrt(1 - a))
    }
}

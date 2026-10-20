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
    public let position: Position?
    public let gauge: GaugeReading?

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

    public var address: String {
        [street, Format.place(postalCode, city), country].compactMap { $0 }.joined(separator: ", ")
    }

    /// Our own costs win; imported slipways only have the OpenStreetMap fee tag.
    public var costsLabel: String {
        if costs == 0 || (costs == nil && fee == "no") { return "kostenlos" }
        if let costs { return Format.euros(costs) }
        if let fee { return Labels.fee(fee) }
        return "Kosten unbekannt"
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

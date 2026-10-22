import Foundation
#if canImport(FoundationNetworking)
import FoundationNetworking
#endif

public struct APIError: Error, Equatable, Sendable {
    public let status: Int
}

/// Sends HTTP requests; `URLSession` in the app, a stub in tests.
public protocol HTTPTransport: Sendable {
    func send(_ request: URLRequest) async throws -> (Data, HTTPURLResponse)
}

extension URLSession: HTTPTransport {
    public func send(_ request: URLRequest) async throws -> (Data, HTTPURLResponse) {
        let (data, response) = try await data(for: request)
        guard let http = response as? HTTPURLResponse else { throw URLError(.badServerResponse) }
        return (data, http)
    }
}

/// Read-only client for the Slipways backend (see backend/ in the repository).
public struct SlipwaysAPI: Sendable {
    private let baseURL: URL
    private let transport: HTTPTransport

    public init(baseURL: URL, transport: HTTPTransport = URLSession.shared) {
        self.baseURL = baseURL
        self.transport = transport
    }

    public func slipways(search: String? = nil) async throws -> [SlipwaySummary] {
        let term = search?.trimmingCharacters(in: .whitespacesAndNewlines) ?? ""
        return try await get("api/slipways", query: term.isEmpty ? [] : [URLQueryItem(name: "search", value: term)])
    }

    public func slipway(id: String) async throws -> SlipwayDetails {
        try await get("api/slipways/\(id)")
    }

    public func waters(search: String? = nil) async throws -> [WaterSummary] {
        let term = search?.trimmingCharacters(in: .whitespacesAndNewlines) ?? ""
        return try await get("api/waters", query: term.isEmpty ? [] : [URLQueryItem(name: "search", value: term)])
    }

    public func marinas() async throws -> [Marina] {
        try await get("api/marinas")
    }

    public func water(id: String) async throws -> WaterDetails {
        try await get("api/waters/\(id)")
    }

    public func marina(id: String) async throws -> MarinaDetails {
        try await get("api/marinas/\(id)")
    }

    /// Shapes of the waters a search finds.
    public func waterShapes(search: String) async throws -> [WaterShape] {
        let term = search.trimmingCharacters(in: .whitespacesAndNewlines)
        let collection: WaterShapeCollection = try await get("api/waters/shapes", query: [URLQueryItem(name: "search", value: term)])
        return collection.features
    }

    /// Shapes of the waters reaching into an area that are at least `minExtent` large.
    public func waterShapes(in area: ShapeArea) async throws -> [WaterShape] {
        let collection: WaterShapeCollection = try await get(
            "api/waters/shapes",
            query: [URLQueryItem(name: "bbox", value: area.bbox), URLQueryItem(name: "minExtent", value: ShapeArea.text(area.minExtent))]
        )
        return collection.features
    }

    private func get<T: Decodable>(_ path: String, query: [URLQueryItem] = []) async throws -> T {
        var components = URLComponents(url: baseURL.appendingPathComponent(path), resolvingAgainstBaseURL: false)!
        if !query.isEmpty { components.queryItems = query }
        var request = URLRequest(url: components.url!)
        request.setValue("application/json", forHTTPHeaderField: "Accept")
        let (data, response) = try await transport.send(request)
        guard (200..<300).contains(response.statusCode) else { throw APIError(status: response.statusCode) }
        return try Self.makeDecoder().decode(T.self, from: data)
    }

    static func makeDecoder() -> JSONDecoder {
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .custom { decoder in
            let container = try decoder.singleValueContainer()
            let text = try container.decode(String.self)
            guard let date = parseDate(text) else {
                throw DecodingError.dataCorruptedError(in: container, debugDescription: "Invalid date \(text)")
            }
            return date
        }
        return decoder
    }

    /// ISO 8601 in UTC as .NET writes it, with no or up to seven fractional digits ("2026-10-20T16:26:23.5072912Z").
    static func parseDate(_ text: String) -> Date? {
        var whole = text
        var fraction = 0.0
        if let dot = text.firstIndex(of: "."), let end = text[dot...].firstIndex(where: { !$0.isNumber && $0 != "." }) {
            fraction = Double("0" + text[dot..<end]) ?? 0
            whole = String(text[..<dot] + text[end...])
        }
        let formatter = ISO8601DateFormatter()
        formatter.formatOptions = [.withInternetDateTime]
        return formatter.date(from: whole).map { $0.addingTimeInterval(fraction) }
    }
}

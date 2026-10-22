import Foundation
#if canImport(FoundationNetworking)
import FoundationNetworking
#endif

public struct APIError: Error, Equatable, Sendable {
    public let status: Int
    /// The server's explanation, e.g. why a suggestion was not taken.
    public var message: String? = nil

    /// A German text for people, as the website shows it.
    public var userMessage: String {
        if status == 429 { return "Du hast gerade viele Vorschläge geschickt. Bitte versuch es später noch einmal." }
        return message ?? "Das hat nicht geklappt (Fehler \(status)). Bitte versuch es später noch einmal."
    }
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

/// Client for the Slipways backend (see backend/ in the repository): reads places and sends suggestions.
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

    /// A proof-of-work challenge to solve with `Proof.solve` before sending a suggestion.
    public func challenge() async throws -> ProofChallenge {
        try await get("api/suggestions/challenge")
    }

    /// Sends a suggestion; throws `NearbySlipwaysError` when a new slipway lies close to known ones.
    public func suggest(_ suggestion: SuggestionRequest) async throws -> SuggestionCreated {
        var request = request("api/suggestions")
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.httpBody = try JSONEncoder().encode(suggestion)
        let (data, response) = try await transport.send(request)
        if response.statusCode == 409 {
            struct Nearby: Decodable { let slipways: [NearbySlipway] }
            throw NearbySlipwaysError(slipways: try Self.makeDecoder().decode(Nearby.self, from: data).slipways)
        }
        return try decode(data, response)
    }

    /// The state of a suggestion this device sent.
    public func suggestion(id: String, token: String) async throws -> SuggestionStatus {
        try await get("api/suggestions/\(id)", query: [URLQueryItem(name: "token", value: token)])
    }

    public func withdrawSuggestion(id: String, token: String) async throws {
        var request = request("api/suggestions/\(id)", query: [URLQueryItem(name: "token", value: token)])
        request.httpMethod = "DELETE"
        let (data, response) = try await transport.send(request)
        try Self.check(data, response)
    }

    private func get<T: Decodable>(_ path: String, query: [URLQueryItem] = []) async throws -> T {
        let (data, response) = try await transport.send(request(path, query: query))
        return try decode(data, response)
    }

    private func request(_ path: String, query: [URLQueryItem] = []) -> URLRequest {
        var components = URLComponents(url: baseURL.appendingPathComponent(path), resolvingAgainstBaseURL: false)!
        if !query.isEmpty { components.queryItems = query }
        var request = URLRequest(url: components.url!)
        request.setValue("application/json", forHTTPHeaderField: "Accept")
        return request
    }

    private func decode<T: Decodable>(_ data: Data, _ response: HTTPURLResponse) throws -> T {
        try Self.check(data, response)
        return try Self.makeDecoder().decode(T.self, from: data)
    }

    /// Throws for answers other than 2xx, with the text of a problem details answer (RFC 9457) if there is one.
    private static func check(_ data: Data, _ response: HTTPURLResponse) throws {
        guard !(200..<300).contains(response.statusCode) else { return }
        struct Problem: Decodable {
            let title: String?
            let detail: String?
            let errors: [String: [String]]?
        }
        let problem = try? JSONDecoder().decode(Problem.self, from: data)
        let message = problem?.errors.map { $0.values.flatMap { $0 }.joined(separator: " ") } ?? problem?.detail
        throw APIError(status: response.statusCode, message: message?.isEmpty == false ? message : nil)
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

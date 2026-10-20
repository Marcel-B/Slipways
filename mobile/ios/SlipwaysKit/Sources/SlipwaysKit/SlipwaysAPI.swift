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

    public func waters() async throws -> [Water] {
        try await get("api/waters")
    }

    private func get<T: Decodable>(_ path: String, query: [URLQueryItem] = []) async throws -> T {
        var components = URLComponents(url: baseURL.appendingPathComponent(path), resolvingAgainstBaseURL: false)!
        if !query.isEmpty { components.queryItems = query }
        var request = URLRequest(url: components.url!)
        request.setValue("application/json", forHTTPHeaderField: "Accept")
        let (data, response) = try await transport.send(request)
        guard (200..<300).contains(response.statusCode) else { throw APIError(status: response.statusCode) }
        return try JSONDecoder().decode(T.self, from: data)
    }
}

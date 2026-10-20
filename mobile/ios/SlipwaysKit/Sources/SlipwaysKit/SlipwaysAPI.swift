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

/// Client for the Slipways backend (see backend/ in the repository).
public struct SlipwaysAPI: Sendable {
    private let baseURL: URL
    private let transport: HTTPTransport
    private let token: @Sendable () -> String?

    public init(baseURL: URL, transport: HTTPTransport = URLSession.shared, token: @escaping @Sendable () -> String? = { nil }) {
        self.baseURL = baseURL
        self.transport = transport
        self.token = token
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

    public func login(email: String, password: String) async throws -> AccessToken {
        try decode(try await post("api/account/login", body: Credentials(email: email, password: password)))
    }

    public func register(email: String, password: String) async throws {
        _ = try await post("api/account/register", body: Credentials(email: email, password: password))
    }

    public func me() async throws -> User {
        try await get("api/account/me", authorized: true)
    }

    private struct Credentials: Encodable {
        let email: String
        let password: String
    }

    private func get<T: Decodable>(_ path: String, query: [URLQueryItem] = [], authorized: Bool = false) async throws -> T {
        var components = URLComponents(url: baseURL.appendingPathComponent(path), resolvingAgainstBaseURL: false)!
        if !query.isEmpty { components.queryItems = query }
        var request = URLRequest(url: components.url!)
        if authorized, let token = token() {
            request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        }
        return try decode(try await send(request))
    }

    private func post(_ path: String, body: some Encodable) async throws -> Data {
        var request = URLRequest(url: baseURL.appendingPathComponent(path))
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.httpBody = try JSONEncoder().encode(body)
        return try await send(request)
    }

    private func send(_ request: URLRequest) async throws -> Data {
        var request = request
        request.setValue("application/json", forHTTPHeaderField: "Accept")
        let (data, response) = try await transport.send(request)
        guard (200..<300).contains(response.statusCode) else { throw APIError(status: response.statusCode) }
        return data
    }

    private func decode<T: Decodable>(_ data: Data) throws -> T {
        try JSONDecoder().decode(T.self, from: data)
    }
}

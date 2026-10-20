import Foundation
#if canImport(FoundationNetworking)
import FoundationNetworking
#endif
import Testing
@testable import SlipwaysKit

/// Answers requests by path and records them.
final class StubTransport: HTTPTransport, @unchecked Sendable {
    private let lock = NSLock()
    private var routes: [String: (Int, String)]
    private(set) var requests: [URLRequest] = []

    init(_ routes: [String: (Int, String)]) {
        self.routes = routes
    }

    func send(_ request: URLRequest) async throws -> (Data, HTTPURLResponse) {
        let (status, body) = lock.withLock {
            requests.append(request)
            return routes[request.url!.path] ?? (404, "")
        }
        let response = HTTPURLResponse(url: request.url!, statusCode: status, httpVersion: nil, headerFields: nil)!
        return (Data(body.utf8), response)
    }
}

private let baseURL = URL(string: "http://localhost:5080")!
private let kelheim = """
{"id":"1","name":"Am Jachthafen, Kelheim","latitude":48.9,"longitude":11.9,"street":"Am Jachthafen 13","postalCode":"93309","city":"Kelheim","country":"DE","comment":null,"rating":0,"costs":null,"pro":null,"contra":null,"water":{"id":"w","name":"Donau"},"marina":null,"extras":["Campingplatz"],"created":"2020-02-18T16:14:59Z","updated":null}
"""

@Test func listsSlipwaysWithTrimmedSearchTerm() async throws {
    let transport = StubTransport(["/api/slipways": (200, "[\(kelheim)]")])
    let api = SlipwaysAPI(baseURL: baseURL, transport: transport)

    let slipways = try await api.slipways(search: " Kelheim ")

    #expect(slipways.map(\.subtitle) == ["Donau · 93309 Kelheim"])
    #expect(transport.requests.first?.url?.absoluteString == "http://localhost:5080/api/slipways?search=Kelheim")
}

@Test func decodesDetailsAndIgnoresUnknownFields() async throws {
    let api = SlipwaysAPI(baseURL: baseURL, transport: StubTransport(["/api/slipways/1": (200, kelheim)]))

    let slipway = try await api.slipway(id: "1")

    #expect(slipway.costs == nil)
    #expect(slipway.extras == ["Campingplatz"])
    #expect(slipway.address == "Am Jachthafen 13, 93309 Kelheim, DE")
}

@Test func errorStatusThrowsAPIError() async {
    let api = SlipwaysAPI(baseURL: baseURL, transport: StubTransport([:]))

    await #expect(throws: APIError(status: 404)) {
        try await api.slipway(id: "missing")
    }
}

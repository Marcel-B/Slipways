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

// Shortened answers of https://slipways.de/api from October 2026.
private let kelheim = """
{"id":"1","name":"Am Jachthafen, Kelheim","latitude":48.9,"longitude":11.9,"street":"Am Jachthafen 13","postalCode":"93309","city":"Kelheim","country":"DE","url":null,"comment":null,"rating":0,"costs":null,"pro":null,"contra":null,"operator":null,"fee":null,"access":null,"openingHours":null,"surface":null,"water":{"id":"w","name":"Donau"},"marina":null,"extras":["Campingplatz"],"source":"manual","osmElement":null,"created":"2020-02-18T16:14:59Z","updated":"2026-10-20T16:26:23.5072912Z"}
"""
private let nonnensteg = """
{"id":"2","name":"Nonnensteg","latitude":52.3832808,"longitude":13.0360706,"postalCode":null,"city":null,"country":"DE","water":null,"source":"osm"}
"""
private let importedDetails = """
{"id":"2","name":"Nonnensteg","latitude":52.38,"longitude":13.03,"street":null,"postalCode":null,"city":null,"country":"DE","url":"www.example.org","comment":null,"rating":0,"costs":null,"pro":null,"contra":null,"operator":"Stadt Potsdam","fee":"no","access":"yes","openingHours":"24/7","surface":"concrete","water":null,"marina":null,"extras":[],"source":"osm","osmElement":"node/42","created":"2026-10-20T12:00:00Z","updated":null}
"""
private let bodensee = """
{"id":"b","name":"Bodensee","shortname":"BODENSEE","slipwayCount":158,"stationCount":1,"position":{"latitude":47.67,"longitude":9.31},"gauge":{"stationId":"k","stationName":"KONSTANZ","waterLevel":{"value":248,"measuredAt":"2026-10-20T17:30:00Z","state":"low"}}}
"""
private let bille = """
{"id":"w","name":"Bille","shortname":"BILLE","bounds":{"south":53.48,"west":10.02,"north":53.65,"east":10.51},
 "shape":{"type":"MultiLineString","coordinates":[[[10.46,53.64],[10.47,53.65]],[[10.29,53.54],[10.30,53.55],[10.31,53.56]]]},
 "osmElements":["relation/123"],
 "slipways":[\(nonnensteg)],
 "stations":[
  {"id":"far","name":"FERN","number":"1","latitude":53.0,"longitude":10.0,"km":1,"agency":null,"water":{"id":"w","name":"Bille"},"waterLevel":{"value":100.5,"measuredAt":"2026-10-20T17:30:00Z","state":"normal"}},
  {"id":"near","name":"NAH","number":"2","latitude":52.39,"longitude":13.04,"km":2,"agency":null,"water":{"id":"w","name":"Bille"},"waterLevel":{"value":80,"measuredAt":"2026-10-20T17:30:00Z","state":"high"}},
  {"id":"none","name":"OHNE","number":"3","latitude":52.3832,"longitude":13.0360,"km":3,"agency":null,"water":{"id":"w","name":"Bille"},"waterLevel":null}],
 "marinas":[]}
"""

@Test func listsSlipwaysWithTrimmedSearchTerm() async throws {
    let transport = StubTransport(["/api/slipways": (200, "[\(kelheim)]")])
    let api = SlipwaysAPI(baseURL: baseURL, transport: transport)

    let slipways = try await api.slipways(search: " Kelheim ")

    #expect(slipways.map(\.subtitle) == ["Donau · 93309 Kelheim"])
    #expect(transport.requests.first?.url?.absoluteString == "http://localhost:5080/api/slipways?search=Kelheim")
}

@Test func listsImportedSlipwaysWithoutWater() async throws {
    let api = SlipwaysAPI(baseURL: baseURL, transport: StubTransport(["/api/slipways": (200, "[\(kelheim),\(nonnensteg)]")]))

    let slipways = try await api.slipways()

    #expect(slipways.map(\.source) == [.manual, .osm])
    #expect(slipways[1].water == nil)
    #expect(slipways[1].subtitle == "")
}

@Test func unknownSourceCountsAsManual() async throws {
    let other = nonnensteg.replacingOccurrences(of: "\"osm\"", with: "\"wikidata\"")
    let api = SlipwaysAPI(baseURL: baseURL, transport: StubTransport(["/api/slipways": (200, "[\(other)]")]))

    #expect(try await api.slipways().first?.source == .manual)
}

@Test func decodesDetailsAndIgnoresUnknownFields() async throws {
    let api = SlipwaysAPI(baseURL: baseURL, transport: StubTransport(["/api/slipways/1": (200, kelheim)]))

    let slipway = try await api.slipway(id: "1")

    #expect(slipway.costs == nil)
    #expect(slipway.costsLabel == "Kosten unbekannt")
    #expect(slipway.extras == ["Campingplatz"])
    #expect(slipway.address == "Am Jachthafen 13, 93309 Kelheim, DE")
}

@Test func decodesOpenStreetMapTagsOfImportedSlipway() async throws {
    let api = SlipwaysAPI(baseURL: baseURL, transport: StubTransport(["/api/slipways/2": (200, importedDetails)]))

    let slipway = try await api.slipway(id: "2")

    #expect(slipway.source == .osm)
    #expect(slipway.water == nil)
    #expect(slipway.address == "DE")
    #expect(slipway.costsLabel == "kostenlos")
    #expect(Labels.access(slipway.access!) == "öffentlich")
    #expect(Labels.surface(slipway.surface!) == "Beton")
    #expect(Links.web(slipway.url!)?.absoluteString == "https://www.example.org")
    #expect(Links.osm(slipway.osmElement!)?.absoluteString == "https://www.openstreetmap.org/node/42")
}

@Test func ownCostsWinOverFeeTag() async throws {
    let paid = importedDetails.replacingOccurrences(of: "\"costs\":null", with: "\"costs\":7.5")
    let api = SlipwaysAPI(baseURL: baseURL, transport: StubTransport(["/api/slipways/2": (200, paid)]))

    let slipway = try await api.slipway(id: "2")

    #expect(slipway.costs == Decimal(string: "7.5"))
    #expect(slipway.costsLabel.hasPrefix("7,50"))
}

@Test func listsWatersWithGaugeReading() async throws {
    let transport = StubTransport(["/api/waters": (200, "[\(bodensee)]")])
    let api = SlipwaysAPI(baseURL: baseURL, transport: transport)

    let waters = try await api.waters(search: "Boden")

    let gauge = try #require(waters.first?.gauge)
    #expect(gauge.stationName == "KONSTANZ")
    #expect(Format.waterLevel(gauge.waterLevel) == "248 cm · niedrig")
    #expect(gauge.waterLevel.measuredAt == Date(timeIntervalSince1970: 1_792_517_400))
    #expect(transport.requests.first?.url?.query == "search=Boden")
}

@Test func decodesWaterDetailsWithShapeAndNearestGauge() async throws {
    let api = SlipwaysAPI(baseURL: baseURL, transport: StubTransport(["/api/waters/w": (200, bille)]))

    let water = try await api.water(id: "w")

    let shape = try #require(water.shape)
    #expect(shape.lines.map(\.count) == [2, 3])
    #expect(shape.lines[0][0] == Position(latitude: 53.64, longitude: 10.46))
    #expect(water.usesOpenStreetMap)
    let nearest = try #require(water.nearestGauge(latitude: 52.3832808, longitude: 13.0360706))
    #expect(nearest.station.name == "NAH")
    #expect(Format.kilometres(nearest.km) == "0,8 km")
    #expect(Format.waterLevel(water.stations[0].waterLevel!) == "100,5 cm · normal")
}

@Test func decodesPolygonsAndIgnoresPoints() throws {
    let decoder = SlipwaysAPI.makeDecoder()
    let lake = #"{"type":"MultiPolygon","coordinates":[[[[9.1,47.6],[9.2,47.7],[9.3,47.6],[9.1,47.6]]]]}"#
    let point = #"{"type":"Point","coordinates":[9.1,47.6]}"#

    let polygons = try decoder.decode(Geometry.self, from: Data(lake.utf8)).polygons
    #expect(polygons.count == 1)
    #expect(polygons[0][0].count == 4)
    #expect(try decoder.decode(Geometry.self, from: Data(point.utf8)) == decoder.decode(Geometry.self, from: Data("{\"type\":\"GeometryCollection\",\"geometries\":[]}".utf8)))
}

@Test func parsesDotNetTimestamps() {
    #expect(SlipwaysAPI.parseDate("2026-10-20T16:26:23Z") == Date(timeIntervalSince1970: 1_792_513_583))
    #expect(SlipwaysAPI.parseDate("2026-10-20T16:26:23.5Z") == Date(timeIntervalSince1970: 1_792_513_583.5))
    #expect(abs(SlipwaysAPI.parseDate("2026-10-20T16:26:23.5072912Z")!.timeIntervalSince1970 - 1_792_513_583.5072912) < 0.001)
    #expect(SlipwaysAPI.parseDate("gestern") == nil)
}

@Test func errorStatusThrowsAPIError() async {
    let api = SlipwaysAPI(baseURL: baseURL, transport: StubTransport([:]))

    await #expect(throws: APIError(status: 404)) {
        try await api.slipway(id: "missing")
    }
}

private let marina = """
{"id":"m","name":"Yachthafen Kelheim","latitude":48.91,"longitude":11.88,"url":"www.example.org","street":null,"postalCode":"93309","city":"Kelheim","phone":null,"email":null,"operator":null,"fee":null,"access":null,"openingHours":null,"water":null,"source":"osm","osmElement":"node/7"}
"""

@Test func listsMarinas() async throws {
    let api = SlipwaysAPI(baseURL: baseURL, transport: StubTransport(["/api/marinas": (200, "[\(marina)]")]))

    let marinas = try await api.marinas()

    #expect(marinas.map(\.name) == ["Yachthafen Kelheim"])
    #expect(Overview.matches(marinas[0], search: " kelheim"))
    #expect(Overview.matches(marinas[0], search: "YACHT"))
    #expect(!Overview.matches(marinas[0], search: "Donau"))
}

@Test func overviewListsOnlyEntriesInAreaUpToLimit() {
    let points = [(latitude: 48.9, longitude: 11.9), (latitude: 52.4, longitude: 13.0), (latitude: 48.95, longitude: 11.85)]
    let kelheim = MapArea(centerLatitude: 48.9, centerLongitude: 11.9, latitudeDelta: 0.2, longitudeDelta: 0.2)

    #expect(Overview.visible(points, in: kelheim) { $0 }?.count == 2)
    #expect(Overview.visible(points, in: kelheim, limit: 1) { $0 } == nil)
    #expect(Overview.visible(points, in: .start, limit: 2) { $0 } == nil)
    #expect(Overview.visible(points, in: .start) { $0 }?.count == 3)
}

@Test func fitsAreaAroundPoints() throws {
    let area = try #require(MapArea.fitting([(latitude: 48.0, longitude: 11.0), (latitude: 49.0, longitude: 12.0)]))
    #expect(area.contains(latitude: 48.0, longitude: 11.0))
    #expect(area.contains(latitude: 49.0, longitude: 12.0))
    #expect(abs(area.latitudeDelta - 1.3) < 0.0001)

    let single = try #require(MapArea.fitting([(latitude: 48.0, longitude: 11.0)]))
    #expect(abs(single.latitudeDelta - 0.02) < 0.0001)
    #expect(MapArea.fitting([]) == nil)
}

@Test func clustersEntriesInGridCells() throws {
    let points = [(latitude: 48.90, longitude: 11.90), (latitude: 48.91, longitude: 11.91), (latitude: 52.4, longitude: 13.0), (latitude: 60.0, longitude: 11.9)]

    let clusters = Overview.clusters(points, in: .start) { $0 }

    #expect(clusters.map(\.items.count).sorted() == [1, 2])
    let kelheim = try #require(clusters.first { $0.items.count == 2 })
    #expect(abs(kelheim.latitude - 48.905) < 0.0001)
    #expect(Overview.clusters(points, in: .start) { $0 }.map(\.id) == clusters.map(\.id))
}

@Test func linksPhoneAndMail() {
    #expect(Links.phone("+49 9441 12-34")?.absoluteString == "tel:+4994411234")
    #expect(Links.phone("unbekannt") == nil)
    #expect(Links.mail(" hafen@example.org ")?.absoluteString == "mailto:hafen@example.org")
}

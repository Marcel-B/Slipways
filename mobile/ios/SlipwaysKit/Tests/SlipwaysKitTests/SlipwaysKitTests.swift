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
{"id":"1","name":"Am Jachthafen, Kelheim","latitude":48.9,"longitude":11.9,"street":"Am Jachthafen 13","postalCode":"93309","city":"Kelheim","country":"DE","url":null,"comment":null,"rating":0,"costs":null,"pro":null,"contra":null,"operator":null,"fee":null,"access":null,"openingHours":null,"surface":null,"water":{"id":"w","name":"Donau"},"marina":null,"extras":["Campingplatz"],"source":"manual","osmElement":null,"created":"2020-02-18T16:14:59Z","updated":"2026-10-20T16:26:23.5072912Z","infos":[]}
"""
private let nonnensteg = """
{"id":"2","name":"Nonnensteg","latitude":52.3832808,"longitude":13.0360706,"postalCode":null,"city":null,"country":"DE","water":null,"source":"osm"}
"""
private let importedDetails = """
{"id":"2","name":"Nonnensteg","latitude":52.38,"longitude":13.03,"street":null,"postalCode":null,"city":null,"country":"DE","url":"www.example.org","comment":null,"rating":0,"costs":null,"pro":null,"contra":null,"operator":"Stadt Potsdam","fee":"no","access":"yes","openingHours":"24/7","surface":"concrete","water":null,"marina":null,"extras":[],"source":"osm","osmElement":"node/42","created":"2026-10-20T12:00:00Z","updated":null,"infos":[]}
"""
private let bodensee = """
{"id":"b","name":"Bodensee","shortname":"BODENSEE","slipwayCount":158,"stationCount":1,"position":{"latitude":47.67,"longitude":9.31},"gauge":{"stationId":"k","stationName":"KONSTANZ","waterLevel":{"value":248,"measuredAt":"2026-10-20T17:30:00Z","state":"low"}},"countries":["DE","CH","AT"]}
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
{"id":"m","name":"Yachthafen Kelheim","latitude":48.91,"longitude":11.88,"url":"www.example.org","street":null,"postalCode":"93309","city":"Kelheim","phone":null,"email":null,"operator":null,"fee":null,"access":null,"openingHours":null,"water":null,"source":"osm","osmElement":"node/7","country":"DE"}
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

    let germany = MapArea(south: 46.0, west: 4.5, north: 55.5, east: 16.5)
    let clusters = Overview.clusters(points, in: germany) { $0 }

    #expect(clusters.map(\.items.count).sorted() == [1, 2])
    let kelheim = try #require(clusters.first { $0.items.count == 2 })
    #expect(abs(kelheim.latitude - 48.905) < 0.0001)
    #expect(Overview.clusters(points, in: germany) { $0 }.map(\.id) == clusters.map(\.id))
}

@Test func linksPhoneAndMail() {
    #expect(Links.phone("+49 9441 12-34")?.absoluteString == "tel:+4994411234")
    #expect(Links.phone("unbekannt") == nil)
    #expect(Links.mail(" hafen@example.org ")?.absoluteString == "mailto:hafen@example.org")
}

// Values from users and the marina's website, as /api/slipways/{id} and /api/marinas/{id} send them.
private let withInfos = importedDetails.replacingOccurrences(of: "\"infos\":[]", with: """
"infos":[{"field":"fee","value":"yes","source":"user","sourceUrl":null,"retrieved":"2026-10-21T10:00:00Z"},
 {"field":"costs","value":"5.5","source":"user","sourceUrl":null,"retrieved":"2026-10-21T10:00:00Z"},
 {"field":"jetty","value":"yes","source":"user","sourceUrl":null,"retrieved":"2026-10-21T10:00:00Z"},
 {"field":"crane","value":"no","source":"user","sourceUrl":null,"retrieved":"2026-10-21T10:00:00Z"},
 {"field":"tip","value":"Früh kommen.","source":"user","sourceUrl":null,"retrieved":"2026-10-21T10:00:00Z"},
 {"field":"surface","value":"gravel","source":"user","sourceUrl":null,"retrieved":"2026-09-01T10:00:00Z"},
 {"field":"surface","value":"wood","source":"user","sourceUrl":null,"retrieved":"2026-10-01T10:00:00Z"}]
""")
private let marinaDetails = """
{"id":"m","name":"Yachthafen Kelheim","latitude":48.91,"longitude":11.88,"url":"www.example.org","street":"Hafen 1","postalCode":"93309","city":"Kelheim","phone":null,"email":"hafen@example.org","operator":null,"fee":null,"access":null,"openingHours":null,"water":{"id":"w","name":"Donau"},"source":"manual","osmElement":null,
 "slipways":[\(nonnensteg)],
 "infos":[{"field":"phone","value":"+49 9441 1234","source":"website","sourceUrl":"https://www.example.org/kontakt","retrieved":"2026-10-20T12:00:00Z"},
  {"field":"email","value":"info@example.org","source":"website","sourceUrl":"https://www.example.org","retrieved":"2026-10-20T12:00:00Z"},
  {"field":"opening_hours","value":"Mo–Fr 8–18 Uhr","source":"user","sourceUrl":null,"retrieved":"2026-10-21T12:00:00Z"}]}
"""

@Test func valuesFromUsersWinOverOwnOnes() async throws {
    let api = SlipwaysAPI(baseURL: baseURL, transport: StubTransport(["/api/slipways/2": (200, withInfos)]))

    let slipway = try await api.slipway(id: "2")

    #expect(slipway.costsLabel.hasPrefix("5,50"))
    #expect(slipway.value("surface")?.value == "wood")
    #expect(slipway.value("surface")?.note == "von Nutzern ergänzt, Stand 10.2026")
    #expect(slipway.value("access")?.value == "yes")
    #expect(slipway.value("access")?.note == nil)
    #expect(slipway.ticked.map(\.label) == ["Steg", "Kran: nein"])
    #expect(slipway.tips.map(\.value) == ["Früh kommen."])
}

@Test func marinaDetailsFillGapsFromWebsite() async throws {
    let transport = StubTransport(["/api/marinas/m": (200, marinaDetails)])
    let api = SlipwaysAPI(baseURL: baseURL, transport: transport)

    let marina = try await api.marina(id: "m")

    #expect(marina.value("phone")?.value == "+49 9441 1234")
    #expect(marina.value("phone")?.note == "laut Webseite, Stand 20.10.2026")
    #expect(marina.value("email")?.value == "hafen@example.org")
    #expect(marina.value("opening_hours")?.note == "von Nutzern ergänzt, Stand 10.2026")
    #expect(marina.value("fee") == nil)
    #expect(marina.slipways.map(\.name) == ["Nonnensteg"])
    #expect(marina.usesOpenStreetMap)
}

@Test func groupsPlacesByCountry() throws {
    let points: [(latitude: Double, longitude: Double, country: String?)] = [
        (48.9, 11.9, "DE"), (52.4, 13.0, "DE"), (50.0, 8.0, "DE"), (52.1, 5.1, "NL"), (47.0, 9.0, nil),
    ]

    let groups = Countries.group(points, country: \.country) { ($0.latitude, $0.longitude) }

    #expect(groups.map(\.code) == ["DE", "NL"])
    #expect(groups[0].items.count == 3)
    #expect(groups[0].latitude == 50.0)
    #expect(groups[0].longitude == 11.9)
    #expect(groups[0].area.contains(latitude: 52.4, longitude: 8.0))
    #expect(groups[1].name == "Niederlande")
    #expect(Countries.counts([["DE", "AT"], ["AT"], []]).map(\.code) == ["AT", "DE"])
}

@Test func zoomMatchesWebsite() {
    // 360° on 256 points is zoom 0; the website starts at zoom 4 with Europe on screen.
    #expect(MapArea(south: -10, west: -180, north: 10, east: 180).zoom(width: 256) == 0)
    #expect(abs(MapArea(south: 40, west: 0, north: 50, east: 22.5).zoom(width: 256) - 4) < 0.0001)
    #expect(Overview.showsCountries(zoom: 5.9))
    #expect(!Overview.showsCountries(zoom: 6))
}

@Test func shapeAreaSnapsToTiles() async throws {
    let area = ShapeArea(MapArea(south: 48.2, west: 11.3, north: 49.1, east: 12.4), zoom: 8.4)
    #expect(area.bbox == "9.8438,46.4063,14.0625,50.625")
    #expect(area.minExtent == 0.1094)
    #expect(ShapeArea(MapArea(south: 48.2, west: 11.3, north: 48.21, east: 11.31), zoom: 13).minExtent == 0)

    let shapes = """
    {"type":"FeatureCollection","features":[{"type":"Feature","id":"w","properties":{"name":"Bille"},"geometry":{"type":"LineString","coordinates":[[10.46,53.64],[10.47,53.65]]}}]}
    """
    let transport = StubTransport(["/api/waters/shapes": (200, shapes)])
    let found = try await SlipwaysAPI(baseURL: baseURL, transport: transport).waterShapes(in: area)
    #expect(found.map(\.name) == ["Bille"])
    #expect(found[0].geometry.lines[0].count == 2)
    #expect(transport.requests.first?.url?.query == "bbox=9.8438,46.4063,14.0625,50.625&minExtent=0.1094")
}

@Test func tapHitsNearestShape() throws {
    let json = """
    [{"id":"river","properties":{"name":"Fluss"},"geometry":{"type":"LineString","coordinates":[[10.0,50.0],[11.0,50.0]]}},
     {"id":"lake","properties":{"name":"See"},"geometry":{"type":"Polygon","coordinates":[[[10.0,51.0],[10.2,51.0],[10.2,51.2],[10.0,51.2],[10.0,51.0]]]}}]
    """
    let shapes = try SlipwaysAPI.makeDecoder().decode([WaterShape].self, from: Data(json.utf8))

    #expect(WaterShape.hit(shapes, latitude: 50.01, longitude: 10.5, tolerance: 0.02)?.id == "river")
    #expect(WaterShape.hit(shapes, latitude: 50.05, longitude: 10.5, tolerance: 0.02) == nil)
    #expect(WaterShape.hit(shapes, latitude: 51.1, longitude: 10.1, tolerance: 0.001)?.id == "lake")
    #expect(WaterShape.hit(shapes, latitude: 51.1, longitude: 10.3, tolerance: 0.001) == nil)
}

// MARK: - Suggestions

private func hex(_ bytes: [UInt8]) -> String { bytes.map { String(format: "%02x", $0) }.joined() }

@Test func hashesLikeTheServer() {
    #expect(hex(Proof.sha256(Array("abc".utf8))) == "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")
    // Longer than one block of 64 bytes.
    let long = Array(String(repeating: "a", count: 100).utf8)
    #expect(hex(Proof.sha256(long)) == "2816597888e4a0d3a36b82b83316ab32680eb8f00f8cd3b904d681246d285a0e")
}

@Test func solvesProofOfWork() async throws {
    let salt = "0a1b2c?expires=1792700000"
    let challenge = ProofChallenge(
        algorithm: "SHA-256", challenge: hex(Proof.sha256(Array("\(salt)4711".utf8))), maxNumber: 10_000,
        salt: salt, signature: "sig")

    let solution = try await Proof.solve(challenge)

    #expect(solution.number == 4711)
    #expect(solution.salt == salt && solution.signature == "sig" && solution.challenge == challenge.challenge)
}

@Test func sendsSuggestionAsJson() async throws {
    let transport = StubTransport(["/api/suggestions": (202, #"{"id":"s1","token":"t1"}"#)])
    let api = SlipwaysAPI(baseURL: baseURL, transport: transport)
    var request = SuggestionRequest(kind: .edit)
    request.placeType = .slipway
    request.placeId = "1"
    request.fields = ["fee": "no"]

    let created = try await api.suggest(request)

    #expect(created == SuggestionCreated(id: "s1", token: "t1"))
    let sent = try #require(transport.requests.first)
    #expect(sent.httpMethod == "POST")
    let body = try JSONSerialization.jsonObject(with: try #require(sent.httpBody)) as? [String: Any]
    #expect(body?["kind"] as? String == "edit")
    #expect(body?["placeType"] as? String == "slipway")
    #expect((body?["fields"] as? [String: String]) == ["fee": "no"])
    #expect(body?["name"] == nil)
}

@Test func reportsNearbySlipways() async throws {
    let nearby = #"{"slipways":[{"id":"2","name":"Nonnensteg","distance":120,"source":"osm"}]}"#
    let api = SlipwaysAPI(baseURL: baseURL, transport: StubTransport(["/api/suggestions": (409, nearby)]))

    do {
        _ = try await api.suggest(SuggestionRequest(kind: .newSlipway))
        Issue.record("expected NearbySlipwaysError")
    } catch let error as NearbySlipwaysError {
        #expect(error.slipways.map(\.name) == ["Nonnensteg"])
        #expect(error.slipways.first?.source == .osm)
    }
}

@Test func showsTheServersReasonForRejectedSuggestions() async throws {
    let problem = #"{"title":"Bad Request","status":400,"detail":"Der Wert für „costs“ ist nicht gültig."}"#
    let api = SlipwaysAPI(baseURL: baseURL, transport: StubTransport(["/api/suggestions": (400, problem)]))

    await #expect(throws: APIError(status: 400, message: "Der Wert für „costs“ ist nicht gültig.")) {
        try await api.suggest(SuggestionRequest(kind: .edit))
    }
    #expect(APIError(status: 429).userMessage.hasPrefix("Du hast gerade viele Vorschläge"))
}

@Test func readsAndWithdrawsSuggestion() async throws {
    let status = #"{"id":"s1","kind":"new_slipway","status":"pending","created":"2026-10-22T10:00:00Z","decided":null}"#
    let transport = StubTransport(["/api/suggestions/s1": (200, status)])
    let api = SlipwaysAPI(baseURL: baseURL, transport: transport)

    let state = try await api.suggestion(id: "s1", token: "a b")
    try await api.withdrawSuggestion(id: "s1", token: "a b")

    #expect(state.kind == .newSlipway && state.status == .pending && state.decided == nil)
    #expect(transport.requests[0].url?.query == "token=a%20b")
    #expect(transport.requests[1].httpMethod == "DELETE")
}

@Test func remembersSentSuggestions() throws {
    let defaults = try #require(UserDefaults(suiteName: "slipways-tests-\(UUID())"))
    let store = SuggestionStore(defaults: defaults)
    let now = Date()
    let day = 86_400.0
    func sent(_ id: String, fields: [String], daysAgo: Double) -> SentSuggestion {
        SentSuggestion(
            created: SuggestionCreated(id: id, token: "t"), kind: .edit, placeType: .slipway, placeId: "1",
            name: "Kelheim", fields: fields, sent: now.addingTimeInterval(-daysAgo * day))
    }
    store.remember(sent("old", fields: ["fee"], daysAgo: 100))
    store.remember(sent("month", fields: ["surface"], daysAgo: 40))
    store.remember(sent("new", fields: ["access", "opening_hours"], daysAgo: 1))

    #expect(store.all(now: now).map(\.id) == ["new", "month"])
    #expect(store.pendingFields(.slipway, "1", changed: nil, now: now) == ["access", "opening_hours"])
    #expect(store.pendingFields(.slipway, "1", changed: now, now: now).isEmpty)
    #expect(store.pendingFields(.marina, "1", changed: nil, now: now).isEmpty)

    store.forget("new")
    #expect(store.all(now: now).map(\.id) == ["month"])
}

@Test func sendsOnlyChangedValues() {
    let changed = Contributions.changed(
        ["fee": "no", "access": "yes", "opening_hours": " ", "surface": "gravel"],
        from: ["fee": "no", "access": "private"])

    #expect(changed == ["access": "yes", "surface": "gravel"])
    #expect(Contributions.fields["access"]?.label(of: "customers") == "nur für Kunden")
    #expect(Contributions.osmEditURL("way/7")?.absoluteString == "https://www.openstreetmap.org/edit?way=7")
}

// MARK: - Clipping

private func p(_ latitude: Double, _ longitude: Double) -> Position { Position(latitude: latitude, longitude: longitude) }

@Test func clipsLinesToTheArea() {
    let area = MapArea(south: 0, west: 0, north: 10, east: 10)
    // In, out over the top, back in, then a segment that only crosses a corner region outside.
    let river = Geometry(lines: [[p(5, -5), p(5, 5), p(15, 5), p(5, 8), p(-5, 20)]], polygons: [])

    let clipped = river.clipped(to: area)

    #expect(clipped.lines == [[p(5, 0), p(5, 5), p(10, 5)], [p(10, 6.5), p(5, 8), p(3.3333333333333335, 10)]])
    #expect(Geometry(lines: [[p(20, 20), p(30, 30)]], polygons: []).clipped(to: area).isEmpty)
}

@Test func clipsAreasToTheArea() {
    let area = MapArea(south: 0, west: 0, north: 10, east: 10)
    let lake = Geometry(lines: [], polygons: [[[p(-5, -5), p(-5, 5), p(5, 5), p(5, -5), p(-5, -5)]]])

    let clipped = lake.clipped(to: area)

    #expect(Set(clipped.polygons.first?.first ?? []) == [p(0, 0), p(0, 5), p(5, 5), p(5, 0)])
    #expect(Geometry(lines: [], polygons: [[[p(20, 20), p(20, 30), p(30, 30)]]]).clipped(to: area).isEmpty)
}

@Test func thinsPointsBelowResolution() {
    let line = Geometry(lines: [[p(0, 0), p(0, 0.001), p(0, 0.002), p(0, 1), p(0, 1.0001)]], polygons: [])

    let thinned = line.clipped(to: MapArea(south: -1, west: -1, north: 1, east: 2), resolution: 0.01)

    #expect(thinned.lines == [[p(0, 0), p(0, 1), p(0, 1.0001)]])
    #expect(MapArea(south: 0, west: 0, north: 2, east: 4).expanded(by: 0.5) == MapArea(south: -1, west: -2, north: 3, east: 6))
}

@Test func flagFromCountryCode() {
    #expect(Countries.flag("DE") == "🇩🇪")
    #expect(Countries.flag("nl") == "🇳🇱")
    #expect(Countries.flag("DEU") == nil)
    #expect(Countries.flag("1A") == nil)
}

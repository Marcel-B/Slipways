package com.bvelop.slipways.shared

import io.ktor.client.HttpClient
import io.ktor.client.engine.mock.MockEngine
import io.ktor.client.engine.mock.MockRequestHandleScope
import io.ktor.client.engine.mock.respond
import io.ktor.client.request.HttpRequestData
import io.ktor.client.request.HttpResponseData
import io.ktor.http.HttpHeaders
import io.ktor.http.HttpStatusCode
import io.ktor.http.headersOf
import kotlinx.coroutines.test.runTest
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertFailsWith
import kotlin.test.assertFalse
import kotlin.test.assertNotNull
import kotlin.test.assertNull
import kotlin.test.assertTrue
import java.math.BigDecimal
import java.time.ZoneId

class SlipwaysApiTest {
    private val requests = mutableListOf<HttpRequestData>()

    private fun api(handler: suspend MockRequestHandleScope.(HttpRequestData) -> HttpResponseData) = SlipwaysApi(
        baseUrl = "http://localhost:5080",
        httpClient = HttpClient(MockEngine { request -> requests += request; handler(request) }),
    )

    private fun MockRequestHandleScope.json(body: String, status: HttpStatusCode = HttpStatusCode.OK) =
        respond(body, status, headersOf(HttpHeaders.ContentType, "application/json"))

    @Test
    fun `lists slipways with search term`() = runTest {
        val api = api { json("[$KELHEIM]") }

        val slipways = api.slipways(search = " Kelheim ")

        assertEquals("Donau · 93309 Kelheim", slipways.single().subtitle)
        assertEquals("http://localhost:5080/api/slipways?search=Kelheim", requests.single().url.toString())
    }

    @Test
    fun `lists imported slipways without water`() = runTest {
        val api = api { json("[$KELHEIM,$NONNENSTEG]") }

        val slipways = api.slipways()

        assertEquals(listOf(Source.MANUAL, Source.OSM), slipways.map { it.source })
        assertNull(slipways[1].water)
        assertEquals("", slipways[1].subtitle)
    }

    @Test
    fun `unknown source counts as manual`() = runTest {
        val api = api { json("[${NONNENSTEG.replace("\"osm\"", "\"wikidata\"")}]") }

        assertEquals(Source.MANUAL, api.slipways().single().source)
    }

    @Test
    fun `reads slipway details and ignores unknown fields`() = runTest {
        val api = api { json(KELHEIM) }

        val slipway = api.slipway("1")

        assertNull(slipway.costs)
        assertEquals("Kosten unbekannt", slipway.costsLabel)
        assertEquals(listOf("Campingplatz"), slipway.extras)
        assertEquals("Am Jachthafen 13, 93309 Kelheim, DE", slipway.address)
    }

    @Test
    fun `reads OpenStreetMap tags of imported slipway`() = runTest {
        val api = api { json(IMPORTED_DETAILS) }

        val slipway = api.slipway("2")

        assertEquals(Source.OSM, slipway.source)
        assertNull(slipway.water)
        assertEquals("DE", slipway.address)
        assertEquals("kostenlos", slipway.costsLabel)
        assertEquals("öffentlich", Labels.access(slipway.access!!))
        assertEquals("Beton", Labels.surface(slipway.surface!!))
        assertEquals("https://www.example.org", Links.web(slipway.url!!))
        assertEquals("https://www.openstreetmap.org/node/42", Links.osm(slipway.osmElement!!))
    }

    @Test
    fun `own costs win over fee tag`() = runTest {
        val api = api { json(IMPORTED_DETAILS.replace("\"costs\":null", "\"costs\":7.5")) }

        val slipway = api.slipway("2")

        assertEquals(BigDecimal("7.5"), slipway.costs)
        assertEquals("7,50 €", slipway.costsLabel.replace('\u00A0', ' '))
    }

    @Test
    fun `lists waters with gauge reading`() = runTest {
        val api = api { json("[$BODENSEE]") }

        val waters = api.waters(search = "Boden")

        val gauge = assertNotNull(waters.single().gauge)
        assertEquals("KONSTANZ", gauge.stationName)
        assertEquals("248 cm · niedrig", Format.waterLevel(gauge.waterLevel))
        assertEquals("20.10.26, 19:30", Format.measuredAt(gauge.waterLevel, ZoneId.of("Europe/Berlin")))
        assertEquals("http://localhost:5080/api/waters?search=Boden", requests.single().url.toString())
    }

    @Test
    fun `reads water details and finds nearest gauge`() = runTest {
        val api = api { json(BILLE) }

        val water = api.water("w")

        assertTrue(water.usesOpenStreetMap)
        val nearest = assertNotNull(water.nearestGauge(52.3832808, 13.0360706))
        assertEquals("NAH", nearest.station.name)
        assertEquals("0,8 km", Format.kilometres(nearest.km))
        assertEquals("100,5 cm · normal", Format.waterLevel(water.stations[0].waterLevel!!))
        assertEquals("http://localhost:5080/api/waters/w", requests.single().url.toString())
    }

    @Test
    fun `lists marinas and matches search`() = runTest {
        val api = api { json("[$MARINA]") }

        val marinas = api.marinas()

        assertEquals("http://localhost:5080/api/marinas", requests.single().url.toString())
        assertTrue(Overview.matches(marinas.single(), " kelheim"))
        assertTrue(Overview.matches(marinas.single(), "YÁCHT"))
        assertFalse(Overview.matches(marinas.single(), "Donau"))
        assertEquals("93309 Kelheim", marinas.single().address)
    }

    @Test
    fun `links phone and mail`() {
        assertEquals("tel:+4994411234", Links.phone("+49 9441 12-34"))
        assertNull(Links.phone("unbekannt"))
        assertEquals("mailto:hafen@example.org", Links.mail(" hafen@example.org "))
    }

    @Test
    fun `overview lists only entries in area up to limit`() {
        val points = listOf(Position(48.9, 11.9), Position(52.4, 13.0), Position(48.95, 11.85))
        val kelheim = MapArea(48.8, 11.8, 49.0, 12.0)

        assertEquals(2, Overview.visible(points, kelheim) { it }?.size)
        assertNull(Overview.visible(points, kelheim, limit = 1) { it })
        assertNull(Overview.visible(points, MapArea.START, limit = 2) { it })
        assertEquals(3, Overview.visible(points, MapArea.START) { it }?.size)
    }

    @Test
    fun `fits area around points`() {
        val area = assertNotNull(MapArea.fitting(listOf(Position(48.0, 11.0), Position(49.0, 12.0))))
        assertTrue(area.contains(48.0, 11.0))
        assertTrue(area.contains(49.0, 12.0))
        assertEquals(1.3, area.north - area.south, 0.0001)

        val single = assertNotNull(MapArea.fitting(listOf(Position(48.0, 11.0))))
        assertEquals(0.02, single.north - single.south, 0.0001)
        assertNull(MapArea.fitting(emptyList()))
    }

    @Test
    fun `turns error status into ApiException`() = runTest {
        val api = api { json("", HttpStatusCode.NotFound) }

        val error = assertFailsWith<ApiException> { api.slipway("missing") }

        assertEquals(404, error.status)
    }

    @Test
    fun `values from users win over own ones`() = runTest {
        val api = api { json(WITH_INFOS) }

        val slipway = api.slipway("2")

        assertEquals("5,50 €", slipway.costsLabel.replace('\u00A0', ' '))
        assertEquals("wood", slipway.value("surface")?.value)
        assertEquals("von Nutzern ergänzt, Stand 10.2026", slipway.value("surface")?.note)
        assertEquals("yes", slipway.value("access")?.value)
        assertNull(slipway.value("access")?.note)
        assertEquals(listOf("Steg", "Kran: nein"), slipway.ticked.map { it.label })
        assertEquals(listOf("Früh kommen."), slipway.tips.map { it.value })
    }

    @Test
    fun `marina details fill gaps from website`() = runTest {
        val api = api { json(MARINA_DETAILS) }

        val marina = api.marina("m")

        assertEquals("http://localhost:5080/api/marinas/m", requests.single().url.toString())
        assertEquals("+49 9441 1234", marina.value("phone")?.value)
        assertEquals("laut Webseite, Stand 20.10.2026", marina.value("phone")?.note)
        assertEquals("hafen@example.org", marina.value("email")?.value)
        assertEquals("von Nutzern ergänzt, Stand 10.2026", marina.value("opening_hours")?.note)
        assertNull(marina.value("fee"))
        assertEquals(listOf("Nonnensteg"), marina.slipways.map { it.name })
        assertTrue(marina.usesOpenStreetMap)
    }

    @Test
    fun `groups places by country`() {
        val points = listOf(
            Triple(48.9, 11.9, "DE"), Triple(52.4, 13.0, "DE"), Triple(50.0, 8.0, "DE"), Triple(52.1, 5.1, "NL"), Triple(47.0, 9.0, null),
        )

        val groups = Countries.group(points, { it.third }) { Position(it.first, it.second) }

        assertEquals(listOf("DE", "NL"), groups.map { it.code })
        assertEquals(3, groups[0].items.size)
        assertEquals(Position(50.0, 11.9), groups[0].center)
        assertTrue(groups[0].area.contains(52.4, 8.0))
        assertEquals("Niederlande", groups[1].name)
        assertEquals(listOf("AT", "DE"), Countries.counts(listOf(listOf("DE", "AT"), listOf("AT"), emptyList())).map { it.code })
        assertEquals("Frankreich (1.234)", CountryCount("FR", 1234).label)
    }

    @Test
    fun `shape area snaps to tiles like the website`() = runTest {
        val area = ShapeArea.of(MapArea(48.2, 11.3, 49.1, 12.4), zoom = 8.4)
        assertEquals("9.8438,46.4063,14.0625,50.625", area.bbox)
        assertEquals(0.1094, area.minExtent)
        assertEquals(0.0, ShapeArea.of(MapArea(48.2, 11.3, 48.21, 11.31), zoom = 13.0).minExtent)

        val api = api {
            json("""{"type":"FeatureCollection","features":[{"type":"Feature","id":"w","properties":{"name":"Bille"},"geometry":{"type":"LineString","coordinates":[[10.46,53.64],[10.47,53.65]]}}]}""")
        }
        val shapes = api.waterShapes(area)

        assertEquals("bbox=9.8438%2C46.4063%2C14.0625%2C50.625&minExtent=0.1094", requests.single().url.encodedQuery)
        assertEquals(listOf("Bille"), shapes.features.map { it.properties.name })
        val geoJson = shapes.toGeoJson(setOf("w"))
        assertTrue(""""id":"w"""" in geoJson)
        assertTrue(""""LineString"""" in geoJson)
        assertFalse("Bille" in shapes.toGeoJson(emptySet()))
    }

    @Test
    fun `reads countries of waters and marinas`() = runTest {
        assertEquals(listOf("DE", "CH", "AT"), api { json("[$BODENSEE]") }.waters().single().countries)
        assertEquals("DE", api { json("[$MARINA]") }.marinas().single().country)
        assertTrue(api { json(BILLE) }.water("w").shape != null)
    }

    private companion object {
        // Shortened answers of https://slipways.de/api from October 2026.
        const val KELHEIM = """{"id":"1","name":"Am Jachthafen, Kelheim","latitude":48.9,"longitude":11.9,"street":"Am Jachthafen 13","postalCode":"93309","city":"Kelheim","country":"DE","url":null,"comment":null,"rating":0,"costs":null,"pro":null,"contra":null,"operator":null,"fee":null,"access":null,"openingHours":null,"surface":null,"water":{"id":"w","name":"Donau"},"marina":null,"extras":["Campingplatz"],"source":"manual","osmElement":null,"created":"2020-02-18T16:14:59Z","updated":"2026-10-20T16:26:23.5072912Z"}"""
        const val NONNENSTEG = """{"id":"2","name":"Nonnensteg","latitude":52.3832808,"longitude":13.0360706,"postalCode":null,"city":null,"country":"DE","water":null,"source":"osm"}"""
        const val IMPORTED_DETAILS = """{"id":"2","name":"Nonnensteg","latitude":52.38,"longitude":13.03,"street":null,"postalCode":null,"city":null,"country":"DE","url":"www.example.org","comment":null,"rating":0,"costs":null,"pro":null,"contra":null,"operator":"Stadt Potsdam","fee":"no","access":"yes","openingHours":"24/7","surface":"concrete","water":null,"marina":null,"extras":[],"source":"osm","osmElement":"node/42","created":"2026-10-20T12:00:00Z","updated":null}"""
        const val MARINA = """{"id":"m","name":"Yachthafen Kelheim","latitude":48.91,"longitude":11.88,"url":"www.example.org","street":null,"postalCode":"93309","city":"Kelheim","phone":null,"email":null,"operator":null,"fee":null,"access":null,"openingHours":null,"water":null,"source":"osm","osmElement":"node/7","country":"DE"}"""
        const val BODENSEE = """{"id":"b","name":"Bodensee","shortname":"BODENSEE","slipwayCount":158,"stationCount":1,"position":{"latitude":47.67,"longitude":9.31},"gauge":{"stationId":"k","stationName":"KONSTANZ","waterLevel":{"value":248,"measuredAt":"2026-10-20T17:30:00Z","state":"low"}},"countries":["DE","CH","AT"]}"""
        const val BILLE = """{"id":"w","name":"Bille","shortname":"BILLE","bounds":{"south":53.48,"west":10.02,"north":53.65,"east":10.51},
            "shape":{"type":"MultiLineString","coordinates":[[[10.46,53.64],[10.47,53.65]]]},
            "osmElements":["relation/123"],
            "slipways":[$NONNENSTEG],
            "stations":[
             {"id":"far","name":"FERN","number":"1","latitude":53.0,"longitude":10.0,"km":1,"agency":null,"water":{"id":"w","name":"Bille"},"waterLevel":{"value":100.5,"measuredAt":"2026-10-20T17:30:00Z","state":"normal"}},
             {"id":"near","name":"NAH","number":"2","latitude":52.39,"longitude":13.04,"km":2,"agency":null,"water":{"id":"w","name":"Bille"},"waterLevel":{"value":80,"measuredAt":"2026-10-20T17:30:00Z","state":"high"}},
             {"id":"none","name":"OHNE","number":"3","latitude":52.3832,"longitude":13.0360,"km":3,"agency":null,"water":{"id":"w","name":"Bille"},"waterLevel":null}],
            "marinas":[]}"""
        val WITH_INFOS = IMPORTED_DETAILS.replace(
            "\"updated\":null",
            """"updated":null,"infos":[
             {"field":"fee","value":"yes","source":"user","sourceUrl":null,"retrieved":"2026-10-21T10:00:00Z"},
             {"field":"costs","value":"5.5","source":"user","sourceUrl":null,"retrieved":"2026-10-21T10:00:00Z"},
             {"field":"jetty","value":"yes","source":"user","sourceUrl":null,"retrieved":"2026-10-21T10:00:00Z"},
             {"field":"crane","value":"no","source":"user","sourceUrl":null,"retrieved":"2026-10-21T10:00:00Z"},
             {"field":"tip","value":"Früh kommen.","source":"user","sourceUrl":null,"retrieved":"2026-10-21T10:00:00Z"},
             {"field":"surface","value":"gravel","source":"user","sourceUrl":null,"retrieved":"2026-09-01T10:00:00Z"},
             {"field":"surface","value":"wood","source":"user","sourceUrl":null,"retrieved":"2026-10-01T10:00:00Z"}]""",
        )
        const val MARINA_DETAILS = """{"id":"m","name":"Yachthafen Kelheim","latitude":48.91,"longitude":11.88,"url":"www.example.org","street":"Hafen 1","postalCode":"93309","city":"Kelheim","phone":null,"email":"hafen@example.org","operator":null,"fee":null,"access":null,"openingHours":null,"water":{"id":"w","name":"Donau"},"source":"manual","osmElement":null,
            "slipways":[$NONNENSTEG],
            "infos":[{"field":"phone","value":"+49 9441 1234","source":"website","sourceUrl":"https://www.example.org/kontakt","retrieved":"2026-10-20T12:00:00Z"},
             {"field":"email","value":"info@example.org","source":"website","sourceUrl":"https://www.example.org","retrieved":"2026-10-20T12:00:00Z"},
             {"field":"opening_hours","value":"Mo–Fr 8–18 Uhr","source":"user","sourceUrl":null,"retrieved":"2026-10-21T12:00:00Z"}]}"""
    }
}

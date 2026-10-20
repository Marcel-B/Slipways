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
    fun `turns error status into ApiException`() = runTest {
        val api = api { json("", HttpStatusCode.NotFound) }

        val error = assertFailsWith<ApiException> { api.slipway("missing") }

        assertEquals(404, error.status)
    }

    private companion object {
        // Shortened answers of https://slipways.de/api from October 2026.
        const val KELHEIM = """{"id":"1","name":"Am Jachthafen, Kelheim","latitude":48.9,"longitude":11.9,"street":"Am Jachthafen 13","postalCode":"93309","city":"Kelheim","country":"DE","url":null,"comment":null,"rating":0,"costs":null,"pro":null,"contra":null,"operator":null,"fee":null,"access":null,"openingHours":null,"surface":null,"water":{"id":"w","name":"Donau"},"marina":null,"extras":["Campingplatz"],"source":"manual","osmElement":null,"created":"2020-02-18T16:14:59Z","updated":"2026-10-20T16:26:23.5072912Z"}"""
        const val NONNENSTEG = """{"id":"2","name":"Nonnensteg","latitude":52.3832808,"longitude":13.0360706,"postalCode":null,"city":null,"country":"DE","water":null,"source":"osm"}"""
        const val IMPORTED_DETAILS = """{"id":"2","name":"Nonnensteg","latitude":52.38,"longitude":13.03,"street":null,"postalCode":null,"city":null,"country":"DE","url":"www.example.org","comment":null,"rating":0,"costs":null,"pro":null,"contra":null,"operator":"Stadt Potsdam","fee":"no","access":"yes","openingHours":"24/7","surface":"concrete","water":null,"marina":null,"extras":[],"source":"osm","osmElement":"node/42","created":"2026-10-20T12:00:00Z","updated":null}"""
        const val BODENSEE = """{"id":"b","name":"Bodensee","shortname":"BODENSEE","slipwayCount":158,"stationCount":1,"position":{"latitude":47.67,"longitude":9.31},"gauge":{"stationId":"k","stationName":"KONSTANZ","waterLevel":{"value":248,"measuredAt":"2026-10-20T17:30:00Z","state":"low"}}}"""
        const val BILLE = """{"id":"w","name":"Bille","shortname":"BILLE","bounds":{"south":53.48,"west":10.02,"north":53.65,"east":10.51},
            "shape":{"type":"MultiLineString","coordinates":[[[10.46,53.64],[10.47,53.65]]]},
            "osmElements":["relation/123"],
            "slipways":[$NONNENSTEG],
            "stations":[
             {"id":"far","name":"FERN","number":"1","latitude":53.0,"longitude":10.0,"km":1,"agency":null,"water":{"id":"w","name":"Bille"},"waterLevel":{"value":100.5,"measuredAt":"2026-10-20T17:30:00Z","state":"normal"}},
             {"id":"near","name":"NAH","number":"2","latitude":52.39,"longitude":13.04,"km":2,"agency":null,"water":{"id":"w","name":"Bille"},"waterLevel":{"value":80,"measuredAt":"2026-10-20T17:30:00Z","state":"high"}},
             {"id":"none","name":"OHNE","number":"3","latitude":52.3832,"longitude":13.0360,"km":3,"agency":null,"water":{"id":"w","name":"Bille"},"waterLevel":null}],
            "marinas":[]}"""
    }
}

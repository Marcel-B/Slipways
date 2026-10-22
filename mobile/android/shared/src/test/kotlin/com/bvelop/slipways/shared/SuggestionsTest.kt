package com.bvelop.slipways.shared

import io.ktor.client.HttpClient
import io.ktor.client.engine.mock.MockEngine
import io.ktor.client.engine.mock.MockRequestHandleScope
import io.ktor.client.engine.mock.respond
import io.ktor.client.request.HttpRequestData
import io.ktor.client.request.HttpResponseData
import io.ktor.http.HttpHeaders
import io.ktor.http.HttpMethod
import io.ktor.http.HttpStatusCode
import io.ktor.http.content.TextContent
import io.ktor.http.headersOf
import kotlinx.coroutines.test.runTest
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import java.time.Duration
import java.time.Instant
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertFailsWith
import kotlin.test.assertFalse
import kotlin.test.assertNull
import kotlin.test.assertTrue

class SuggestionsTest {
    private val requests = mutableListOf<HttpRequestData>()

    private fun api(handler: suspend MockRequestHandleScope.(HttpRequestData) -> HttpResponseData) = SlipwaysApi(
        baseUrl = "http://localhost:5080",
        httpClient = HttpClient(MockEngine { request -> requests += request; handler(request) }),
    )

    private fun MockRequestHandleScope.json(body: String, status: HttpStatusCode = HttpStatusCode.OK) =
        respond(body, status, headersOf(HttpHeaders.ContentType, "application/json"))

    @Test
    fun `hashes like the server`() {
        assertEquals("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", Proof.sha256Hex("abc"))
    }

    @Test
    fun `solves proof of work`() = runTest {
        val salt = "0a1b2c?expires=1792700000"
        val challenge = ProofChallenge("SHA-256", Proof.sha256Hex("${salt}4711"), 10_000, salt, "sig")

        val solution = Proof.solve(challenge)

        assertEquals(ProofSolution("SHA-256", challenge.challenge, 4711, salt, "sig"), solution)
    }

    @Test
    fun `sends suggestion as json without empty values`() = runTest {
        val api = api { json("""{"id":"s1","token":"t1"}""", HttpStatusCode.Accepted) }

        val created = api.suggest(
            SuggestionRequest(SuggestionKind.EDIT, PlaceType.SLIPWAY, "1", fields = mapOf("fee" to "no")),
        )

        assertEquals(SuggestionCreated("s1", "t1"), created)
        val request = requests.single()
        assertEquals(HttpMethod.Post, request.method)
        assertEquals("http://localhost:5080/api/suggestions", request.url.toString())
        val body = Json.parseToJsonElement((request.body as TextContent).text).jsonObject
        assertEquals("edit", body["kind"]?.jsonPrimitive?.content)
        assertEquals("slipway", body["placeType"]?.jsonPrimitive?.content)
        assertEquals("no", (body["fields"] as JsonObject)["fee"]?.jsonPrimitive?.content)
        assertFalse("name" in body)
    }

    @Test
    fun `reports nearby slipways`() = runTest {
        val api = api {
            json("""{"slipways":[{"id":"2","name":"Nonnensteg","distance":120,"source":"osm"}]}""", HttpStatusCode.Conflict)
        }

        val error = assertFailsWith<NearbySlipwaysException> { api.suggest(SuggestionRequest(SuggestionKind.NEW_SLIPWAY)) }

        assertEquals(listOf("Nonnensteg"), error.slipways.map { it.name })
        assertEquals(Source.OSM, error.slipways.single().source)
    }

    @Test
    fun `shows the server's reason for rejected suggestions`() = runTest {
        val api = api {
            json("""{"title":"Bad Request","status":400,"detail":"Der Wert für „costs“ ist nicht gültig."}""", HttpStatusCode.BadRequest)
        }

        val error = assertFailsWith<ApiException> { api.suggest(SuggestionRequest(SuggestionKind.EDIT)) }

        assertEquals(400, error.status)
        assertEquals("Der Wert für „costs“ ist nicht gültig.", error.userMessage)
        assertTrue(ApiException(429, "Too Many Requests").userMessage.startsWith("Du hast gerade viele Vorschläge"))
    }

    @Test
    fun `reads and withdraws suggestion`() = runTest {
        val api = api { request ->
            if (request.method == HttpMethod.Delete) respond("", HttpStatusCode.NoContent)
            else json("""{"id":"s1","kind":"new_slipway","status":"pending","created":"2026-10-22T10:00:00Z","decided":null}""")
        }

        val status = api.suggestion("s1", "t1")
        api.withdrawSuggestion("s1", "t1")

        assertEquals(SuggestionKind.NEW_SLIPWAY, status.kind)
        assertEquals("pending", status.status)
        assertNull(status.decided)
        assertEquals("http://localhost:5080/api/suggestions/s1?token=t1", requests[0].url.toString())
        assertEquals(HttpMethod.Delete, requests[1].method)
    }

    @Test
    fun `remembers sent suggestions`() {
        var stored: String? = null
        val store = SuggestionStore(read = { stored }, write = { stored = it })
        val now = Instant.now()
        fun sent(id: String, fields: List<String>, daysAgo: Long) = SentSuggestion(
            SuggestionCreated(id, "t"), SuggestionKind.EDIT, "Kelheim", PlaceType.SLIPWAY, "1", fields,
            now - Duration.ofDays(daysAgo),
        )
        store.remember(sent("old", listOf("fee"), 100))
        store.remember(sent("month", listOf("surface"), 40))
        store.remember(sent("new", listOf("access", "opening_hours"), 1))

        assertEquals(listOf("new", "month"), store.all(now).map { it.id })
        assertEquals(setOf("access", "opening_hours"), store.pendingFields(PlaceType.SLIPWAY, "1", null, now))
        assertEquals(emptySet(), store.pendingFields(PlaceType.SLIPWAY, "1", now.toString(), now))
        assertEquals(emptySet(), store.pendingFields(PlaceType.MARINA, "1", null, now))

        store.forget("new")
        assertEquals(listOf("month"), store.all(now).map { it.id })
    }

    @Test
    fun `sends only changed values`() {
        val changed = Contributions.changed(
            mapOf("fee" to "no", "access" to "yes", "opening_hours" to " ", "surface" to "gravel"),
            mapOf("fee" to "no", "access" to "private"),
        )

        assertEquals(mapOf("access" to "yes", "surface" to "gravel"), changed)
        assertEquals("nur für Kunden", Contributions.FIELDS.getValue("access").labelOf("customers"))
        assertEquals("https://www.openstreetmap.org/edit?way=7", Contributions.osmEditUrl("way/7"))
    }

    @Test
    fun `latest change wins over text order`() {
        assertEquals("2026-10-20T16:26:23.5Z", Format.latest(listOf("2026-10-20T16:26:23Z", "2026-10-20T16:26:23.5Z")))
    }
}

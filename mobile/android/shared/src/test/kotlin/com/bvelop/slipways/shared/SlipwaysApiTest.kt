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
import kotlin.test.assertNull

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
        val api = api {
            json("""[{"id":"1","name":"Am Jachthafen, Kelheim","latitude":48.9,"longitude":11.9,"postalCode":"93309","city":"Kelheim","country":"DE","water":{"id":"w","name":"Donau"}}]""")
        }

        val slipways = api.slipways(search = " Kelheim ")

        assertEquals("Donau", slipways.single().water.name)
        assertEquals("http://localhost:5080/api/slipways?search=Kelheim", requests.single().url.toString())
    }

    @Test
    fun `reads slipway details and ignores unknown fields`() = runTest {
        val api = api {
            json("""{"id":"1","name":"Kelheim","latitude":48.9,"longitude":11.9,"street":"Am Jachthafen 13","postalCode":"93309","city":"Kelheim","country":"DE","comment":null,"rating":0,"costs":null,"pro":null,"contra":null,"water":{"id":"w","name":"Donau"},"marina":null,"extras":["Campingplatz"],"created":"2020-02-18T16:14:59Z","updated":null}""")
        }

        val slipway = api.slipway("1")

        assertNull(slipway.costs)
        assertEquals(listOf("Campingplatz"), slipway.extras)
        assertEquals("Am Jachthafen 13, 93309 Kelheim, DE", slipway.address)
    }

    @Test
    fun `turns error status into ApiException`() = runTest {
        val api = api { json("", HttpStatusCode.NotFound) }

        val error = assertFailsWith<ApiException> { api.slipway("missing") }

        assertEquals(404, error.status)
    }
}

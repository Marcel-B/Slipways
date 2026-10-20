package com.bvelop.slipways.shared

import io.ktor.client.HttpClient
import io.ktor.client.engine.mock.MockEngine
import io.ktor.client.engine.mock.respond
import io.ktor.http.HttpHeaders
import io.ktor.http.HttpStatusCode
import io.ktor.http.headersOf
import kotlinx.coroutines.test.runTest
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNull

class SessionTest {
    private class MemoryTokens(var value: String? = null) : TokenStorage {
        override fun token() = value
        override fun save(token: String?) {
            value = token
        }
    }

    private val jsonHeaders = headersOf(HttpHeaders.ContentType, "application/json")

    private fun api(tokens: TokenStorage, meStatus: HttpStatusCode = HttpStatusCode.OK) = SlipwaysApi(
        baseUrl = "http://test",
        httpClient = HttpClient(
            MockEngine { request ->
                when (request.url.encodedPath) {
                    "/api/account/login" -> respond(
                        """{"tokenType":"Bearer","accessToken":"t1","expiresIn":3600,"refreshToken":"r1"}""",
                        HttpStatusCode.OK,
                        jsonHeaders,
                    )
                    "/api/account/me" -> respond("""{"id":"u","email":"skipper@example.com"}""", meStatus, jsonHeaders)
                    else -> respond("", HttpStatusCode.NotFound)
                }
            },
        ),
        tokenProvider = tokens::token,
    )

    @Test
    fun loginStoresTokenAndLoadsUser() = runTest {
        val tokens = MemoryTokens()
        val session = Session(api(tokens), tokens)

        session.login("skipper@example.com", "secret123")

        assertEquals("t1", tokens.value)
        assertEquals("skipper@example.com", session.user.value?.email)
    }

    @Test
    fun logoutClearsTokenAndUser() = runTest {
        val tokens = MemoryTokens()
        val session = Session(api(tokens), tokens)
        session.login("skipper@example.com", "secret123")

        session.logout()

        assertNull(tokens.value)
        assertNull(session.user.value)
    }

    @Test
    fun storedTokenRestoresUser() = runTest {
        val tokens = MemoryTokens("t0")
        val session = Session(api(tokens), tokens)

        session.restore()

        assertEquals("skipper@example.com", session.user.value?.email)
    }

    @Test
    fun expiredStoredTokenIsDropped() = runTest {
        val tokens = MemoryTokens("expired")
        val session = Session(api(tokens, meStatus = HttpStatusCode.Unauthorized), tokens)

        session.restore()

        assertNull(tokens.value)
        assertNull(session.user.value)
    }
}

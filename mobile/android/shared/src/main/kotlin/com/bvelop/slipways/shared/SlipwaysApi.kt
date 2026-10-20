package com.bvelop.slipways.shared

import io.ktor.client.HttpClient
import io.ktor.client.HttpClientConfig
import io.ktor.client.call.body
import io.ktor.client.plugins.contentnegotiation.ContentNegotiation
import io.ktor.client.plugins.defaultRequest
import io.ktor.client.request.bearerAuth
import io.ktor.client.request.get
import io.ktor.client.request.parameter
import io.ktor.client.request.post
import io.ktor.client.request.setBody
import io.ktor.client.statement.HttpResponse
import io.ktor.http.ContentType
import io.ktor.http.contentType
import io.ktor.http.isSuccess
import io.ktor.serialization.kotlinx.json.json
import kotlinx.serialization.json.Json

class ApiException(val status: Int, message: String) : Exception(message)

/**
 * Client for the Slipways backend (see backend/ in the repository).
 *
 * @param tokenProvider returns the current bearer token, or null when signed out.
 */
class SlipwaysApi(
    baseUrl: String,
    httpClient: HttpClient,
    private val tokenProvider: () -> String? = { null },
) {
    private val client = httpClient.config { configure(baseUrl) }

    suspend fun slipways(search: String? = null): List<SlipwaySummary> =
        client.get("api/slipways") {
            if (!search.isNullOrBlank()) parameter("search", search.trim())
        }.bodyOrThrow()

    suspend fun slipway(id: String): SlipwayDetails = client.get("api/slipways/$id").bodyOrThrow()

    suspend fun waters(): List<Water> = client.get("api/waters").bodyOrThrow()

    suspend fun login(email: String, password: String): AccessToken =
        client.post("api/account/login") {
            contentType(ContentType.Application.Json)
            setBody(Credentials(email, password))
        }.bodyOrThrow()

    suspend fun register(email: String, password: String) {
        client.post("api/account/register") {
            contentType(ContentType.Application.Json)
            setBody(Credentials(email, password))
        }.throwIfFailed()
    }

    suspend fun me(): User =
        client.get("api/account/me") {
            tokenProvider()?.let { bearerAuth(it) }
        }.bodyOrThrow()

    private suspend inline fun <reified T> HttpResponse.bodyOrThrow(): T {
        throwIfFailed()
        return body()
    }

    private fun HttpResponse.throwIfFailed() {
        if (!status.isSuccess()) throw ApiException(status.value, status.description)
    }

    companion object {
        val json = Json { ignoreUnknownKeys = true }

        private fun HttpClientConfig<*>.configure(baseUrl: String) {
            expectSuccess = false
            install(ContentNegotiation) { json(json) }
            defaultRequest { url(baseUrl.trimEnd('/') + "/") }
        }
    }
}

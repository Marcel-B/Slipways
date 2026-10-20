package com.bvelop.slipways.shared

import io.ktor.client.HttpClient
import io.ktor.client.HttpClientConfig
import io.ktor.client.call.body
import io.ktor.client.plugins.contentnegotiation.ContentNegotiation
import io.ktor.client.plugins.defaultRequest
import io.ktor.client.request.get
import io.ktor.client.request.parameter
import io.ktor.client.statement.HttpResponse
import io.ktor.http.isSuccess
import io.ktor.serialization.kotlinx.json.json
import kotlinx.serialization.json.Json

class ApiException(val status: Int, message: String) : Exception(message)

/** Read-only client for the Slipways backend (see backend/ in the repository). */
class SlipwaysApi(baseUrl: String, httpClient: HttpClient) {
    private val client = httpClient.config { configure(baseUrl) }

    suspend fun slipways(search: String? = null): List<SlipwaySummary> =
        client.get("api/slipways") {
            if (!search.isNullOrBlank()) parameter("search", search.trim())
        }.bodyOrThrow()

    suspend fun slipway(id: String): SlipwayDetails = client.get("api/slipways/$id").bodyOrThrow()

    suspend fun waters(search: String? = null): List<WaterSummary> =
        client.get("api/waters") {
            if (!search.isNullOrBlank()) parameter("search", search.trim())
        }.bodyOrThrow()

    suspend fun marinas(): List<Marina> = client.get("api/marinas").bodyOrThrow()

    suspend fun water(id: String): WaterDetails = client.get("api/waters/$id").bodyOrThrow()

    private suspend inline fun <reified T> HttpResponse.bodyOrThrow(): T {
        if (!status.isSuccess()) throw ApiException(status.value, status.description)
        return body()
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

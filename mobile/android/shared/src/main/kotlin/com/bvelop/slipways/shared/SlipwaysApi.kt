package com.bvelop.slipways.shared

import io.ktor.client.HttpClient
import io.ktor.client.HttpClientConfig
import io.ktor.client.call.body
import io.ktor.client.plugins.contentnegotiation.ContentNegotiation
import io.ktor.client.plugins.defaultRequest
import io.ktor.client.request.delete
import io.ktor.client.request.get
import io.ktor.client.request.parameter
import io.ktor.client.request.post
import io.ktor.client.request.setBody
import io.ktor.client.statement.HttpResponse
import io.ktor.client.statement.bodyAsText
import io.ktor.http.ContentType
import io.ktor.http.HttpStatusCode
import io.ktor.http.contentType
import io.ktor.http.isSuccess
import io.ktor.serialization.kotlinx.json.json
import kotlinx.serialization.Serializable
import kotlinx.serialization.json.Json

/** An answer other than 2xx; [message] is the server's explanation when it sent one, e.g. why a suggestion was not taken. */
class ApiException(val status: Int, message: String) : Exception(message) {
    /** A German text for people, as the website shows it. */
    val userMessage: String
        get() = if (status == 429) "Du hast gerade viele Vorschläge geschickt. Bitte versuch es später noch einmal." else message!!
}

/** Client for the Slipways backend (see backend/ in the repository): reads places and sends suggestions. */
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

    suspend fun marina(id: String): MarinaDetails = client.get("api/marinas/$id").bodyOrThrow()

    /** Shapes of the waters a search finds. */
    suspend fun waterShapes(search: String): WaterShapes =
        client.get("api/waters/shapes") { parameter("search", search.trim()) }.bodyOrThrow()

    /** Shapes of the waters reaching into an area that are at least [ShapeArea.minExtent] large. */
    suspend fun waterShapes(area: ShapeArea): WaterShapes =
        client.get("api/waters/shapes") {
            parameter("bbox", area.bbox)
            parameter("minExtent", ShapeArea.text(area.minExtent))
        }.bodyOrThrow()

    /** A proof-of-work challenge to solve with [Proof.solve] before sending a suggestion. */
    suspend fun challenge(): ProofChallenge = client.get("api/suggestions/challenge").bodyOrThrow()

    /** Sends a suggestion; throws [NearbySlipwaysException] when a new slipway lies close to known ones. */
    suspend fun suggest(suggestion: SuggestionRequest): SuggestionCreated {
        val response = client.post("api/suggestions") {
            contentType(ContentType.Application.Json)
            setBody(suggestion)
        }
        if (response.status == HttpStatusCode.Conflict) throw NearbySlipwaysException(response.body<NearbySlipways>().slipways)
        return response.bodyOrThrow()
    }

    /** The state of a suggestion this device sent. */
    suspend fun suggestion(id: String, token: String): SuggestionStatus =
        client.get("api/suggestions/$id") { parameter("token", token) }.bodyOrThrow()

    suspend fun withdrawSuggestion(id: String, token: String) {
        client.delete("api/suggestions/$id") { parameter("token", token) }.check()
    }

    private suspend inline fun <reified T> HttpResponse.bodyOrThrow(): T {
        check()
        return body()
    }

    /** Throws for answers other than 2xx, with the text of a problem details answer (RFC 9457) if there is one. */
    private suspend fun HttpResponse.check() {
        if (status.isSuccess()) return
        val problem = runCatching { json.decodeFromString<Problem>(bodyAsText()) }.getOrNull()
        val message = problem?.errors?.values?.flatten()?.joinToString(" ")?.takeIf { it.isNotBlank() }
            ?: problem?.detail
            ?: "Das hat nicht geklappt (Fehler ${status.value}). Bitte versuch es später noch einmal."
        throw ApiException(status.value, message)
    }

    @Serializable
    private data class Problem(val detail: String? = null, val errors: Map<String, List<String>>? = null)

    companion object {
        val json = Json { ignoreUnknownKeys = true }

        private fun HttpClientConfig<*>.configure(baseUrl: String) {
            expectSuccess = false
            install(ContentNegotiation) { json(json) }
            defaultRequest { url(baseUrl.trimEnd('/') + "/") }
        }
    }
}

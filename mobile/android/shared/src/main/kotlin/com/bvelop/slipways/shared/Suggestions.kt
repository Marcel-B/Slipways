package com.bvelop.slipways.shared

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ensureActive
import kotlinx.coroutines.withContext
import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable
import kotlinx.serialization.builtins.ListSerializer
import java.security.MessageDigest
import java.time.Duration
import java.time.Instant

// Mirrors the suggestion DTOs in backend/src/Slipways.Api/Endpoints/Contracts.cs.

@Serializable
enum class PlaceType {
    @SerialName("slipway") SLIPWAY,
    @SerialName("marina") MARINA,
}

@Serializable
enum class SuggestionKind(val label: String) {
    @SerialName("new_slipway") NEW_SLIPWAY("Neue Slipanlage"),
    @SerialName("edit") EDIT("Ergänzung"),
    @SerialName("tip") TIP("Tipp"),
    @SerialName("report") REPORT("Hinweis auf ein Problem"),
}

/** A proof-of-work challenge in the format of ALTCHA: find the number with SHA-256(salt + number) = challenge. */
@Serializable
data class ProofChallenge(
    val algorithm: String,
    val challenge: String,
    val maxNumber: Int,
    val salt: String,
    val signature: String,
)

@Serializable
data class ProofSolution(
    val algorithm: String,
    val challenge: String,
    val number: Int,
    val salt: String,
    val signature: String,
)

object Proof {
    class UnsolvableException : Exception("Der Spam-Schutz ließ sich nicht lösen.")

    /** Finds the number for a challenge; about a second for the server's usual one. Runs on a background thread. */
    suspend fun solve(challenge: ProofChallenge): ProofSolution = withContext(Dispatchers.Default) {
        val target = hexBytes(challenge.challenge) ?: throw UnsolvableException()
        val digest = MessageDigest.getInstance("SHA-256")
        val salt = challenge.salt.toByteArray()
        for (number in 0..challenge.maxNumber.coerceAtLeast(0)) {
            if (number % 1000 == 0) ensureActive()
            digest.update(salt)
            if (digest.digest(number.toString().toByteArray()).contentEquals(target)) {
                return@withContext ProofSolution(
                    challenge.algorithm, challenge.challenge, number, challenge.salt, challenge.signature,
                )
            }
        }
        throw UnsolvableException()
    }

    internal fun sha256Hex(text: String): String =
        MessageDigest.getInstance("SHA-256").digest(text.toByteArray()).joinToString("") { "%02x".format(it) }

    private fun hexBytes(hex: String): ByteArray? =
        if (hex.length % 2 != 0) null
        else runCatching { ByteArray(hex.length / 2) { hex.substring(it * 2, it * 2 + 2).toInt(16).toByte() } }.getOrNull()
}

/** A suggestion as the app sends it; see `SuggestionRequestDto` in the backend. Null values are left out. */
@Serializable
data class SuggestionRequest(
    val kind: SuggestionKind,
    val placeType: PlaceType? = null,
    val placeId: String? = null,
    val name: String? = null,
    val latitude: Double? = null,
    val longitude: Double? = null,
    val fields: Map<String, String>? = null,
    val text: String? = null,
    val confirmNew: Boolean = false,
    val proof: ProofSolution? = null,
)

/** [token] is shown only once; with it the sender can look at or withdraw the suggestion. */
@Serializable
data class SuggestionCreated(val id: String, val token: String)

@Serializable
data class NearbySlipway(
    val id: String,
    val name: String,
    /** In metres. */
    val distance: Double,
    val source: Source = Source.MANUAL,
)

@Serializable
internal data class NearbySlipways(val slipways: List<NearbySlipway> = emptyList())

/** A suggested new slipway lies close to ones we have; sending it again with `confirmNew` takes it anyway. */
class NearbySlipwaysException(val slipways: List<NearbySlipway>) : Exception("In der Nähe gibt es schon Slipanlagen.")

@Serializable
data class SuggestionStatus(
    val id: String,
    val kind: SuggestionKind,
    /** "pending", "accepted" or "rejected". */
    val status: String,
    val created: String,
    val decided: String? = null,
)

/** A suggestion sent from this device; kept only here, so the app can say what is waiting and show its state. */
@Serializable
data class SentSuggestion(
    val id: String,
    val token: String,
    val kind: SuggestionKind,
    val placeType: PlaceType? = null,
    val placeId: String? = null,
    /** The place's name, or the name of a new slipway, for the list of sent suggestions. */
    val name: String,
    val fields: List<String> = emptyList(),
    /** ISO 8601. */
    val sent: String,
) {
    constructor(
        created: SuggestionCreated,
        kind: SuggestionKind,
        name: String,
        placeType: PlaceType? = null,
        placeId: String? = null,
        fields: Collection<String> = emptyList(),
        sent: Instant = Instant.now(),
    ) : this(created.id, created.token, kind, placeType, placeId, name, fields.toList(), sent.toString())
}

/**
 * Suggestions sent from this device, kept by [read] and [write] (in the app: SharedPreferences). Like the website's
 * local storage: nothing leaves the device.
 */
class SuggestionStore(private val read: () -> String?, private val write: (String) -> Unit) {
    private val serializer = ListSerializer(SentSuggestion.serializer())

    /** Newest first. */
    @Synchronized
    fun all(now: Instant = Instant.now()): List<SentSuggestion> = load(now).sortedByDescending { Instant.parse(it.sent) }

    @Synchronized
    fun remember(suggestion: SentSuggestion) = save(load(Instant.now()) + suggestion)

    @Synchronized
    fun forget(id: String) = save(load(Instant.now()).filter { it.id != id })

    /**
     * Fields of a place this device sent values for that still wait, as far as the app can tell: sent recently, and
     * after the place last changed ([changed], ISO 8601: when it was updated or got a value from users).
     */
    fun pendingFields(type: PlaceType, id: String, changed: String?, now: Instant = Instant.now()): Set<String> {
        val cutoff = now - PENDING
        val since = changed?.let(::parseOrNull)
        return all(now)
            .filter { it.placeType == type && it.placeId == id }
            .filter { Instant.parse(it.sent).let { sent -> sent > cutoff && (since == null || sent > since) } }
            .flatMap { it.fields }
            .toSet()
    }

    private fun load(now: Instant): List<SentSuggestion> {
        val all = read()?.let { runCatching { SlipwaysApi.json.decodeFromString(serializer, it) }.getOrNull() } ?: emptyList()
        val cutoff = now - KEEP
        return all.filter { parseOrNull(it.sent)?.let { sent -> sent > cutoff } == true }
    }

    private fun save(suggestions: List<SentSuggestion>) = write(SlipwaysApi.json.encodeToString(serializer, suggestions))

    private companion object {
        // Older entries are decided or deleted on the server by then (see the privacy page).
        val KEEP: Duration = Duration.ofDays(90)

        // A suggestion still undecided after this long was most likely turned down.
        val PENDING: Duration = Duration.ofDays(30)

        fun parseOrNull(text: String): Instant? = runCatching { Instant.parse(text) }.getOrNull()
    }
}

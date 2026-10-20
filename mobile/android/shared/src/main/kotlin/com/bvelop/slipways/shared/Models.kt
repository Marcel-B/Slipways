package com.bvelop.slipways.shared

import kotlinx.serialization.Serializable

@Serializable
data class Water(val id: String, val name: String)

@Serializable
data class SlipwaySummary(
    val id: String,
    val name: String,
    val latitude: Double,
    val longitude: Double,
    val postalCode: String? = null,
    val city: String? = null,
    val country: String? = null,
    val water: Water,
)

@Serializable
data class MarinaSummary(val id: String, val name: String, val url: String? = null)

@Serializable
data class SlipwayDetails(
    val id: String,
    val name: String,
    val latitude: Double,
    val longitude: Double,
    val street: String? = null,
    val postalCode: String? = null,
    val city: String? = null,
    val country: String? = null,
    val comment: String? = null,
    val rating: Int = 0,
    /** Launch fee in euros; null when unknown. */
    val costs: Double? = null,
    val pro: String? = null,
    val contra: String? = null,
    val water: Water,
    val marina: MarinaSummary? = null,
    val extras: List<String> = emptyList(),
) {
    val address: String
        get() = listOfNotNull(street, listOfNotNull(postalCode, city).joinToString(" ").ifBlank { null }, country)
            .joinToString(", ")
}

@Serializable
data class User(val id: String, val email: String, val displayName: String? = null)

@Serializable
data class AccessToken(
    val tokenType: String,
    val accessToken: String,
    val expiresIn: Long,
    val refreshToken: String,
)

@Serializable
internal data class Credentials(val email: String, val password: String)

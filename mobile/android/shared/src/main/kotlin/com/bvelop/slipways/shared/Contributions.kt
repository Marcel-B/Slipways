package com.bvelop.slipways.shared

/**
 * Fields users can send values for, as the server knows them (SuggestionFields.cs) and the website labels them
 * (web/src/contribute/fields.ts).
 */
object Contributions {
    private val labels = mapOf(
        "fee" to "Gebühr",
        "costs" to "Kosten in Euro",
        "access" to "Zugang",
        "opening_hours" to "Öffnungszeiten",
        "surface" to "Untergrund",
        "operator" to "Betreiber",
        "phone" to "Telefon",
        "email" to "E-Mail",
        "trailer_parking" to "Parkplatz für Gespann",
        "jetty" to "Steg",
        "toilet" to "Toilette",
        "crane" to "Kran",
        "steep" to "Rampe steil",
        "slippery_at_low_water" to "Rutschig bei Niedrigwasser",
        "key_required" to "Nur mit Schlüssel",
    )

    /** Facts about a slipway to tick, shown together under „Was gibt es dort?“. */
    val FACTS = listOf("trailer_parking", "jetty", "toilet", "crane", "steep", "slippery_at_low_water", "key_required")

    fun label(field: String) = labels[field] ?: field

    /** The newest value users sent in for a field, if any; it wins over the place's own one. */
    fun fromUsers(infos: List<PlaceInfo>, field: String): PlaceInfo? =
        infos.filter { it.field == field && it.source == "user" }.maxByOrNull { it.retrieved }

    /** Tips from users in the order the server sends them. */
    fun tips(infos: List<PlaceInfo>): List<PlaceInfo> = infos.filter { it.field == "tip" && it.source == "user" }
}

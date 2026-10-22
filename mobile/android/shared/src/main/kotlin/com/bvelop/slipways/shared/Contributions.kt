package com.bvelop.slipways.shared

/** A field users can suggest a value for, as the server knows it (SuggestionFields.cs). */
data class SuggestionField(
    val name: String,
    val label: String,
    /** Value and label of each choice; without them the field takes text. */
    val choices: List<Pair<String, String>> = emptyList(),
    val placeholder: String? = null,
    val keyboard: Keyboard = Keyboard.TEXT,
) {
    enum class Keyboard { TEXT, DECIMAL, PHONE, EMAIL }

    /** The label of a choice, else the value itself. */
    fun labelOf(value: String): String = choices.firstOrNull { it.first == value }?.second ?: value
}

/**
 * Fields users can send values for, as the server knows them (SuggestionFields.cs) and the website labels them
 * (web/src/contribute/fields.ts).
 */
object Contributions {
    private val yesNo = listOf("yes" to "ja", "no" to "nein")

    val FIELDS: Map<String, SuggestionField> = listOf(
        SuggestionField("fee", "Gebühr", listOf("no" to "kostenlos", "yes" to "kostenpflichtig")),
        SuggestionField("costs", "Kosten in Euro", placeholder = "z. B. 5", keyboard = SuggestionField.Keyboard.DECIMAL),
        SuggestionField(
            "access", "Zugang",
            listOf(
                "yes" to "öffentlich", "permissive" to "geduldet", "customers" to "nur für Kunden",
                "members" to "nur für Mitglieder", "private" to "privat", "no" to "kein Zugang",
            ),
        ),
        SuggestionField("opening_hours", "Öffnungszeiten", placeholder = "z. B. Mo–Fr 8–18 Uhr, Sa 9–12 Uhr"),
        SuggestionField(
            "surface", "Untergrund",
            listOf(
                "concrete" to "Beton", "asphalt" to "Asphalt", "paving_stones" to "Pflaster", "gravel" to "Kies",
                "wood" to "Holz", "sand" to "Sand", "grass" to "Gras", "unpaved" to "unbefestigt",
            ),
        ),
        SuggestionField("operator", "Betreiber", placeholder = "z. B. Stadt, Verein oder Marina"),
        SuggestionField("phone", "Telefon", placeholder = "+49 …", keyboard = SuggestionField.Keyboard.PHONE),
        SuggestionField("email", "E-Mail", placeholder = "hafen@example.de", keyboard = SuggestionField.Keyboard.EMAIL),
        SuggestionField("trailer_parking", "Parkplatz für Gespann", yesNo),
        SuggestionField("jetty", "Steg", yesNo),
        SuggestionField("toilet", "Toilette", yesNo),
        SuggestionField("crane", "Kran", yesNo),
        SuggestionField("steep", "Rampe steil", yesNo),
        SuggestionField("slippery_at_low_water", "Rutschig bei Niedrigwasser", yesNo),
        SuggestionField("key_required", "Nur mit Schlüssel", yesNo),
    ).associateBy { it.name }

    /** Facts about a slipway to tick, shown together under „Was gibt es dort?“. */
    val FACTS = listOf("trailer_parking", "jetty", "toilet", "crane", "steep", "slippery_at_low_water", "key_required")

    /** Longest tip the server takes; other texts may have up to [TEXT_LENGTH] characters. */
    const val TIP_LENGTH = 200
    const val TEXT_LENGTH = 500
    const val NAME_LENGTH = 120

    /** The few fields a place should have; only these are asked for when missing. */
    fun wanted(type: PlaceType): List<String> = when (type) {
        PlaceType.SLIPWAY -> listOf("fee", "access", "opening_hours", "surface")
        PlaceType.MARINA -> listOf("phone", "opening_hours", "access", "fee")
    }

    fun label(field: String) = FIELDS[field]?.label ?: field

    /** The newest value users sent in for a field, if any; it wins over the place's own one. */
    fun fromUsers(infos: List<PlaceInfo>, field: String): PlaceInfo? =
        infos.filter { it.field == field && it.source == "user" }.maxByOrNull { it.retrieved }

    /** Tips from users in the order the server sends them. */
    fun tips(infos: List<PlaceInfo>): List<PlaceInfo> = infos.filter { it.field == "tip" && it.source == "user" }

    /** The OpenStreetMap editor for an element such as "node/123". */
    fun osmEditUrl(element: String): String? {
        val parts = element.split("/")
        return if (parts.size == 2) "https://www.openstreetmap.org/edit?${parts[0]}=${parts[1]}" else null
    }

    /** Only the values the sender filled in and changed go to the server. */
    fun changed(values: Map<String, String>, current: Map<String, String>): Map<String, String> =
        values.filter { (field, value) -> value.isNotBlank() && value != (current[field] ?: "") }
}

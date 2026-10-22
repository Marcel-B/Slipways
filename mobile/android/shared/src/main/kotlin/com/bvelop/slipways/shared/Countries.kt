package com.bvelop.slipways.shared

import java.util.Locale

/**
 * Places of one country, drawn as a single bubble while the map shows a continent. [center] is the median, which stays
 * on land for long coasts where the mean drifts out to sea; [area] is where most of its places lie.
 */
data class CountryGroup<T>(val code: String, val items: List<T>, val center: Position, val area: MapArea) {
    val name: String
        get() = Countries.name(code)
}

/** A country to choose with the number of entries in it. */
data class CountryCount(val code: String, val count: Int) {
    /** "Frankreich (1.234)" */
    val label: String
        get() = "${Countries.name(code)} (${Format.number(count)})"
}

/** Grouping by country as on the website (web/src/components/countries.ts). */
object Countries {
    private val names = mapOf(
        "AT" to "Österreich", "BE" to "Belgien", "CH" to "Schweiz", "CZ" to "Tschechien", "DE" to "Deutschland",
        "DK" to "Dänemark", "ES" to "Spanien", "FI" to "Finnland", "FR" to "Frankreich", "GB" to "Vereinigtes Königreich",
        "GR" to "Griechenland", "HR" to "Kroatien", "IT" to "Italien", "LU" to "Luxemburg", "NL" to "Niederlande",
        "NO" to "Norwegen", "PL" to "Polen", "PT" to "Portugal", "SE" to "Schweden", "SI" to "Slowenien",
    )

    /** The German name of a country, e.g. "Niederlande" for NL. */
    fun name(code: String): String =
        names[code] ?: Locale.Builder().setRegion(code).build().getDisplayCountry(Locale.GERMAN).ifBlank { code }

    /** Groups places by their ISO country code, largest first; places without one are left out. */
    fun <T> group(items: List<T>, country: (T) -> String?, position: (T) -> Position): List<CountryGroup<T>> =
        items.groupBy(country)
            .mapNotNull { (code, members) ->
                code ?: return@mapNotNull null
                val points = members.map(position)
                CountryGroup(
                    code,
                    members,
                    Position(median(points.map { it.latitude }), median(points.map { it.longitude })),
                    coreArea(points)!!,
                )
            }
            .sortedWith(compareByDescending<CountryGroup<T>> { it.items.size }.thenBy { it.code })

    /** How many entries touch each country, most first; a river through three countries counts in all three. */
    fun counts(countries: List<List<String>>): List<CountryCount> =
        countries.flatten().groupingBy { it }.eachCount()
            .map { (code, count) -> CountryCount(code, count) }
            .sortedWith(compareByDescending<CountryCount> { it.count }.thenBy { it.code })

    /**
     * Like [MapArea.fitting], but from the 2nd to the 98th percentile, so a single far-off place does not zoom out to
     * half of Europe.
     */
    fun coreArea(points: List<Position>): MapArea? {
        if (points.isEmpty()) return null
        fun spread(values: List<Double>): Pair<Double, Double> {
            val sorted = values.sorted()
            return sorted[(sorted.size * 0.02).toInt()] to sorted[Math.ceil(sorted.size * 0.98).toInt() - 1]
        }
        val (south, north) = spread(points.map { it.latitude })
        val (west, east) = spread(points.map { it.longitude })
        return MapArea.fitting(listOf(Position(south, west), Position(north, east)))
    }

    private fun median(values: List<Double>) = values.sorted()[values.size / 2]
}

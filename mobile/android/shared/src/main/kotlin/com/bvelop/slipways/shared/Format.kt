package com.bvelop.slipways.shared

import java.math.BigDecimal
import java.text.NumberFormat
import java.time.Instant
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.Locale

/** German texts and formats shared by the screens; kept here so they are tested. */
object Format {
    private val german = Locale.GERMANY
    private val timestamp = DateTimeFormatter.ofPattern("dd.MM.yy, HH:mm", german)
    private val day = DateTimeFormatter.ofPattern("dd.MM.yyyy", german)
    private val month = DateTimeFormatter.ofPattern("MM.yyyy", german)

    /** "93309 Kelheim", or null when both are missing. */
    fun place(postalCode: String?, city: String?): String? =
        listOfNotNull(postalCode, city).joinToString(" ").ifBlank { null }

    fun euros(value: BigDecimal): String = NumberFormat.getCurrencyInstance(german).format(value)

    /** "248 cm · niedrig" */
    fun waterLevel(level: WaterLevel): String =
        listOfNotNull("${decimal(level.value)} cm", level.state?.let(Labels::state)).joinToString(" · ")

    /** "1,2 km" */
    fun kilometres(value: Double): String = "${decimal(value)} km"

    /** "20.10.26, 19:30" in the device's time zone; the text as it is when it is no ISO 8601 instant. */
    fun measuredAt(level: WaterLevel, zone: ZoneId = ZoneId.systemDefault()): String =
        runCatching { timestamp.format(Instant.parse(level.measuredAt).atZone(zone)) }.getOrDefault(level.measuredAt)

    /** "4 Slipanlagen", "1 Slipanlage", "1.234 Slipanlagen" */
    fun count(n: Int, singular: String, plural: String): String = "${number(n)} ${if (n == 1) singular else plural}"

    /** "1.234" */
    fun number(n: Int): String = NumberFormat.getIntegerInstance(german).format(n)

    /** "20.10.2026" in the device's time zone; the text as it is when it is no ISO 8601 instant. */
    fun date(iso: String, zone: ZoneId = ZoneId.systemDefault()): String =
        runCatching { day.format(Instant.parse(iso).atZone(zone)) }.getOrDefault(iso)

    /** "10.2026", for values from users: the day would suggest more precision than a moderated value has. */
    fun monthYear(iso: String, zone: ZoneId = ZoneId.systemDefault()): String =
        runCatching { month.format(Instant.parse(iso).atZone(zone)) }.getOrDefault(iso)

    private fun decimal(value: Double): String =
        NumberFormat.getNumberInstance(german).apply { maximumFractionDigits = 1 }.format(value)
}

/** OpenStreetMap tag values and PEGELONLINE states in German; values without a label are shown as they are. */
object Labels {
    private val fees = mapOf("yes" to "kostenpflichtig", "no" to "kostenlos")

    private val accesses = mapOf(
        "yes" to "öffentlich",
        "permissive" to "geduldet",
        "customers" to "nur für Kunden",
        "members" to "nur für Mitglieder",
        "private" to "privat",
        "no" to "kein Zugang",
    )

    private val surfaces = mapOf(
        "concrete" to "Beton",
        "asphalt" to "Asphalt",
        "paved" to "befestigt",
        "paving_stones" to "Pflaster",
        "sett" to "Pflaster",
        "gravel" to "Kies",
        "fine_gravel" to "Kies",
        "wood" to "Holz",
        "sand" to "Sand",
        "grass" to "Gras",
        "unpaved" to "unbefestigt",
    )

    private val states = mapOf(
        "low" to "niedrig",
        "normal" to "normal",
        "high" to "hoch",
        "unknown" to "unbekannt",
        "commented" to "kommentiert",
        "out-dated" to "veraltet",
    )

    fun fee(value: String) = fees[value] ?: value
    fun access(value: String) = accesses[value] ?: value
    fun surface(value: String) = surfaces[value] ?: value

    /** Null for states PEGELONLINE may add later. */
    fun state(value: String): String? = states[value]
}

/** Links the apps show; the legal pages of the website also cover the apps. */
object Links {
    const val WEBSITE = "https://slipways.de"
    const val IMPRESSUM = "https://slipways.de/impressum"
    const val DATENSCHUTZ = "https://slipways.de/datenschutz"
    const val SUPPORT = "https://slipways.de/support"
    const val OSM_COPYRIGHT = "https://www.openstreetmap.org/copyright"
    const val PEGELONLINE = "https://www.pegelonline.wsv.de/"
    const val LICENCE = "https://github.com/Marcel-B/Slipways/blob/master/LICENSE"
    const val LICENCE_CONTACT = "mailto:slipways@marcelbenders.com"

    /** Required next to every list or page with imported entries. */
    const val OSM_ATTRIBUTION = "Daten © OpenStreetMap-Mitwirkende (ODbL)"

    fun osm(element: String) = "https://www.openstreetmap.org/$element"

    /** Phone tags from OpenStreetMap look like "+49 9441 1234"; the dial link keeps only digits and the plus. */
    fun phone(value: String): String? {
        val digits = value.filter { it.isDigit() || it == '+' }
        return if (digits.isEmpty()) null else "tel:$digits"
    }

    fun mail(value: String): String? = value.trim().takeIf { it.isNotEmpty() }?.let { "mailto:$it" }

    /** Website tags from OpenStreetMap sometimes lack the scheme, e.g. "www.leg-an.com". */
    fun web(value: String): String? {
        val text = value.trim()
        if (text.isEmpty()) return null
        return if ("://" in text) text else "https://$text"
    }
}

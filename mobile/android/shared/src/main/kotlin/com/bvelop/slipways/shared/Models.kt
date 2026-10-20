package com.bvelop.slipways.shared

import kotlinx.serialization.KSerializer
import kotlinx.serialization.Serializable
import kotlinx.serialization.descriptors.PrimitiveKind
import kotlinx.serialization.descriptors.PrimitiveSerialDescriptor
import kotlinx.serialization.encoding.Decoder
import kotlinx.serialization.encoding.Encoder
import java.math.BigDecimal

// Mirrors backend/src/Slipways.Api/Endpoints/Contracts.cs.

@Serializable
data class Water(val id: String, val name: String)

@Serializable
data class Position(val latitude: Double, val longitude: Double)

/** Where an entry comes from: our own data or OpenStreetMap (ODbL, needs attribution). */
@Serializable(with = SourceSerializer::class)
enum class Source { MANUAL, OSM }

/** Unknown future sources count as our own data, so the list still loads. */
object SourceSerializer : KSerializer<Source> {
    override val descriptor = PrimitiveSerialDescriptor("Source", PrimitiveKind.STRING)
    override fun deserialize(decoder: Decoder) = if (decoder.decodeString() == "osm") Source.OSM else Source.MANUAL
    override fun serialize(encoder: Encoder, value: Source) = encoder.encodeString(value.name.lowercase())
}

/** Latest reading at a gauge from PEGELONLINE; [value] in centimetres, [measuredAt] ISO 8601 in UTC. */
@Serializable
data class WaterLevel(val value: Double, val measuredAt: String, val state: String? = null)

@Serializable
data class GaugeReading(val stationId: String, val stationName: String, val waterLevel: WaterLevel)

/** A water for the overview; [gauge] is set only when the water has exactly one gauge with a reading. */
@Serializable
data class WaterSummary(
    val id: String,
    val name: String,
    val shortname: String? = null,
    val slipwayCount: Int = 0,
    val stationCount: Int = 0,
    val position: Position? = null,
    val gauge: GaugeReading? = null,
)

@Serializable
data class SlipwaySummary(
    val id: String,
    val name: String,
    val latitude: Double,
    val longitude: Double,
    val postalCode: String? = null,
    val city: String? = null,
    val country: String? = null,
    /** Null when the slipway lies on none of the known waters (most imported ones). */
    val water: Water? = null,
    val source: Source = Source.MANUAL,
) {
    /** "Donau · 93309 Kelheim" */
    val subtitle: String
        get() = listOfNotNull(water?.name, Format.place(postalCode, city)).joinToString(" · ")
}

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
    val url: String? = null,
    val comment: String? = null,
    val rating: Int = 0,
    /** Our own launch fee in euros; null when unknown. */
    @Serializable(with = BigDecimalSerializer::class)
    val costs: BigDecimal? = null,
    val pro: String? = null,
    val contra: String? = null,
    /** [operator] to [surface] are OpenStreetMap tags as they are, e.g. fee "yes". */
    val operator: String? = null,
    val fee: String? = null,
    val access: String? = null,
    val openingHours: String? = null,
    val surface: String? = null,
    val water: Water? = null,
    val marina: MarinaSummary? = null,
    val extras: List<String> = emptyList(),
    val source: Source = Source.MANUAL,
    /** Such as "node/123"; also set for our own slipways that were matched to OpenStreetMap. */
    val osmElement: String? = null,
) {
    val address: String
        get() = listOfNotNull(street, Format.place(postalCode, city), country).joinToString(", ")

    /** Our own costs win; imported slipways only have the OpenStreetMap fee tag. */
    val costsLabel: String
        get() = when {
            costs?.signum() == 0 || (costs == null && fee == "no") -> "kostenlos"
            costs != null -> Format.euros(costs)
            fee != null -> Labels.fee(fee)
            else -> "Kosten unbekannt"
        }
}

@Serializable
data class Marina(
    val id: String,
    val name: String,
    val latitude: Double,
    val longitude: Double,
    val url: String? = null,
    val street: String? = null,
    val postalCode: String? = null,
    val city: String? = null,
    val phone: String? = null,
    val email: String? = null,
    val operator: String? = null,
    val fee: String? = null,
    val access: String? = null,
    val openingHours: String? = null,
    val water: Water? = null,
    val source: Source = Source.MANUAL,
    val osmElement: String? = null,
)

@Serializable
data class Station(
    val id: String,
    val name: String,
    val number: String? = null,
    val latitude: Double,
    val longitude: Double,
    val km: Double = 0.0,
    val agency: String? = null,
    val water: Water,
    val waterLevel: WaterLevel? = null,
)

data class NearestGauge(val station: Station, val level: WaterLevel, val km: Double)

/** The course of the water ([WaterDetails] "shape") is not read: the Android app has no map of its own. */
@Serializable
data class WaterDetails(
    val id: String,
    val name: String,
    val shortname: String? = null,
    val osmElements: List<String> = emptyList(),
    val slipways: List<SlipwaySummary> = emptyList(),
    val stations: List<Station> = emptyList(),
    val marinas: List<Marina> = emptyList(),
) {
    /** True when the page shows anything from OpenStreetMap and so needs the attribution. */
    val usesOpenStreetMap: Boolean
        get() = slipways.any { it.source == Source.OSM } || marinas.any { it.source == Source.OSM }

    /** The gauge with a reading closest to the given point. */
    fun nearestGauge(latitude: Double, longitude: Double): NearestGauge? =
        stations
            .mapNotNull { station ->
                station.waterLevel?.let { NearestGauge(station, it, Distance.km(latitude, longitude, station.latitude, station.longitude)) }
            }
            .minByOrNull { it.km }
}

/** Reads JSON numbers such as 7.5 exactly, so 7.50 € is not shown as 7.4999… */
object BigDecimalSerializer : KSerializer<BigDecimal> {
    override val descriptor = PrimitiveSerialDescriptor("BigDecimal", PrimitiveKind.STRING)
    override fun deserialize(decoder: Decoder): BigDecimal =
        if (decoder is kotlinx.serialization.json.JsonDecoder) {
            BigDecimal((decoder.decodeJsonElement() as kotlinx.serialization.json.JsonPrimitive).content)
        } else {
            BigDecimal(decoder.decodeString())
        }

    override fun serialize(encoder: Encoder, value: BigDecimal) = encoder.encodeString(value.toPlainString())
}

internal object Distance {
    /** Haversine distance in kilometres. */
    fun km(lat1: Double, lon1: Double, lat2: Double, lon2: Double): Double {
        val rad = Math.PI / 180
        val dLat = (lat2 - lat1) * rad
        val dLon = (lon2 - lon1) * rad
        val a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos(lat1 * rad) * Math.cos(lat2 * rad) * Math.sin(dLon / 2) * Math.sin(dLon / 2)
        return 6371 * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a))
    }
}

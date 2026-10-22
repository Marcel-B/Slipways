package com.bvelop.slipways.shared

import kotlinx.serialization.KSerializer
import kotlinx.serialization.Serializable
import kotlinx.serialization.descriptors.PrimitiveKind
import kotlinx.serialization.descriptors.PrimitiveSerialDescriptor
import kotlinx.serialization.encoding.Decoder
import kotlinx.serialization.encoding.Encoder
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.addJsonObject
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.put
import kotlinx.serialization.json.putJsonArray
import kotlinx.serialization.json.putJsonObject
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
    /** ISO codes of the countries its slipways, marinas and gauges lie in, the one with the most first. */
    val countries: List<String> = emptyList(),
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
    val updated: String? = null,
    /** Values from users, such as facts and tips; they win over the slipway's own. */
    val infos: List<PlaceInfo> = emptyList(),
) {
    val address: String
        get() = listOfNotNull(street, Format.place(postalCode, city), country).joinToString(", ")

    /** The value users sent in for a field, else the slipway's own (for imports from OpenStreetMap). */
    fun value(field: String): SourcedValue? {
        Contributions.fromUsers(infos, field)?.let { return SourcedValue(it.value, it) }
        val own = when (field) {
            "fee" -> fee
            "costs" -> costs?.toPlainString()
            "operator" -> operator
            "access" -> access
            "opening_hours" -> openingHours
            "surface" -> surface
            else -> null
        }
        return own?.let { SourcedValue(it, null) }
    }

    /** Costs win over the fee; values from users over our own and those from OpenStreetMap. */
    val costsLabel: String
        get() {
            val costs = value("costs")?.value?.toBigDecimalOrNull()
            val fee = value("fee")?.value
            return when {
                costs?.signum() == 0 || (costs == null && fee == "no") -> "kostenlos"
                costs != null -> Format.euros(costs)
                fee != null -> Labels.fee(fee)
                else -> "Kosten unbekannt"
            }
        }

    /** Facts users ticked, such as a jetty or a crane. */
    val ticked: List<TickedFact>
        get() = Contributions.FACTS.mapNotNull { field ->
            Contributions.fromUsers(infos, field)?.let { TickedFact(field, it.value == "yes", it.retrieved) }
        }

    val tips: List<PlaceInfo>
        get() = Contributions.tips(infos)
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
    /** ISO code of the country the import found it in, for our own marinas that of their slipways. */
    val country: String? = null,
) {
    /** "Hafenstraße 1, 93309 Kelheim" */
    val address: String
        get() = listOfNotNull(street, Format.place(postalCode, city)).joinToString(", ")
}

/** A marina with its slipways and details from other sources, such as its website or users. */
@Serializable
data class MarinaDetails(
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
    val slipways: List<SlipwaySummary> = emptyList(),
    val infos: List<PlaceInfo> = emptyList(),
) {
    /** "Hafenstraße 1, 93309 Kelheim" */
    val address: String
        get() = listOfNotNull(street, Format.place(postalCode, city)).joinToString(", ")

    /** Values from users win; details from the marina's website only fill gaps after its own. */
    fun value(field: String): SourcedValue? {
        Contributions.fromUsers(infos, field)?.let { return SourcedValue(it.value, it) }
        val own = when (field) {
            "phone" -> phone
            "email" -> email
            "operator" -> operator
            "fee" -> fee
            "access" -> access
            "opening_hours" -> openingHours
            else -> null
        }
        if (own != null) return SourcedValue(own, null)
        return infos.firstOrNull { it.field == field && it.source == "website" }?.let { SourcedValue(it.value, it) }
    }

    val tips: List<PlaceInfo>
        get() = Contributions.tips(infos)

    /** True when the page shows anything from OpenStreetMap and so needs the attribution. */
    val usesOpenStreetMap: Boolean
        get() = source == Source.OSM || slipways.any { it.source == Source.OSM }
}

/**
 * A detail such as a phone number from [source] ("website": read from [sourceUrl], "user": an accepted suggestion),
 * as it was on [retrieved] (ISO 8601). [field] is e.g. "phone", "opening_hours", "jetty" or "tip".
 */
@Serializable
data class PlaceInfo(
    val field: String,
    val value: String,
    val source: String,
    val sourceUrl: String? = null,
    val retrieved: String,
)

/** A value a place shows; [info] says where it comes from when it is not the place's own. */
data class SourcedValue(val value: String, val info: PlaceInfo?) {
    /** "von Nutzern ergänzt, Stand 10.2026" or "laut Webseite, Stand 20.10.2026"; null for the place's own value. */
    val note: String?
        get() = when {
            info == null -> null
            info.source == "website" -> "laut Webseite, Stand ${Format.date(info.retrieved)}"
            else -> "von Nutzern ergänzt, Stand ${Format.monthYear(info.retrieved)}"
        }
}

/** A fact users ticked for a slipway; [present] is false for an explicit "no". */
data class TickedFact(val field: String, val present: Boolean, val retrieved: String) {
    /** "Steg" or "Steg: nein" */
    val label: String
        get() = Contributions.label(field).let { if (present) it else "$it: nein" }
}

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

@Serializable
data class Bounds(val south: Double, val west: Double, val north: Double, val east: Double)

/**
 * [shape] is the simplified course or area from OpenStreetMap (ODbL) as GeoJSON geometry, handed to the map as it is;
 * null when the water has none. [bounds] covers the shape and everything on the water.
 */
@Serializable
data class WaterDetails(
    val id: String,
    val name: String,
    val shortname: String? = null,
    val bounds: Bounds? = null,
    val shape: JsonElement? = null,
    val osmElements: List<String> = emptyList(),
    val slipways: List<SlipwaySummary> = emptyList(),
    val stations: List<Station> = emptyList(),
    val marinas: List<Marina> = emptyList(),
) {
    /** True when the page shows anything from OpenStreetMap and so needs the attribution. */
    val usesOpenStreetMap: Boolean
        get() = shape != null || slipways.any { it.source == Source.OSM } || marinas.any { it.source == Source.OSM }

    /** The gauge with a reading closest to the given point. */
    fun nearestGauge(latitude: Double, longitude: Double): NearestGauge? =
        stations
            .mapNotNull { station ->
                station.waterLevel?.let { NearestGauge(station, it, Distance.km(latitude, longitude, station.latitude, station.longitude)) }
            }
            .minByOrNull { it.km }
}

/** Water shapes for an overview map (OpenStreetMap, ODbL) from a GeoJSON FeatureCollection; feature ids are water ids. */
@Serializable
data class WaterShapes(val features: List<WaterShape> = emptyList()) {
    /** The shapes as GeoJSON for the map, only those of [ids] when given; each feature keeps its water id. */
    fun toGeoJson(ids: Set<String>? = null): String = buildJsonObject {
        put("type", "FeatureCollection")
        putJsonArray("features") {
            features.filter { ids == null || it.id in ids }.forEach { shape ->
                addJsonObject {
                    put("type", "Feature")
                    put("id", shape.id)
                    putJsonObject("properties") {
                        put("id", shape.id)
                        put("name", shape.properties.name)
                    }
                    put("geometry", shape.geometry)
                }
            }
        }
    }.toString()
}

@Serializable
data class WaterShape(val id: String, val properties: WaterShapeProperties, val geometry: JsonElement)

@Serializable
data class WaterShapeProperties(val name: String)

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

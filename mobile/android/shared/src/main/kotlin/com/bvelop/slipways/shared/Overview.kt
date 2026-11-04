package com.bvelop.slipways.shared

import java.text.Normalizer

/** A rectangle on the map, as the visible part of the overview map. */
data class MapArea(val south: Double, val west: Double, val north: Double, val east: Double) {
    val centerLatitude get() = (south + north) / 2
    val centerLongitude get() = (west + east) / 2

    fun contains(latitude: Double, longitude: Double) = latitude in south..north && longitude in west..east

    /**
     * True when this view at [zoom] shows about the same as [other] at [otherZoom]: zoom levels a tenth apart at most
     * and centres less than a fiftieth of the view apart. A map that only changes size keeps its view.
     */
    fun sameView(zoom: Double, other: MapArea, otherZoom: Double): Boolean {
        if (kotlin.math.abs(zoom - otherZoom) > 0.1) return false
        val width = kotlin.math.abs(other.east - other.west)
        val height = kotlin.math.abs(other.north - other.south)
        return kotlin.math.abs(centerLongitude - other.centerLongitude) <= width / 50 &&
            kotlin.math.abs(centerLatitude - other.centerLatitude) <= height / 50
    }

    companion object {
        /** Europe as the map shows it at start, one bubble per country like on the website. */
        val START = MapArea(south = 36.0, west = -10.0, north = 64.0, east = 25.0)

        /** The smallest area around the points with a margin, at least about 2 km wide; null without points. */
        fun fitting(points: List<Position>): MapArea? {
            if (points.isEmpty()) return null
            val south = points.minOf { it.latitude }
            val north = points.maxOf { it.latitude }
            val west = points.minOf { it.longitude }
            val east = points.maxOf { it.longitude }
            val latitudeDelta = maxOf((north - south) * 1.3, 0.02)
            val longitudeDelta = maxOf((east - west) * 1.3, 0.03)
            val lat = (south + north) / 2
            val lon = (west + east) / 2
            return MapArea(lat - latitudeDelta / 2, lon - longitudeDelta / 2, lat + latitudeDelta / 2, lon + longitudeDelta / 2)
        }
    }
}

/** What the overview shows: thousands of pins help nobody, so only a zoomed-in area lists its entries. */
object Overview {
    /** More entries than this in the visible area ask the user to zoom in or search. */
    const val LIMIT = 250

    /**
     * Up to this zoom the map shows one bubble per country instead of clusters (`COUNTRY_MAX_ZOOM` on the website).
     * Zoom levels here count tiles of 256 points like the website; MapLibre's are one less (tiles of 512).
     */
    const val COUNTRY_MAX_ZOOM = 5

    /** The entries inside the area, or null when there are more than [limit]. */
    fun <T> visible(items: List<T>, area: MapArea, limit: Int = LIMIT, position: (T) -> Position): List<T>? {
        val result = mutableListOf<T>()
        for (item in items) {
            val point = position(item)
            if (!area.contains(point.latitude, point.longitude)) continue
            result += item
            if (result.size > limit) return null
        }
        return result
    }

    /** Case- and accent-insensitive match on name, city, postal code or water, as the API search does for slipways. */
    fun matches(marina: Marina, search: String): Boolean {
        val term = fold(search.trim())
        if (term.isEmpty()) return true
        return listOfNotNull(marina.name, marina.city, marina.postalCode, marina.water?.name).any { term in fold(it) }
    }

    private fun fold(text: String) =
        Normalizer.normalize(text, Normalizer.Form.NFD).replace(Regex("\\p{Mn}+"), "").lowercase()
}

/**
 * Which water shapes to load for a map view (`shapeArea` on the website): those reaching into it, widened to a grid so
 * small moves reuse what was loaded, and further out only those large enough to see, about 20 points. [zoom] counts
 * tiles of 256 points like the website.
 */
data class ShapeArea(val bbox: String, val minExtent: Double) {
    companion object {
        /** From this zoom on every water is drawn, however small. */
        const val ALL_ZOOM = 12

        fun of(area: MapArea, zoom: Double): ShapeArea {
            val level = maxOf(0, Math.floor(zoom).toInt())
            // About one tile of 256 points at this zoom.
            val step = 360 / Math.pow(2.0, level.toDouble())
            fun snap(value: Double, round: (Double) -> Double, low: Double, high: Double) =
                minOf(high, maxOf(low, round(value / step) * step))
            val bbox = listOf(
                snap(area.west - step, Math::floor, -180.0, 180.0),
                snap(area.south - step, Math::floor, -90.0, 90.0),
                snap(area.east + step, Math::ceil, -180.0, 180.0),
                snap(area.north + step, Math::ceil, -90.0, 90.0),
            ).joinToString(",") { text(it) }
            val minExtent = if (level >= ALL_ZOOM) 0.0 else Math.round(28 / Math.pow(2.0, level.toDouble()) * 10_000) / 10_000.0
            return ShapeArea(bbox, minExtent)
        }

        /** Up to four decimals and no trailing zeros, like the website, so both ask for the same, cacheable URLs. */
        fun text(value: Double): String =
            java.math.BigDecimal(Math.round(value * 10_000) / 10_000.0).setScale(4, java.math.RoundingMode.HALF_UP)
                .stripTrailingZeros().toPlainString()
    }
}

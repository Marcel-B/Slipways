package com.bvelop.slipways.shared

import java.text.Normalizer

/** A rectangle on the map, as the visible part of the overview map. */
data class MapArea(val south: Double, val west: Double, val north: Double, val east: Double) {
    val centerLatitude get() = (south + north) / 2
    val centerLongitude get() = (west + east) / 2

    fun contains(latitude: Double, longitude: Double) = latitude in south..north && longitude in west..east

    companion object {
        /** Germany and its neighbours as the map shows them at start. */
        val START = MapArea(south = 46.0, west = 4.5, north = 55.5, east = 16.5)

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

package com.bvelop.slipways.ui.overview

import android.graphics.Bitmap
import android.graphics.Canvas
import android.graphics.Paint
import android.graphics.RectF
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberUpdatedState
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.viewinterop.AndroidView
import com.bvelop.slipways.shared.Countries
import com.bvelop.slipways.shared.Format
import com.bvelop.slipways.shared.MapArea
import com.bvelop.slipways.shared.Overview
import com.bvelop.slipways.shared.Position
import com.bvelop.slipways.ui.map.MapTarget
import com.bvelop.slipways.ui.map.STYLE_URL
import com.bvelop.slipways.ui.map.WEB_ZOOM_OFFSET
import com.bvelop.slipways.ui.map.rememberMapView
import com.bvelop.slipways.ui.map.toBounds
import com.bvelop.slipways.ui.map.visibleArea
import com.bvelop.slipways.ui.map.webZoom
import org.maplibre.android.camera.CameraUpdateFactory
import org.maplibre.android.geometry.LatLng
import org.maplibre.android.maps.MapLibreMap
import org.maplibre.android.maps.Style
import org.maplibre.android.style.expressions.Expression.concat
import org.maplibre.android.style.expressions.Expression.get
import org.maplibre.android.style.expressions.Expression.has
import org.maplibre.android.style.expressions.Expression.literal
import org.maplibre.android.style.expressions.Expression.not
import org.maplibre.android.style.expressions.Expression.step
import org.maplibre.android.style.expressions.Expression.stop
import org.maplibre.android.style.layers.CircleLayer
import org.maplibre.android.style.layers.Layer
import org.maplibre.android.style.layers.PropertyFactory.circleColor
import org.maplibre.android.style.layers.PropertyFactory.circleRadius
import org.maplibre.android.style.layers.PropertyFactory.circleStrokeColor
import org.maplibre.android.style.layers.PropertyFactory.circleStrokeWidth
import org.maplibre.android.style.layers.PropertyFactory.iconAllowOverlap
import org.maplibre.android.style.layers.PropertyFactory.iconAnchor
import org.maplibre.android.style.layers.PropertyFactory.iconIgnorePlacement
import org.maplibre.android.style.layers.PropertyFactory.iconImage
import org.maplibre.android.style.layers.Property
import org.maplibre.android.style.layers.PropertyFactory.textAllowOverlap
import org.maplibre.android.style.layers.PropertyFactory.textAnchor
import org.maplibre.android.style.layers.PropertyFactory.textColor
import org.maplibre.android.style.layers.PropertyFactory.textField
import org.maplibre.android.style.layers.PropertyFactory.textFont
import org.maplibre.android.style.layers.PropertyFactory.textIgnorePlacement
import org.maplibre.android.style.layers.PropertyFactory.textSize
import org.maplibre.android.style.layers.SymbolLayer
import org.maplibre.android.style.sources.GeoJsonOptions
import org.maplibre.android.style.sources.GeoJsonSource
import org.maplibre.geojson.Feature
import org.maplibre.geojson.FeatureCollection
import org.maplibre.geojson.Point

private const val PLACES = "places"
private const val CLUSTERS = "clusters"
private const val COUNTS = "counts"
private const val PINS = "pins"
private const val COUNTRIES = "countries"
private const val COUNTRY_CIRCLES = "country-circles"
private const val COUNTRY_LABELS = "country-labels"
private const val FLAG_PREFIX = "flag-"

/** Zoomed out further than this (in MapLibre's levels), the map shows countries instead of clusters. */
private val COUNTRY_LAYERS_MAX_ZOOM = (Overview.COUNTRY_MAX_ZOOM + 1 - WEB_ZOOM_OFFSET).toFloat()

/** A dot on the overview map. */
data class MapPin(val id: String, val position: Position)

/** One bubble per country while the map shows a continent; a tap zooms to [area]. */
data class CountryBubble(val code: String, val count: Int, val center: Position, val area: MapArea)

/**
 * MapLibre map with a dot per pin; MapLibre groups nearby dots into bubbles with a count, so thousands of pins stay
 * fast. Zoomed out, [countries] (two or more) take the place of those bubbles, as on the website. [fitTo] moves the
 * camera when it changes; every finished camera move reports the visible area and the zoom as the website counts it.
 */
@Composable
fun OverviewMap(
    pins: List<MapPin>,
    countries: List<CountryBubble>,
    color: String,
    fitTo: MapTarget?,
    onAreaChange: (MapArea, Double) -> Unit,
    onPinClick: (String) -> Unit,
    modifier: Modifier = Modifier,
) {
    // Fingers are less precise than the dots, so taps hit anything within this many pixels.
    val touchSlop = with(LocalDensity.current) { 16f * density }
    val flagSize = with(LocalDensity.current) { 18f * density }
    val mapView = rememberMapView()
    var map by remember { mutableStateOf<MapLibreMap?>(null) }
    var style by remember { mutableStateOf<Style?>(null) }
    val currentOnAreaChange by rememberUpdatedState(onAreaChange)
    val currentOnPinClick by rememberUpdatedState(onPinClick)
    val currentCountries by rememberUpdatedState(countries)

    LaunchedEffect(mapView) {
        mapView.getMapAsync { loaded ->
            var loadedStyle: Style? = null
            loaded.uiSettings.isRotateGesturesEnabled = false
            loaded.uiSettings.isTiltGesturesEnabled = false
            loaded.moveCamera(CameraUpdateFactory.newLatLngBounds(MapArea.START.toBounds(), 0))
            loaded.setStyle(Style.Builder().fromUri(STYLE_URL)) { ready ->
                val options = GeoJsonOptions().withCluster(true).withClusterRadius(50).withClusterMaxZoom(13)
                ready.addSource(GeoJsonSource(PLACES, FeatureCollection.fromFeatures(emptyList()), options))
                ready.addLayer(
                    CircleLayer(CLUSTERS, PLACES).withFilter(has("point_count")).withProperties(
                        circleColor(color),
                        circleRadius(step(get("point_count"), literal(14), stop(50, 18), stop(500, 24))),
                        circleStrokeColor("#FFFFFF"),
                        circleStrokeWidth(2f),
                    ),
                )
                ready.addLayer(
                    SymbolLayer(COUNTS, PLACES).withFilter(has("point_count")).withProperties(
                        textField(get("point_count_abbreviated")),
                        textFont(arrayOf("Noto Sans Bold")),
                        textSize(12f),
                        textColor("#FFFFFF"),
                        textAllowOverlap(true),
                        textIgnorePlacement(true),
                    ),
                )
                ready.addLayer(
                    CircleLayer(PINS, PLACES).withFilter(not(has("point_count"))).withProperties(
                        circleColor(color),
                        circleRadius(7f),
                        circleStrokeColor("#FFFFFF"),
                        circleStrokeWidth(1.5f),
                    ),
                )
                ready.addSource(GeoJsonSource(COUNTRIES, FeatureCollection.fromFeatures(emptyList())))
                ready.addLayer(
                    CircleLayer(COUNTRY_CIRCLES, COUNTRIES).withProperties(
                        circleColor(color),
                        circleRadius(26f),
                        circleStrokeColor("#FFFFFF"),
                        circleStrokeWidth(2f),
                    ).apply { maxZoom = COUNTRY_LAYERS_MAX_ZOOM },
                )
                // The map's fonts have no emojis, so the flag is an image drawn by the system above the count.
                ready.addLayer(
                    SymbolLayer(COUNTRY_LABELS, COUNTRIES).withProperties(
                        iconImage(concat(literal(FLAG_PREFIX), get("code"))),
                        iconAnchor(Property.ICON_ANCHOR_BOTTOM),
                        iconAllowOverlap(true),
                        iconIgnorePlacement(true),
                        textField(get("count")),
                        textAnchor(Property.TEXT_ANCHOR_TOP),
                        textFont(arrayOf("Noto Sans Bold")),
                        textSize(12f),
                        textColor("#FFFFFF"),
                        textAllowOverlap(true),
                        textIgnorePlacement(true),
                    ).apply { maxZoom = COUNTRY_LAYERS_MAX_ZOOM },
                )
                loadedStyle = ready
                style = ready
            }
            loaded.addOnCameraIdleListener { currentOnAreaChange(loaded.visibleArea(), loaded.webZoom) }
            loaded.addOnMapClickListener { point ->
                val screen = loaded.projection.toScreenLocation(point)
                val touched = RectF(screen.x - touchSlop, screen.y - touchSlop, screen.x + touchSlop, screen.y + touchSlop)
                val country = loaded.queryRenderedFeatures(touched, COUNTRY_CIRCLES).firstOrNull()?.getStringProperty("code")
                    ?.let { code -> currentCountries.firstOrNull { it.code == code } }
                val cluster = loaded.queryRenderedFeatures(touched, CLUSTERS).firstOrNull()
                val pin = loaded.queryRenderedFeatures(touched, PINS).firstOrNull()?.getStringProperty("id")
                val center = cluster?.geometry() as? Point
                when {
                    country != null -> {
                        loaded.animateCamera(CameraUpdateFactory.newLatLngBounds(country.area.toBounds(), 48))
                        true
                    }
                    cluster != null && center != null -> {
                        val zoom = loadedStyle?.getSourceAs<GeoJsonSource>(PLACES)?.getClusterExpansionZoom(cluster)
                            ?: (loaded.cameraPosition.zoom + 2).toInt()
                        loaded.animateCamera(CameraUpdateFactory.newLatLngZoom(LatLng(center.latitude(), center.longitude()), zoom.toDouble()))
                        true
                    }
                    pin != null -> {
                        currentOnPinClick(pin)
                        true
                    }
                    else -> false
                }
            }
            map = loaded
        }
    }

    // Only a new search, a chosen country or a reload changes the data; panning only changes what the list shows.
    LaunchedEffect(style, pins) {
        style?.getSourceAs<GeoJsonSource>(PLACES)?.setGeoJson(
            FeatureCollection.fromFeatures(
                pins.map { pin ->
                    Feature.fromGeometry(Point.fromLngLat(pin.position.longitude, pin.position.latitude)).apply {
                        addStringProperty("id", pin.id)
                    }
                },
            ),
        )
    }
    LaunchedEffect(style, countries) {
        val ready = style ?: return@LaunchedEffect
        // With a single country its bubble would say nothing; the clusters show then at every zoom.
        val byCountry = countries.size > 1
        for (country in countries) {
            if (ready.getImage(FLAG_PREFIX + country.code) != null) continue
            val flag = Countries.flag(country.code) ?: country.code
            ready.addImage(FLAG_PREFIX + country.code, flagBitmap(flag, flagSize))
        }
        ready.getSourceAs<GeoJsonSource>(COUNTRIES)?.setGeoJson(
            FeatureCollection.fromFeatures(
                if (!byCountry) {
                    emptyList()
                } else {
                    countries.map { country ->
                        Feature.fromGeometry(Point.fromLngLat(country.center.longitude, country.center.latitude)).apply {
                            addStringProperty("code", country.code)
                            addStringProperty("count", Format.number(country.count))
                        }
                    }
                },
            ),
        )
        listOf(CLUSTERS, COUNTS, PINS).mapNotNull { ready.getLayer(it) }.forEach { layer: Layer ->
            layer.minZoom = if (byCountry) COUNTRY_LAYERS_MAX_ZOOM else 0f
        }
    }
    LaunchedEffect(map, fitTo) {
        val target = fitTo ?: return@LaunchedEffect
        map?.animateCamera(CameraUpdateFactory.newLatLngBounds(target.area.toBounds(), target.padding))
    }

    AndroidView(factory = { mapView }, modifier = modifier)
}

/** The flag emoji (or, without one, the ISO code) as an image, since the map's own fonts have no emojis. */
private fun flagBitmap(flag: String, size: Float): Bitmap {
    val paint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        textSize = size
        color = android.graphics.Color.WHITE
        isFakeBoldText = flag.length == 2
    }
    val metrics = paint.fontMetrics
    val width = paint.measureText(flag).coerceAtLeast(1f)
    val height = metrics.descent - metrics.ascent
    val bitmap = Bitmap.createBitmap(kotlin.math.ceil(width).toInt(), kotlin.math.ceil(height).toInt(), Bitmap.Config.ARGB_8888)
    Canvas(bitmap).drawText(flag, 0f, -metrics.ascent, paint)
    return bitmap
}

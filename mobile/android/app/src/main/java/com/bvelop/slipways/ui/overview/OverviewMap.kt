package com.bvelop.slipways.ui.overview

import android.graphics.RectF
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberUpdatedState
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.viewinterop.AndroidView
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import androidx.lifecycle.compose.LocalLifecycleOwner
import com.bvelop.slipways.shared.MapArea
import com.bvelop.slipways.shared.Position
import org.maplibre.android.camera.CameraUpdateFactory
import org.maplibre.android.geometry.LatLng
import org.maplibre.android.geometry.LatLngBounds
import org.maplibre.android.maps.MapLibreMap
import org.maplibre.android.maps.MapView
import org.maplibre.android.maps.Style
import org.maplibre.android.style.expressions.Expression.get
import org.maplibre.android.style.expressions.Expression.has
import org.maplibre.android.style.expressions.Expression.literal
import org.maplibre.android.style.expressions.Expression.not
import org.maplibre.android.style.expressions.Expression.step
import org.maplibre.android.style.expressions.Expression.stop
import org.maplibre.android.style.layers.CircleLayer
import org.maplibre.android.style.layers.PropertyFactory.circleColor
import org.maplibre.android.style.layers.PropertyFactory.circleRadius
import org.maplibre.android.style.layers.PropertyFactory.circleStrokeColor
import org.maplibre.android.style.layers.PropertyFactory.circleStrokeWidth
import org.maplibre.android.style.layers.PropertyFactory.textAllowOverlap
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

/** OpenFreeMap vector tiles (OpenStreetMap data); the style brings its own attribution. */
private const val STYLE_URL = "https://tiles.openfreemap.org/styles/liberty"
private const val PLACES = "places"
private const val CLUSTERS = "clusters"
private const val COUNTS = "counts"
private const val PINS = "pins"

/** A dot on the overview map. */
data class MapPin(val id: String, val position: Position)

/**
 * MapLibre map with a dot per pin; MapLibre groups nearby dots into bubbles with a count, so thousands of pins stay
 * fast. [fitTo] moves the camera when it changes; every finished camera move reports the visible area.
 */
@Composable
fun OverviewMap(
    pins: List<MapPin>,
    color: String,
    fitTo: MapArea?,
    onAreaChange: (MapArea) -> Unit,
    onPinClick: (String) -> Unit,
    modifier: Modifier = Modifier,
) {
    val context = LocalContext.current
    val lifecycle = LocalLifecycleOwner.current.lifecycle
    // Fingers are less precise than the dots, so taps hit anything within this many pixels.
    val touchSlop = with(LocalDensity.current) { 16f * density }
    val mapView = remember { MapView(context).apply { onCreate(null) } }
    var map by remember { mutableStateOf<MapLibreMap?>(null) }
    var style by remember { mutableStateOf<Style?>(null) }
    val currentOnAreaChange by rememberUpdatedState(onAreaChange)
    val currentOnPinClick by rememberUpdatedState(onPinClick)

    DisposableEffect(lifecycle, mapView) {
        val observer = LifecycleEventObserver { _, event ->
            when (event) {
                Lifecycle.Event.ON_START -> mapView.onStart()
                Lifecycle.Event.ON_RESUME -> mapView.onResume()
                Lifecycle.Event.ON_PAUSE -> mapView.onPause()
                Lifecycle.Event.ON_STOP -> mapView.onStop()
                else -> Unit
            }
        }
        lifecycle.addObserver(observer)
        onDispose {
            lifecycle.removeObserver(observer)
            mapView.onDestroy()
        }
    }

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
                loadedStyle = ready
                style = ready
            }
            loaded.addOnCameraIdleListener {
                val bounds = loaded.projection.visibleRegion.latLngBounds
                currentOnAreaChange(MapArea(bounds.latitudeSouth, bounds.longitudeWest, bounds.latitudeNorth, bounds.longitudeEast))
            }
            loaded.addOnMapClickListener { point ->
                val screen = loaded.projection.toScreenLocation(point)
                val touched = RectF(screen.x - touchSlop, screen.y - touchSlop, screen.x + touchSlop, screen.y + touchSlop)
                val cluster = loaded.queryRenderedFeatures(touched, CLUSTERS).firstOrNull()
                val pin = loaded.queryRenderedFeatures(touched, PINS).firstOrNull()?.getStringProperty("id")
                val center = cluster?.geometry() as? Point
                when {
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

    // Only a new search or a reload changes the data; panning only changes what the list shows.
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
    LaunchedEffect(map, fitTo) {
        val target = fitTo ?: return@LaunchedEffect
        map?.animateCamera(CameraUpdateFactory.newLatLngBounds(target.toBounds(), 48))
    }

    AndroidView(factory = { mapView }, modifier = modifier)
}

private fun MapArea.toBounds(): LatLngBounds =
    LatLngBounds.Builder().include(LatLng(south, west)).include(LatLng(north, east)).build()

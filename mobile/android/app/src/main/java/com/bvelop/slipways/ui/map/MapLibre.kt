package com.bvelop.slipways.ui.map

import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.remember
import androidx.compose.ui.platform.LocalContext
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import androidx.lifecycle.compose.LocalLifecycleOwner
import com.bvelop.slipways.shared.MapArea
import org.maplibre.android.geometry.LatLng
import org.maplibre.android.geometry.LatLngBounds
import org.maplibre.android.maps.MapLibreMap
import org.maplibre.android.maps.MapView

/** OpenFreeMap vector tiles (OpenStreetMap data); the style brings its own attribution. */
const val STYLE_URL = "https://tiles.openfreemap.org/styles/liberty"

/** MapLibre counts tiles of 512 points; the website and the shared code count tiles of 256, one zoom level more. */
const val WEB_ZOOM_OFFSET = 1.0

/**
 * A camera move the screen asks for. Every new object moves the map, even to the area of the last one, after the user
 * has panned away. A [padding] of 0 shows exactly the start view again.
 */
class MapTarget(val area: MapArea, val padding: Int = 48)

/** Colours of the pins, as on the website's map. */
object PinColors {
    const val SLIPWAY = "#0B5394"
    const val MARINA = "#0D9488"
    const val STATION = "#E69500"
    const val WATER = "#1E88E5"
    const val SELECTED = "#F4511E"
}

/** A MapView that follows the lifecycle of the screen it is on. */
@Composable
fun rememberMapView(): MapView {
    val context = LocalContext.current
    val lifecycle = LocalLifecycleOwner.current.lifecycle
    val mapView = remember { MapView(context).apply { onCreate(null) } }
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
    return mapView
}

fun MapArea.toBounds(): LatLngBounds =
    LatLngBounds.Builder().include(LatLng(south, west)).include(LatLng(north, east)).build()

fun MapLibreMap.visibleArea(): MapArea {
    val bounds = projection.visibleRegion.latLngBounds
    return MapArea(bounds.latitudeSouth, bounds.longitudeWest, bounds.latitudeNorth, bounds.longitudeEast)
}

/** The zoom as the website and the shared code count it. */
val MapLibreMap.webZoom: Double
    get() = cameraPosition.zoom + WEB_ZOOM_OFFSET

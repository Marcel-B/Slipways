package com.bvelop.slipways.ui.overview

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
import androidx.compose.ui.viewinterop.AndroidView
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import androidx.lifecycle.compose.LocalLifecycleOwner
import com.bvelop.slipways.shared.MapArea
import com.bvelop.slipways.shared.Marina
import com.bvelop.slipways.shared.SlipwaySummary
import org.maplibre.android.camera.CameraUpdateFactory
import org.maplibre.android.geometry.LatLng
import org.maplibre.android.geometry.LatLngBounds
import org.maplibre.android.maps.MapLibreMap
import org.maplibre.android.maps.MapView
import org.maplibre.android.maps.Style
import org.maplibre.android.style.layers.CircleLayer
import org.maplibre.android.style.layers.PropertyFactory.circleColor
import org.maplibre.android.style.layers.PropertyFactory.circleRadius
import org.maplibre.android.style.layers.PropertyFactory.circleStrokeColor
import org.maplibre.android.style.layers.PropertyFactory.circleStrokeWidth
import org.maplibre.android.style.sources.GeoJsonSource
import org.maplibre.geojson.Feature
import org.maplibre.geojson.FeatureCollection
import org.maplibre.geojson.Point

/** OpenFreeMap vector tiles (OpenStreetMap data); the style brings its own attribution. */
private const val STYLE_URL = "https://tiles.openfreemap.org/styles/liberty"
private const val SLIPWAYS = "slipways"
private const val MARINAS = "marinas"

/**
 * MapLibre map with a dot per slipway and marina. [fitTo] moves the camera when it changes; every finished
 * camera move reports the visible area.
 */
@Composable
fun OverviewMap(
    slipways: List<SlipwaySummary>,
    marinas: List<Marina>,
    fitTo: MapArea?,
    onAreaChange: (MapArea) -> Unit,
    onSlipwayClick: (String) -> Unit,
    onMarinaClick: (String) -> Unit,
    modifier: Modifier = Modifier,
) {
    val context = LocalContext.current
    val lifecycle = LocalLifecycleOwner.current.lifecycle
    val mapView = remember { MapView(context).apply { onCreate(null) } }
    var map by remember { mutableStateOf<MapLibreMap?>(null) }
    var style by remember { mutableStateOf<Style?>(null) }
    val currentOnAreaChange by rememberUpdatedState(onAreaChange)
    val currentOnSlipwayClick by rememberUpdatedState(onSlipwayClick)
    val currentOnMarinaClick by rememberUpdatedState(onMarinaClick)

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
            loaded.uiSettings.isRotateGesturesEnabled = false
            loaded.moveCamera(CameraUpdateFactory.newLatLngBounds(MapArea.START.toBounds(), 0))
            loaded.setStyle(Style.Builder().fromUri(STYLE_URL)) { ready ->
                ready.addSource(GeoJsonSource(SLIPWAYS, FeatureCollection.fromFeatures(emptyList())))
                ready.addSource(GeoJsonSource(MARINAS, FeatureCollection.fromFeatures(emptyList())))
                ready.addLayer(dots(MARINAS, "#0D9488"))
                ready.addLayer(dots(SLIPWAYS, "#0B5394"))
                style = ready
            }
            loaded.addOnCameraIdleListener {
                val bounds = loaded.projection.visibleRegion.latLngBounds
                currentOnAreaChange(MapArea(bounds.latitudeSouth, bounds.longitudeWest, bounds.latitudeNorth, bounds.longitudeEast))
            }
            loaded.addOnMapClickListener { point ->
                val screen = loaded.projection.toScreenLocation(point)
                val slipway = loaded.queryRenderedFeatures(screen, SLIPWAYS).firstOrNull()?.getStringProperty("id")
                val marina = loaded.queryRenderedFeatures(screen, MARINAS).firstOrNull()?.getStringProperty("id")
                when {
                    slipway != null -> {
                        currentOnSlipwayClick(slipway)
                        true
                    }
                    marina != null -> {
                        currentOnMarinaClick(marina)
                        true
                    }
                    else -> false
                }
            }
            map = loaded
        }
    }

    LaunchedEffect(style, slipways) {
        style?.getSourceAs<GeoJsonSource>(SLIPWAYS)?.setGeoJson(features(slipways.map { Triple(it.id, it.latitude, it.longitude) }))
    }
    LaunchedEffect(style, marinas) {
        style?.getSourceAs<GeoJsonSource>(MARINAS)?.setGeoJson(features(marinas.map { Triple(it.id, it.latitude, it.longitude) }))
    }
    LaunchedEffect(map, fitTo) {
        val target = fitTo ?: return@LaunchedEffect
        map?.animateCamera(CameraUpdateFactory.newLatLngBounds(target.toBounds(), 48))
    }

    AndroidView(factory = { mapView }, modifier = modifier)
}

private fun dots(source: String, color: String) = CircleLayer(source, source).withProperties(
    circleColor(color),
    circleRadius(6f),
    circleStrokeColor("#FFFFFF"),
    circleStrokeWidth(1.5f),
)

private fun features(places: List<Triple<String, Double, Double>>): FeatureCollection =
    FeatureCollection.fromFeatures(
        places.map { (id, latitude, longitude) ->
            Feature.fromGeometry(Point.fromLngLat(longitude, latitude)).apply { addStringProperty("id", id) }
        },
    )

private fun MapArea.toBounds(): LatLngBounds =
    LatLngBounds.Builder().include(LatLng(south, west)).include(LatLng(north, east)).build()


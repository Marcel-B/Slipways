package com.bvelop.slipways.ui.waters

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
import com.bvelop.slipways.shared.MapArea
import com.bvelop.slipways.ui.map.PinColors
import com.bvelop.slipways.ui.map.STYLE_URL
import com.bvelop.slipways.ui.map.isArea
import com.bvelop.slipways.ui.map.rememberMapView
import com.bvelop.slipways.ui.map.toBounds
import com.bvelop.slipways.ui.map.visibleArea
import com.bvelop.slipways.ui.map.webZoom
import org.maplibre.android.camera.CameraUpdateFactory
import org.maplibre.android.maps.MapLibreMap
import org.maplibre.android.maps.Style
import org.maplibre.android.style.expressions.Expression
import org.maplibre.android.style.expressions.Expression.eq
import org.maplibre.android.style.expressions.Expression.get
import org.maplibre.android.style.expressions.Expression.literal
import org.maplibre.android.style.expressions.Expression.switchCase
import org.maplibre.android.style.layers.FillLayer
import org.maplibre.android.style.layers.LineLayer
import org.maplibre.android.style.layers.PropertyFactory.fillColor
import org.maplibre.android.style.layers.PropertyFactory.fillOpacity
import org.maplibre.android.style.layers.PropertyFactory.lineColor
import org.maplibre.android.style.layers.PropertyFactory.lineWidth
import org.maplibre.android.style.sources.GeoJsonSource

private const val SHAPES = "water-shapes"
private const val FILLS = "water-fills"
private const val LINES = "water-lines"

/**
 * MapLibre map with the courses and areas of waters, given as GeoJSON whose features carry the water id; a tap selects
 * a water, a tap beside every water clears the selection. Every finished camera move reports the visible area and
 * the zoom as the website counts it.
 */
@Composable
fun WaterShapesMap(
    geoJson: String,
    selectedId: String?,
    fitTo: MapArea?,
    onViewChange: (MapArea, Double) -> Unit,
    onSelect: (String?) -> Unit,
    modifier: Modifier = Modifier,
) {
    val touchSlop = with(LocalDensity.current) { 12f * density }
    val mapView = rememberMapView()
    var map by remember { mutableStateOf<MapLibreMap?>(null) }
    var style by remember { mutableStateOf<Style?>(null) }
    val currentOnViewChange by rememberUpdatedState(onViewChange)
    val currentOnSelect by rememberUpdatedState(onSelect)

    LaunchedEffect(mapView) {
        mapView.getMapAsync { loaded ->
            loaded.uiSettings.isRotateGesturesEnabled = false
            loaded.uiSettings.isTiltGesturesEnabled = false
            loaded.moveCamera(CameraUpdateFactory.newLatLngBounds(MapArea.START.toBounds(), 0))
            loaded.setStyle(Style.Builder().fromUri(STYLE_URL)) { ready ->
                ready.addSource(GeoJsonSource(SHAPES, """{"type":"FeatureCollection","features":[]}"""))
                ready.addLayer(FillLayer(FILLS, SHAPES).withFilter(isArea()).withProperties(fillOpacity(0.2f)))
                ready.addLayer(LineLayer(LINES, SHAPES))
                style = ready
            }
            loaded.addOnCameraIdleListener { currentOnViewChange(loaded.visibleArea(), loaded.webZoom) }
            loaded.addOnMapClickListener { point ->
                val screen = loaded.projection.toScreenLocation(point)
                val touched = RectF(screen.x - touchSlop, screen.y - touchSlop, screen.x + touchSlop, screen.y + touchSlop)
                val hit = loaded.queryRenderedFeatures(touched, LINES, FILLS).firstOrNull()?.getStringProperty("id")
                currentOnSelect(hit)
                hit != null
            }
            map = loaded
        }
    }

    LaunchedEffect(style, geoJson) {
        style?.getSourceAs<GeoJsonSource>(SHAPES)?.setGeoJson(geoJson)
    }
    LaunchedEffect(style, selectedId) {
        val ready = style ?: return@LaunchedEffect
        val selected: Expression = eq(get("id"), literal(selectedId.orEmpty()))
        val color = switchCase(selected, literal(PinColors.SELECTED), literal(PinColors.WATER))
        ready.getLayerAs<FillLayer>(FILLS)?.setProperties(fillColor(color))
        ready.getLayerAs<LineLayer>(LINES)?.setProperties(
            lineColor(color),
            lineWidth(switchCase(selected, literal(4f), literal(2.5f))),
        )
    }
    LaunchedEffect(map, fitTo) {
        val target = fitTo ?: return@LaunchedEffect
        map?.animateCamera(CameraUpdateFactory.newLatLngBounds(target.toBounds(), 48))
    }

    AndroidView(factory = { mapView }, modifier = modifier)
}

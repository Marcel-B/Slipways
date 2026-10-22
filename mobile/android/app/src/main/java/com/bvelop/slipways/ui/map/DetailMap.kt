package com.bvelop.slipways.ui.map

import android.provider.Settings
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.viewinterop.AndroidView
import com.bvelop.slipways.shared.MapArea
import com.bvelop.slipways.shared.Position
import org.maplibre.android.camera.CameraUpdateFactory
import org.maplibre.android.geometry.LatLng
import org.maplibre.android.maps.Style
import org.maplibre.android.style.expressions.Expression
import org.maplibre.android.style.expressions.Expression.any
import org.maplibre.android.style.expressions.Expression.eq
import org.maplibre.android.style.expressions.Expression.geometryType
import org.maplibre.android.style.expressions.Expression.get
import org.maplibre.android.style.expressions.Expression.literal
import org.maplibre.android.style.layers.CircleLayer
import org.maplibre.android.style.layers.FillLayer
import org.maplibre.android.style.layers.LineLayer
import org.maplibre.android.style.layers.PropertyFactory.circleColor
import org.maplibre.android.style.layers.PropertyFactory.circleRadius
import org.maplibre.android.style.layers.PropertyFactory.circleStrokeColor
import org.maplibre.android.style.layers.PropertyFactory.circleStrokeWidth
import org.maplibre.android.style.layers.PropertyFactory.fillColor
import org.maplibre.android.style.layers.PropertyFactory.fillOpacity
import org.maplibre.android.style.layers.PropertyFactory.lineColor
import org.maplibre.android.style.layers.PropertyFactory.lineWidth
import org.maplibre.android.style.sources.GeoJsonSource
import org.maplibre.geojson.Feature
import org.maplibre.geojson.FeatureCollection
import org.maplibre.geojson.Point

private const val SHAPE = "shape"
private const val SHAPE_FILL = "shape-fill"
private const val SHAPE_LINE = "shape-line"
private const val DETAIL_PINS = "detail-pins"
private const val DETAIL_PIN_LAYER = "detail-pin-layer"

/** Starts this many zoom levels further out and flies in, like the website's details pages. */
private const val INTRO_ZOOM_OUT = 3.0

/** A coloured dot on a details map, such as a slipway, a gauge or a marina. */
data class DetailPin(val position: Position, val color: String)

/**
 * Map on a details page showing [area] with [pins] and, for a water, its [shape] as GeoJSON geometry. It opens zoomed
 * out and flies in, unless the system turned animations off.
 */
@Composable
fun DetailMap(area: MapArea, pins: List<DetailPin>, modifier: Modifier = Modifier, shape: String? = null) {
    val context = LocalContext.current
    val mapView = rememberMapView()
    val animate = Settings.Global.getFloat(context.contentResolver, Settings.Global.ANIMATOR_DURATION_SCALE, 1f) > 0f

    LaunchedEffect(mapView) {
        mapView.getMapAsync { map ->
            map.uiSettings.isRotateGesturesEnabled = false
            map.uiSettings.isTiltGesturesEnabled = false
            map.moveCamera(CameraUpdateFactory.newLatLngZoom(LatLng(area.centerLatitude, area.centerLongitude), 4.0))
            map.setStyle(Style.Builder().fromUri(STYLE_URL)) { style ->
                // The view has its size by now, which fitting the bounds needs.
                map.moveCamera(CameraUpdateFactory.newLatLngBounds(area.toBounds(), 48))
                if (animate) {
                    val target = map.cameraPosition
                    map.moveCamera(CameraUpdateFactory.zoomTo(target.zoom - INTRO_ZOOM_OUT))
                    map.animateCamera(CameraUpdateFactory.newCameraPosition(target), 1000)
                }
                if (shape != null) {
                    style.addSource(GeoJsonSource(SHAPE, """{"type":"Feature","properties":{},"geometry":$shape}"""))
                    style.addLayer(
                        FillLayer(SHAPE_FILL, SHAPE).withFilter(isArea())
                            .withProperties(fillColor(PinColors.WATER), fillOpacity(0.2f)),
                    )
                    style.addLayer(LineLayer(SHAPE_LINE, SHAPE).withProperties(lineColor(PinColors.WATER), lineWidth(3f)))
                }
                style.addSource(
                    GeoJsonSource(
                        DETAIL_PINS,
                        FeatureCollection.fromFeatures(
                            pins.map { pin ->
                                Feature.fromGeometry(Point.fromLngLat(pin.position.longitude, pin.position.latitude)).apply {
                                    addStringProperty("color", pin.color)
                                }
                            },
                        ),
                    ),
                )
                style.addLayer(
                    CircleLayer(DETAIL_PIN_LAYER, DETAIL_PINS).withProperties(
                        circleColor(get("color")),
                        circleRadius(8f),
                        circleStrokeColor("#FFFFFF"),
                        circleStrokeWidth(2f),
                    ),
                )
            }
        }
    }

    AndroidView(factory = { mapView }, modifier = modifier)
}

/** Lakes and other areas get a fill; rivers and canals only the line. */
internal fun isArea(): Expression = any(eq(geometryType(), literal("Polygon")), eq(geometryType(), literal("MultiPolygon")))

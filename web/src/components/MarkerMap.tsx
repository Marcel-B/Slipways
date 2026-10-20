import type { GeoJSON as LeafletGeoJSON, Map as LeafletMap } from 'leaflet'
import 'leaflet/dist/leaflet.css'
import { useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react'
import { GeoJSON, MapContainer, Marker, Pane, Popup, TileLayer, useMap, useMapEvents } from 'react-leaflet'
import { Link } from 'react-router'
import Supercluster, { type PointFeature } from 'supercluster'
import type { Bounds } from '../api/types'
import { faintShapeStyle, shapeStyle, type MapMarker, type MapShape } from './mapMarkers'
import type { MapView } from './mapView'
import { clusterIcon, pinIcon } from './markerIcons'

const GERMANY: [number, number] = [51.2, 10.4]

interface MarkerMapProps {
  markers: MapMarker[]
  center?: [number, number]
  zoom?: number
  /** Fits the map to these bounds instead of using center and zoom. */
  bounds?: Bounds
  shapes?: MapShape[]
  /** Called with the id of the marker or shape whose popup opens, and with null when it closes. */
  onSelect?: (id: string | null) => void
  /** Groups nearby markers into counted clusters that open on click. */
  cluster?: boolean
  /** Draws this marker larger and on top, even when it would be in a cluster. */
  highlightedId?: string | null
  /** Called with the visible part of the map once it is measured and after every move or zoom. */
  onViewChange?: (view: MapView) => void
  /** Moves the map to these bounds whenever a new object is passed, e.g. to show search hits. */
  focus?: Bounds | null
  /** Starts a little further out and glides in to the initial view, as a gentle entrance on detail pages. */
  introZoom?: boolean
}

type SelectHandlers = (id: string) => Record<string, () => void> | undefined

export function MarkerMap({
  markers,
  center = GERMANY,
  zoom = 6,
  bounds,
  shapes = [],
  onSelect,
  cluster = false,
  highlightedId = null,
  onViewChange,
  focus = null,
  introZoom = false,
}: MarkerMapProps) {
  const selectHandlers: SelectHandlers = (id) =>
    onSelect && { popupopen: () => onSelect(id), popupclose: () => onSelect(null) }

  return (
    // MapContainer ignores bounds as soon as it gets a center and zoom.
    <MapContainer
      {...(bounds
        ? {
            bounds: [
              [bounds.south, bounds.west],
              [bounds.north, bounds.east],
            ],
            // A single place has empty bounds, which would otherwise zoom in all the way.
            boundsOptions: { padding: [24, 24], maxZoom: 13 },
          }
        : { center, zoom })}
      className="map"
      scrollWheelZoom={false}
    >
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        url="https://tile.openstreetmap.org/{z}/{x}/{y}.png"
      />
      {/* Below the markers (overlay pane: 400), so a river never covers them. */}
      <Pane name="shapes" style={{ zIndex: 350 }}>
        {shapes.map((shape) => (
          <ShapeLayer key={shape.id} shape={shape} eventHandlers={selectHandlers(shape.id)} />
        ))}
      </Pane>
      <AutoResize />
      {introZoom && <IntroZoom />}
      {onViewChange && <ViewReporter onViewChange={onViewChange} />}
      {focus && <Focus bounds={focus} />}
      <MarkerLayer markers={markers} cluster={cluster} highlightedId={highlightedId} selectHandlers={selectHandlers} />
    </MapContainer>
  )
}

/** Leaflet measures its container once; the map may start hidden (mobile list view) or change size. */
function AutoResize() {
  const map = useMap()
  useEffect(() => {
    const observer = new ResizeObserver(() => map.invalidateSize())
    observer.observe(map.getContainer())
    return () => observer.disconnect()
  }, [map])
  return null
}

const INTRO_ZOOM_OUT = 2

function IntroZoom() {
  const map = useMap()
  // Layout effect: the map moves out before the first paint, so the target view never flashes up.
  useLayoutEffect(() => {
    if (window.matchMedia?.('(prefers-reduced-motion: reduce)').matches) return
    const center = map.getCenter()
    const zoom = map.getZoom()
    map.setView(center, zoom - INTRO_ZOOM_OUT, { animate: false })
    const frame = requestAnimationFrame(() => map.flyTo(center, zoom, { duration: 1 }))
    return () => {
      cancelAnimationFrame(frame)
      map.stop()
    }
  }, [map])
  return null
}

function ViewReporter({ onViewChange }: { onViewChange: (view: MapView) => void }) {
  const map = useMap()
  // A hidden map (mobile list view) has no size and so no meaningful view; keep the last one.
  useMapEvents({ moveend: () => map.getSize().x > 0 && onViewChange(viewOf(map)) })
  useEffect(() => {
    if (map.getSize().x > 0) onViewChange(viewOf(map))
  }, [map, onViewChange])
  return null
}

function Focus({ bounds }: { bounds: Bounds }) {
  const map = useMap()
  useEffect(() => {
    map.flyToBounds(
      [
        [bounds.south, bounds.west],
        [bounds.north, bounds.east],
      ],
      { padding: [24, 24], maxZoom: 13, duration: 0.8 },
    )
  }, [map, bounds])
  return null
}

interface MarkerLayerProps {
  markers: MapMarker[]
  cluster: boolean
  highlightedId: string | null
  selectHandlers: SelectHandlers
}

type MarkerFeature = PointFeature<MapMarker>

function MarkerLayer({ markers, cluster, highlightedId, selectHandlers }: MarkerLayerProps) {
  const map = useMap()
  const [view, setView] = useState(() => viewOf(map))
  useMapEvents({ moveend: () => setView(viewOf(map)) })

  const index = useMemo(() => {
    if (!cluster) return null
    const points = markers.map(
      (m): MarkerFeature => ({ type: 'Feature', geometry: { type: 'Point', coordinates: [m.longitude, m.latitude] }, properties: m }),
    )
    // Up to zoom 12 places a few hundred metres apart still merge; beyond that each slipway stands alone.
    return new Supercluster<MapMarker>({ radius: 48, maxZoom: 12 }).load(points)
  }, [markers, cluster])

  const highlighted = markers.find((m) => m.id === highlightedId)
  const pin = (marker: MapMarker, isHighlighted = false) => (
    <Marker
      key={`${marker.kind}-${marker.id}`}
      position={[marker.latitude, marker.longitude]}
      icon={pinIcon(marker.kind, isHighlighted)}
      zIndexOffset={isHighlighted ? 1000 : 0}
      eventHandlers={selectHandlers(marker.id)}
    >
      <Popup>
        <PopupContent {...marker} />
      </Popup>
    </Marker>
  )

  if (!index) {
    return markers.map((m) => pin(m, m === highlighted))
  }

  return (
    <>
      {index.getClusters(view.bbox, view.zoom).map((feature) => {
        const [longitude, latitude] = feature.geometry.coordinates
        if ('cluster' in feature.properties && feature.properties.cluster) {
          const { cluster_id: id, point_count: count } = feature.properties
          return (
            <Marker
              key={`cluster-${id}`}
              position={[latitude, longitude]}
              icon={clusterIcon(count)}
              title={`${count} Orte`}
              eventHandlers={{ click: () => map.flyTo([latitude, longitude], index.getClusterExpansionZoom(id)) }}
            />
          )
        }
        const marker = feature.properties as MapMarker
        return marker === highlighted ? null : pin(marker)
      })}
      {highlighted && pin(highlighted, true)}
    </>
  )
}

function viewOf(map: LeafletMap): MapView {
  const b = map.getBounds()
  return { bbox: [b.getWest(), b.getSouth(), b.getEast(), b.getNorth()], zoom: Math.round(map.getZoom()) }
}

function ShapeLayer({ shape, eventHandlers }: { shape: MapShape; eventHandlers?: Record<string, () => void> }) {
  const layer = useRef<LeafletGeoJSON>(null)
  useEffect(() => {
    if (shape.highlighted) layer.current?.bringToFront()
  }, [shape.highlighted])

  // Changing the style restyles the layer in place, so an open popup stays open.
  return (
    <GeoJSON
      ref={layer}
      data={shape.geometry}
      style={shape.highlighted ? shapeStyle : faintShapeStyle}
      interactive={shape.popup !== undefined}
      eventHandlers={eventHandlers}
    >
      {shape.popup && (
        <Popup>
          <PopupContent {...shape.popup} />
        </Popup>
      )}
    </GeoJSON>
  )
}

function PopupContent({ title, subtitle, to }: Pick<MapMarker, 'title' | 'subtitle' | 'to'>) {
  return (
    <>
      {to ? <Link to={to}>{title}</Link> : <strong>{title}</strong>}
      {subtitle && (
        <>
          <br />
          {subtitle}
        </>
      )}
    </>
  )
}

import type { GeoJSON as LeafletGeoJSON } from 'leaflet'
import 'leaflet/dist/leaflet.css'
import { useEffect, useRef } from 'react'
import { CircleMarker, GeoJSON, MapContainer, Pane, Popup, TileLayer } from 'react-leaflet'
import { Link } from 'react-router'
import type { Bounds } from '../api/types'
import { faintShapeStyle, markerColors, shapeStyle, type MapMarker, type MapShape } from './mapMarkers'

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
}

export function MarkerMap({ markers, center = GERMANY, zoom = 6, bounds, shapes = [], onSelect }: MarkerMapProps) {
  const selectHandlers = (id: string) =>
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
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />
      {/* Below the markers (overlay pane: 400), so a river never covers them. */}
      <Pane name="shapes" style={{ zIndex: 350 }}>
        {shapes.map((shape) => (
          <ShapeLayer key={shape.id} shape={shape} eventHandlers={selectHandlers(shape.id)} />
        ))}
      </Pane>
      {markers.map((marker) => (
        <CircleMarker
          key={`${marker.kind}-${marker.id}`}
          center={[marker.latitude, marker.longitude]}
          radius={7}
          pathOptions={{ ...markerColors[marker.kind], fillOpacity: 0.8 }}
          eventHandlers={selectHandlers(marker.id)}
        >
          <Popup>
            <PopupContent {...marker} />
          </Popup>
        </CircleMarker>
      ))}
    </MapContainer>
  )
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

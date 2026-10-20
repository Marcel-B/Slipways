import type { Geometry } from 'geojson'
import 'leaflet/dist/leaflet.css'
import { CircleMarker, GeoJSON, MapContainer, Pane, Popup, TileLayer } from 'react-leaflet'
import { Link } from 'react-router'
import type { Bounds } from '../api/types'
import { markerColors, shapeStyle, type MapMarker } from './mapMarkers'

const GERMANY: [number, number] = [51.2, 10.4]

interface MarkerMapProps {
  markers: MapMarker[]
  center?: [number, number]
  zoom?: number
  /** Fits the map to these bounds instead of using center and zoom. */
  bounds?: Bounds
  /** Highlighted shape; the id tells shapes apart, since Leaflet does not update a shape in place. */
  shape?: { id: string; geometry: Geometry }
  /** Called with the marker whose popup opens, and with null when it closes. */
  onSelect?: (marker: MapMarker | null) => void
}

export function MarkerMap({ markers, center = GERMANY, zoom = 6, bounds, shape, onSelect }: MarkerMapProps) {
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
        {shape && <GeoJSON key={shape.id} data={shape.geometry} style={shapeStyle} interactive={false} />}
      </Pane>
      {markers.map((marker) => (
        <CircleMarker
          key={`${marker.kind}-${marker.id}`}
          center={[marker.latitude, marker.longitude]}
          radius={7}
          pathOptions={{ ...markerColors[marker.kind], fillOpacity: 0.8 }}
          eventHandlers={onSelect && { popupopen: () => onSelect(marker), popupclose: () => onSelect(null) }}
        >
          <Popup>
            {marker.to ? <Link to={marker.to}>{marker.title}</Link> : <strong>{marker.title}</strong>}
            {marker.subtitle && (
              <>
                <br />
                {marker.subtitle}
              </>
            )}
          </Popup>
        </CircleMarker>
      ))}
    </MapContainer>
  )
}

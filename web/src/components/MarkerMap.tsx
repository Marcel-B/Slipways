import 'leaflet/dist/leaflet.css'
import { CircleMarker, MapContainer, Popup, TileLayer } from 'react-leaflet'
import { Link } from 'react-router'
import type { Bounds } from '../api/types'
import { markerColors, type MapMarker } from './mapMarkers'

const GERMANY: [number, number] = [51.2, 10.4]

interface MarkerMapProps {
  markers: MapMarker[]
  center?: [number, number]
  zoom?: number
  /** Fits the map to these bounds instead of using center and zoom. */
  bounds?: Bounds
}

export function MarkerMap({ markers, center = GERMANY, zoom = 6, bounds }: MarkerMapProps) {
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
      {markers.map((marker) => (
        <CircleMarker
          key={`${marker.kind}-${marker.id}`}
          center={[marker.latitude, marker.longitude]}
          radius={7}
          pathOptions={{ ...markerColors[marker.kind], fillOpacity: 0.8 }}
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

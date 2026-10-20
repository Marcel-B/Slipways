import 'leaflet/dist/leaflet.css'
import { CircleMarker, MapContainer, Popup, TileLayer } from 'react-leaflet'
import { Link } from 'react-router'
import type { SlipwaySummary } from '../api/types'

const GERMANY: [number, number] = [51.2, 10.4]

interface SlipwayMapProps {
  slipways: SlipwaySummary[]
  center?: [number, number]
  zoom?: number
}

export function SlipwayMap({ slipways, center = GERMANY, zoom = 6 }: SlipwayMapProps) {
  return (
    <MapContainer center={center} zoom={zoom} className="map" scrollWheelZoom={false}>
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />
      {slipways.map((slipway) => (
        <CircleMarker
          key={slipway.id}
          center={[slipway.latitude, slipway.longitude]}
          radius={7}
          pathOptions={{ color: '#0b5394', fillColor: '#3d85c6', fillOpacity: 0.8 }}
        >
          <Popup>
            <Link to={`/slipways/${slipway.id}`}>{slipway.name}</Link>
            <br />
            {slipway.water.name}
          </Popup>
        </CircleMarker>
      ))}
    </MapContainer>
  )
}

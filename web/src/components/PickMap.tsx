import type { Marker as LeafletMarker } from 'leaflet'
import 'leaflet/dist/leaflet.css'
import { useMemo } from 'react'
import { MapContainer, Marker, TileLayer, useMapEvents } from 'react-leaflet'
import type { Position } from '../api/types'
import { pinIcon } from './markerIcons'

interface PickMapProps {
  position: Position
  zoom: number
  onChange: (position: Position) => void
}

/** A map with one pin to drag, or to move by tapping the map, for placing something new. */
export function PickMap({ position, zoom, onChange }: PickMapProps) {
  const handlers = useMemo(
    () => ({
      dragend: (event: { target: LeafletMarker }) => {
        const { lat, lng } = event.target.getLatLng()
        onChange({ latitude: lat, longitude: lng })
      },
    }),
    [onChange],
  )

  return (
    <MapContainer center={[position.latitude, position.longitude]} zoom={zoom} className="map" scrollWheelZoom={false}>
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        url="https://tile.openstreetmap.org/{z}/{x}/{y}.png"
      />
      <Marker
        position={[position.latitude, position.longitude]}
        icon={pinIcon('slipway', true)}
        draggable
        eventHandlers={handlers}
        keyboard
        title="Position der Slipanlage, zum Verschieben ziehen"
      />
      <TapToMove onChange={onChange} />
    </MapContainer>
  )
}

function TapToMove({ onChange }: { onChange: (position: Position) => void }) {
  useMapEvents({ click: (event) => onChange({ latitude: event.latlng.lat, longitude: event.latlng.lng }) })
  return null
}

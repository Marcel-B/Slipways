import type { Geometry } from 'geojson'
import type { SlipwaySummary } from '../api/types'

// Kept apart from MarkerMap so pages can use it without pulling Leaflet into the main chunk.

export type MarkerKind = 'slipway' | 'station' | 'marina' | 'water'

export const markerColors: Record<MarkerKind, { color: string; fillColor: string }> = {
  slipway: { color: '#0b5394', fillColor: '#3d85c6' },
  station: { color: '#38761d', fillColor: '#6aa84f' },
  marina: { color: '#741b47', fillColor: '#a64d79' },
  water: { color: '#0b5394', fillColor: '#3d85c6' },
}

/** Highlights a water's course or area; orange stands out against the blue water of the map tiles. */
export const shapeStyle = { color: '#e69138', weight: 5, opacity: 0.85, fillOpacity: 0.3 }

/** For many shapes at once: dark blue reads as water and, unlike orange, does not blend in with the roads. */
export const faintShapeStyle = { color: '#1c4587', weight: 3, opacity: 0.7, fillOpacity: 0.2 }

export interface MapMarker {
  id: string
  kind: MarkerKind
  latitude: number
  longitude: number
  title: string
  subtitle?: string
  /** Route the title links to. */
  to?: string
}

export interface MapShape {
  id: string
  geometry: Geometry
  /** Drawn bold and on top of the others. */
  highlighted?: boolean
  /** Makes the shape clickable, with the same popup as a marker. */
  popup?: Pick<MapMarker, 'title' | 'subtitle' | 'to'>
}

export function slipwayMarker(slipway: SlipwaySummary): MapMarker {
  return {
    id: slipway.id,
    kind: 'slipway',
    latitude: slipway.latitude,
    longitude: slipway.longitude,
    title: slipway.name,
    subtitle: slipway.water.name,
    to: `/slipways/${slipway.id}`,
  }
}

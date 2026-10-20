import type { SlipwaySummary } from '../api/types'

// Kept apart from MarkerMap so pages can use it without pulling Leaflet into the main chunk.

export type MarkerKind = 'slipway' | 'station' | 'marina' | 'water'

export const markerColors: Record<MarkerKind, { color: string; fillColor: string }> = {
  slipway: { color: '#0b5394', fillColor: '#3d85c6' },
  station: { color: '#38761d', fillColor: '#6aa84f' },
  marina: { color: '#741b47', fillColor: '#a64d79' },
  water: { color: '#0b5394', fillColor: '#3d85c6' },
}

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

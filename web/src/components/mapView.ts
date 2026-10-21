import type { Bounds, Position } from '../api/types'

/** The part of the map on screen: west, south, east, north, and the zoom level. */
export interface MapView {
  bbox: [number, number, number, number]
  zoom: number
}

/** Below this zoom the map shows a whole region or more, and a list of everything on it would help no one. */
export const LIST_MIN_ZOOM = 8

/** Up to this zoom a clustered map shows one bubble per country instead of clusters. */
export const COUNTRY_MAX_ZOOM = 5

/** The cluster radius in pixels: wide while a country or more is on screen, finer once a region is. */
export function clusterRadius(zoom: number): number {
  return zoom < 6 ? 130 : zoom < 8 ? 100 : zoom < 10 ? 72 : 52
}

/**
 * What the list next to the map shows: every search hit, or without a search the places in the map view.
 * Null when the map is zoomed out too far (or not measured yet) and the list should ask to zoom in or search.
 */
export function listedInView<T extends Position>(items: T[], view: MapView | null, searching: boolean): T[] | null {
  if (searching) return items
  if (!view || view.zoom < LIST_MIN_ZOOM) return null
  const [west, south, east, north] = view.bbox
  return items.filter((p) => p.longitude >= west && p.longitude <= east && p.latitude >= south && p.latitude <= north)
}

/** The smallest bounds around the given places, or null for none. */
export function boundsOf(places: Position[]): Bounds | null {
  if (places.length === 0) return null
  const lats = places.map((p) => p.latitude)
  const lons = places.map((p) => p.longitude)
  return { south: Math.min(...lats), west: Math.min(...lons), north: Math.max(...lats), east: Math.max(...lons) }
}

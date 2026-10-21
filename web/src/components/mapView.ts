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

/** Below this zoom a water has to be this many degrees wide or high to be drawn: smaller ones would be a dot. */
export const SHAPE_ALL_ZOOM = 12

/**
 * Which water shapes to load for a map view: those reaching into it (widened to a grid, so small moves reuse what was
 * loaded) and, further out, only those large enough to see, about 20 pixels.
 */
export function shapeArea(view: MapView): { bbox: string; minExtent: number } {
  // About one tile of 256 pixels at this zoom.
  const step = 360 / 2 ** Math.max(0, Math.floor(view.zoom))
  const [west, south, east, north] = view.bbox
  const snap = (value: number, round: (v: number) => number, min: number, max: number) =>
    Math.min(max, Math.max(min, round(value / step) * step))
  const bbox = [
    snap(west - step, Math.floor, -180, 180),
    snap(south - step, Math.floor, -90, 90),
    snap(east + step, Math.ceil, -180, 180),
    snap(north + step, Math.ceil, -90, 90),
  ]
    .map((v) => +v.toFixed(4))
    .join(',')
  const minExtent = view.zoom >= SHAPE_ALL_ZOOM ? 0 : +(28 / 2 ** view.zoom).toFixed(4)
  return { bbox, minExtent }
}

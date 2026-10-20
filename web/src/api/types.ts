import type { Geometry } from 'geojson'

export interface Water {
  id: string
  name: string
}

export interface SlipwaySummary {
  id: string
  name: string
  latitude: number
  longitude: number
  postalCode: string | null
  city: string | null
  country: string | null
  water: Water
}

export interface SlipwayDetails extends SlipwaySummary {
  street: string | null
  comment: string | null
  rating: number
  costs: number | null
  pro: string | null
  contra: string | null
  marina: { id: string; name: string; url: string | null } | null
  extras: string[]
  created: string
  updated: string | null
}

export interface Position {
  latitude: number
  longitude: number
}

export interface Bounds {
  south: number
  west: number
  north: number
  east: number
}

/** Latest reading at a gauge; `value` is in centimetres. */
export interface WaterLevel {
  value: number
  measuredAt: string
  state: string | null
}

export interface WaterSummary extends Water {
  shortname: string | null
  slipwayCount: number
  stationCount: number
  /** Something on the water near its middle; null when nothing on it has coordinates. */
  position: Position | null
  /** Set only when the water has exactly one gauge with a reading. */
  gauge: { stationId: string; stationName: string; waterLevel: WaterLevel } | null
}

export interface Station {
  id: string
  name: string
  number: string | null
  latitude: number
  longitude: number
  km: number
  agency: string | null
  water: Water
  waterLevel: WaterLevel | null
}

export interface Marina {
  id: string
  name: string
  latitude: number
  longitude: number
  url: string | null
  street: string | null
  postalCode: string | null
  city: string | null
  phone: string | null
  email: string | null
  water: Water
}

export interface WaterDetails extends Water {
  shortname: string | null
  /** Covers the shape and everything on the water. */
  bounds: Bounds | null
  /** Simplified course or area from OpenStreetMap; null when the water has none. */
  shape: Geometry | null
  /** OpenStreetMap elements of the shape, such as "relation/123924". */
  osmElements: string[]
  slipways: SlipwaySummary[]
  stations: Station[]
  marinas: Marina[]
}

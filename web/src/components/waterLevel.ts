import type { WaterLevel } from '../api/types'

const centimetres = new Intl.NumberFormat('de-DE', { maximumFractionDigits: 1 })
const timestamp = new Intl.DateTimeFormat('de-DE', { dateStyle: 'short', timeStyle: 'short' })

// PEGELONLINE compares the level with mean low and mean high water (stateMnwMhw).
const states: Record<string, string> = {
  low: 'niedrig',
  normal: 'normal',
  high: 'hoch',
  unknown: 'unbekannt',
  commented: 'kommentiert',
  'out-dated': 'veraltet',
}

export function formatWaterLevel(level: WaterLevel): string {
  const state = level.state && states[level.state]
  return [`${centimetres.format(level.value)} cm`, state].filter(Boolean).join(' · ')
}

export function formatMeasuredAt(level: WaterLevel): string {
  return timestamp.format(new Date(level.measuredAt))
}

import type { Bounds, Position } from '../api/types'

const regionNames = new Intl.DisplayNames('de', { type: 'region' })

/** The German name of a country, e.g. "Niederlande" for NL. */
export function countryName(code: string): string {
  return regionNames.of(code) ?? code
}

/** The flag emoji of a country, built from its ISO code ("DE" → 🇩🇪); null for anything that is not two letters. */
export function countryFlag(code: string): string | null {
  if (!/^[A-Za-z]{2}$/.test(code)) return null
  return String.fromCodePoint(...[...code.toUpperCase()].map((letter) => 0x1f1e6 + letter.charCodeAt(0) - 65))
}

let flagSupport: boolean | undefined

/**
 * Whether this device draws flag emojis in colour. Windows has none and shows two plain letters instead,
 * so the bubbles keep the ISO code there.
 */
export function supportsFlags(): boolean {
  if (flagSupport !== undefined) return flagSupport
  flagSupport = false
  try {
    const context = document.createElement('canvas').getContext('2d', { willReadFrequently: true })
    if (context) {
      context.canvas.width = context.canvas.height = 20
      context.textBaseline = 'top'
      context.font = '16px sans-serif'
      context.fillText(countryFlag('DE')!, 0, 0)
      const pixels = context.getImageData(0, 0, 20, 20).data
      for (let i = 0; i < pixels.length && !flagSupport; i += 4) {
        // A flag has colour; plain letters are grey.
        flagSupport = pixels[i + 3] > 0 && (pixels[i] !== pixels[i + 1] || pixels[i + 1] !== pixels[i + 2])
      }
    }
  } catch {
    // No canvas (tests, very old browsers): keep the code.
  }
  return flagSupport
}

export interface CountryGroup<T> {
  code: string
  items: T[]
  /** Where the country's bubble stands: the median, which stays on land for long coasts where the mean drifts out to sea. */
  center: Position
  /** Where most of its places lie; a few far-off ones (islands, Svalbard) do not count. */
  bounds: Bounds
}

/** Groups places by their ISO country code, largest first; places without one are left out. */
export function byCountry<T extends Position & { country?: string | null }>(items: T[]): CountryGroup<T>[] {
  const groups = new Map<string, T[]>()
  for (const item of items) {
    if (!item.country) continue
    const group = groups.get(item.country)
    if (group) group.push(item)
    else groups.set(item.country, [item])
  }
  return [...groups]
    .map(([code, members]) => ({ code, items: members, center: medianOf(members), bounds: coreBounds(members)! }))
    .sort((a, b) => b.items.length - a.items.length || a.code.localeCompare(b.code))
}

function medianOf(places: Position[]): Position {
  const middle = (values: number[]) => values.sort((a, b) => a - b)[Math.floor(values.length / 2)]
  return { latitude: middle(places.map((p) => p.latitude)), longitude: middle(places.map((p) => p.longitude)) }
}

/** Like boundsOf, but from the 2nd to the 98th percentile, so a single far-off place does not zoom out to half of Europe. */
export function coreBounds(places: Position[]): Bounds | null {
  if (places.length === 0) return null
  const spread = (values: number[]) => {
    values.sort((a, b) => a - b)
    return [values[Math.floor(values.length * 0.02)], values[Math.ceil(values.length * 0.98) - 1]]
  }
  const [south, north] = spread(places.map((p) => p.latitude))
  const [west, east] = spread(places.map((p) => p.longitude))
  return { south, west, north, east }
}

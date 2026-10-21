import type { Bounds, Position } from '../api/types'

const regionNames = new Intl.DisplayNames('de', { type: 'region' })

/** The German name of a country, e.g. "Niederlande" for NL. */
export function countryName(code: string): string {
  return regionNames.of(code) ?? code
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

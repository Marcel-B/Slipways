import { describe, expect, it } from 'vitest'
import { byCountry, coreBounds, countryName } from './countries'

const place = (latitude: number, longitude: number, country: string | null) => ({ latitude, longitude, country })

describe('byCountry', () => {
  it('groups by country, largest first, and leaves out places without one', () => {
    const groups = byCountry([place(48, 11, 'DE'), place(52, 5, 'NL'), place(53, 10, 'DE'), place(50, 8, null)])
    expect(groups.map((g) => [g.code, g.items.length])).toEqual([
      ['DE', 2],
      ['NL', 1],
    ])
  })

  it('puts the bubble at the median, so a far-off place does not pull it away', () => {
    const [norway] = byCountry([place(59, 10, 'NO'), place(60, 5, 'NO'), place(61, 6, 'NO'), place(78, 15, 'NO')])
    expect(norway.center).toEqual({ latitude: 61, longitude: 10 })
  })
})

describe('coreBounds', () => {
  it('leaves out the outermost 2 percent', () => {
    const mainland = Array.from({ length: 99 }, (_, i) => place(58 + i / 33, 5 + i / 20, 'NO'))
    const bounds = coreBounds([...mainland, place(78, 15, 'NO')])!
    expect(bounds.north).toBeLessThan(61)
    expect(bounds.east).toBeLessThan(10)
    expect(coreBounds([])).toBeNull()
  })
})

describe('countryName', () => {
  it('names countries in German', () => {
    expect(countryName('NL')).toBe('Niederlande')
  })
})

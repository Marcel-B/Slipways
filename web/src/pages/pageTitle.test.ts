import { describe, expect, it } from 'vitest'
import { placeTitle } from './pageTitle'

describe('placeTitle', () => {
  it('adds the town to names without it', () => {
    expect(placeTitle('Slipanlage', 'Kiel')).toBe('Slipanlage in Kiel')
  })

  it('keeps names that already contain the town', () => {
    expect(placeTitle('Am Jachthafen, Kelheim', 'Kelheim')).toBe('Am Jachthafen, Kelheim')
    expect(placeTitle('Marina', null)).toBe('Marina')
  })
})

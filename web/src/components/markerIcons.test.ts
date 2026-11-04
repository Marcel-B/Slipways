import { describe, expect, it } from 'vitest'
import { countryIcon } from './markerIcons'

const html = (flags: boolean) => String(countryIcon('FR', 1234, flags).options.html)

describe('countryIcon', () => {
  it('shows the flag and the count', () => {
    expect(html(true)).toContain('🇫🇷')
    expect(html(true)).toContain('1.234')
    expect(html(true)).not.toContain('>FR<')
  })

  it('keeps the ISO code where the device draws no flags', () => {
    expect(html(false)).toContain('>FR<')
    expect(html(false)).not.toContain('🇫🇷')
  })
})

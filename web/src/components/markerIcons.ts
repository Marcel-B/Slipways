import { divIcon, type DivIcon } from 'leaflet'
import { countryFlag, supportsFlags } from './countries'
import { markerColors, type MarkerKind } from './mapMarkers'

// Leaflet wants the icons as HTML strings, so the Lucide glyphs (ISC) are inlined here.
const glyphs: Record<MarkerKind, string> = {
  slipway:
    '<path d="M10 2v15"/><path d="M7 22a4 4 0 0 1-4-4 1 1 0 0 1 1-1h16a1 1 0 0 1 1 1 4 4 0 0 1-4 4z"/><path d="M9.159 2.46a1 1 0 0 1 1.521-.193l9.977 8.98A1 1 0 0 1 20 13H4a1 1 0 0 1-.824-1.567z"/>',
  station:
    '<path d="M21.3 15.3a2.4 2.4 0 0 1 0 3.4l-2.6 2.6a2.4 2.4 0 0 1-3.4 0L2.7 8.7a2.41 2.41 0 0 1 0-3.4l2.6-2.6a2.41 2.41 0 0 1 3.4 0Z"/><path d="m14.5 12.5 2-2"/><path d="m11.5 9.5 2-2"/><path d="m8.5 6.5 2-2"/><path d="m17.5 15.5 2-2"/>',
  marina: '<path d="M12 6v16"/><path d="m19 13 2-1a9 9 0 0 1-18 0l2 1"/><path d="M9 11h6"/><circle cx="12" cy="4" r="2"/>',
  water:
    '<path d="M2 6c.6.5 1.2 1 2.5 1C7 7 7 5 9.5 5c2.6 0 2.4 2 5 2 2.5 0 2.5-2 5-2 1.3 0 1.9.5 2.5 1"/><path d="M2 12c.6.5 1.2 1 2.5 1 2.5 0 2.5-2 5-2 2.6 0 2.4 2 5 2 2.5 0 2.5-2 5-2 1.3 0 1.9.5 2.5 1"/><path d="M2 18c.6.5 1.2 1 2.5 1 2.5 0 2.5-2 5-2 2.6 0 2.4 2 5 2 2.5 0 2.5-2 5-2 1.3 0 1.9.5 2.5 1"/>',
}

const pins = new Map<string, DivIcon>()

/** A pin in the colour of the marker kind, with its symbol; the highlighted one is larger. */
export function pinIcon(kind: MarkerKind, highlighted = false): DivIcon {
  const key = `${kind}-${highlighted}`
  let icon = pins.get(key)
  if (!icon) {
    const size = highlighted ? 40 : 32
    icon = divIcon({
      className: 'map-pin-anchor',
      html:
        `<span class="map-pin${highlighted ? ' map-pin-highlighted' : ''}" style="--pin:${markerColors[kind].color};--pin-size:${size}px">` +
        `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">${glyphs[kind]}</svg>` +
        '</span>',
      iconSize: [size, size],
      // The pin is a square turned by 45°, so its tip sits half a diagonal below the centre.
      iconAnchor: [size / 2, size / 2 + size * Math.SQRT1_2],
      popupAnchor: [0, -size * Math.SQRT1_2],
    })
    pins.set(key, icon)
  }
  return icon
}

const compact = new Intl.NumberFormat('de-DE', { notation: 'compact', maximumFractionDigits: 1 })

/** A counted bubble that grows with the logarithm of its count, so 3,000 and 30 differ at a glance. */
export function clusterIcon(count: number): DivIcon {
  const size = Math.round(Math.min(64, 28 + Math.log10(count) * 11))
  const label = count >= 1000 ? compact.format(count) : String(count)
  return divIcon({
    className: 'map-pin-anchor',
    html: `<span class="map-cluster" style="--cluster-size:${size}px">${label}</span>`,
    iconSize: [size, size],
  })
}

/**
 * A bubble for a whole country, with its flag and the number of places in it. Where the device has no flag
 * emojis (Windows), the ISO code stands in for the flag.
 */
export function countryIcon(code: string, count: number, flags = supportsFlags()): DivIcon {
  const size = Math.round(Math.min(66, 34 + Math.log10(count) * 9))
  const flag = flags ? countryFlag(code) : null
  const top = flag
    ? `<span class="map-country-flag" aria-hidden="true">${flag}</span>`
    : `<span class="map-country-code">${code}</span>`
  return divIcon({
    className: 'map-pin-anchor',
    html:
      `<span class="map-country" style="--cluster-size:${size}px">` +
      `${top}<span>${count.toLocaleString('de-DE')}</span></span>`,
    iconSize: [size, size],
  })
}

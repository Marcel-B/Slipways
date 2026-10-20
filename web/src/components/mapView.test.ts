import { describe, expect, it } from 'vitest'
import { boundsOf, listedInView, type MapView } from './mapView'

const kelheim = { latitude: 48.93, longitude: 11.99 }
const hamburg = { latitude: 53.55, longitude: 9.99 }

describe('listedInView', () => {
  const aroundKelheim: MapView = { bbox: [11.5, 48.7, 12.5, 49.1], zoom: 10 }

  it('lists nothing while the map is zoomed out or not measured yet', () => {
    expect(listedInView([kelheim], null, false)).toBeNull()
    expect(listedInView([kelheim], { bbox: [5, 47, 15, 55], zoom: 6 }, false)).toBeNull()
  })

  it('lists the places in the map view', () => {
    expect(listedInView([kelheim, hamburg], aroundKelheim, false)).toEqual([kelheim])
  })

  it('lists every search hit, wherever the map is', () => {
    expect(listedInView([kelheim, hamburg], aroundKelheim, true)).toEqual([kelheim, hamburg])
  })
})

describe('boundsOf', () => {
  it('spans all places', () => {
    expect(boundsOf([kelheim, hamburg])).toEqual({ south: 48.93, west: 9.99, north: 53.55, east: 11.99 })
    expect(boundsOf([])).toBeNull()
  })
})

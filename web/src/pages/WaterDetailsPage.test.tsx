import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { WaterDetails } from '../api/types'
import type { MarkerMap } from '../components/MarkerMap'
import { WaterDetailsPage } from './WaterDetailsPage'

const mapProps = vi.hoisted(() => [] as Parameters<typeof MarkerMap>[0][])
vi.mock('../components/LazyMarkerMap', () => ({
  LazyMarkerMap: (props: Parameters<typeof MarkerMap>[0]) => {
    mapProps.push(props)
    return null
  },
}))

const donau = { id: 'd19708ca-ac42-4ad4-8d62-e85722f946f6', name: 'Donau' }

const water: WaterDetails = {
  ...donau,
  shortname: 'DONAU',
  bounds: { south: 47.9, west: 8.2, north: 49.0, east: 13.5 },
  shape: {
    type: 'LineString',
    coordinates: [
      [8.2, 47.9],
      [11.9, 48.9],
      [13.5, 48.6],
    ],
  },
  osmElements: ['relation/89652'],
  slipways: [
    {
      id: 'eac96a93-fd47-4a9a-8771-0005805b9655',
      name: 'Am Jachthafen, Kelheim',
      latitude: 48.93,
      longitude: 11.99,
      postalCode: '93309',
      city: 'Kelheim',
      country: 'DE',
      water: donau,
    },
  ],
  stations: [
    {
      id: '1b0e6a42-55c4-4a63-9f1a-3a8e1c2d4f50',
      name: 'KELHEIM',
      number: '10052006',
      latitude: 48.91,
      longitude: 11.88,
      km: 2414.82,
      agency: 'REGENSBURG',
      water: donau,
      waterLevel: { value: 312, measuredAt: '2026-10-20T09:45:00Z', state: 'normal' },
    },
    {
      id: '2c1f7b53-66d5-4b74-8a2b-4b9f2d3e5a61',
      name: 'PFELLING',
      number: '10088003',
      latitude: 48.88,
      longitude: 12.75,
      km: 2305.53,
      agency: 'REGENSBURG',
      water: donau,
      waterLevel: null,
    },
  ],
  marinas: [],
}

function renderAt(url: string) {
  const router = createMemoryRouter([{ path: '/waters/:id', element: <WaterDetailsPage /> }], {
    initialEntries: [url],
  })
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  )
}

describe('WaterDetailsPage', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    mapProps.length = 0
  })

  it('shows slipways and gauges and fits the map to the water', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify(water), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    renderAt(`/waters/${water.id}`)

    expect(await screen.findByRole('heading', { name: 'Donau' })).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith(`/api/waters/${water.id}`, expect.anything())
    expect(screen.getByRole('link', { name: /Am Jachthafen, Kelheim/ })).toHaveAttribute(
      'href',
      '/slipways/eac96a93-fd47-4a9a-8771-0005805b9655',
    )

    const rows = screen.getAllByRole('row')
    expect(within(rows[1]).getByText('KELHEIM')).toBeInTheDocument()
    expect(within(rows[1]).getByText('312 cm · normal')).toBeInTheDocument()
    expect(within(rows[2]).getAllByText('–')).toHaveLength(2)

    const map = mapProps.at(-1)
    expect(map?.bounds).toEqual(water.bounds)
    expect(map?.markers.map((m) => m.kind)).toEqual(['slipway', 'station', 'station'])
  })

  it('highlights the shape of the water and links to OpenStreetMap', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(water), { status: 200 })))

    renderAt(`/waters/${water.id}`)

    expect(await screen.findByRole('link', { name: 'OpenStreetMap' })).toHaveAttribute(
      'href',
      'https://www.openstreetmap.org/relation/89652',
    )
    expect(mapProps.at(-1)?.shapes).toEqual([{ id: water.id, geometry: water.shape, highlighted: true }])
  })

  it('shows no course without a shape', async () => {
    const noShape: WaterDetails = { ...water, shape: null, osmElements: [] }
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(noShape), { status: 200 })))

    renderAt(`/waters/${water.id}`)

    expect(await screen.findByRole('heading', { name: 'Donau' })).toBeInTheDocument()
    expect(screen.queryByText(/Verlauf/)).not.toBeInTheDocument()
    expect(mapProps.at(-1)?.shapes).toEqual([])
  })

  it('has no map when nothing on the water has coordinates', async () => {
    const empty: WaterDetails = { ...water, bounds: null, slipways: [], stations: [] }
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(empty), { status: 200 })))

    renderAt(`/waters/${water.id}`)

    expect(await screen.findByText(/keine Orte bekannt/)).toBeInTheDocument()
    expect(mapProps).toHaveLength(0)
  })

  it('reports an unknown water', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('', { status: 404, statusText: 'Not Found' })))

    renderAt(`/waters/${water.id}`)

    expect(await screen.findByText('Dieses Gewässer gibt es nicht.')).toBeInTheDocument()
  })
})

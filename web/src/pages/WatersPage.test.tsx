import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { act, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { WaterShapes, WaterSummary } from '../api/types'
import type { MarkerMap } from '../components/MarkerMap'
import { WatersPage } from './WatersPage'

const mapProps = vi.hoisted(() => [] as Parameters<typeof MarkerMap>[0][])
vi.mock('../components/LazyMarkerMap', () => ({
  LazyMarkerMap: (props: Parameters<typeof MarkerMap>[0]) => {
    mapProps.push(props)
    return null
  },
}))

const waters: WaterSummary[] = [
  {
    id: 'd19708ca-ac42-4ad4-8d62-e85722f946f6',
    name: 'Donau',
    shortname: 'DONAU',
    slipwayCount: 9,
    stationCount: 17,
    position: { latitude: 48.93, longitude: 11.99 },
    gauge: null,
    countries: ['DE', 'AT'],
  },
  {
    id: '714b47c3-c63b-426f-accc-bcb7ebdde0e3',
    name: 'Schwinge',
    shortname: 'Schwinge',
    slipwayCount: 1,
    stationCount: 1,
    position: { latitude: 53.6, longitude: 9.47 },
    gauge: {
      stationId: '5a1f0d7e-2f43-4c53-9a4c-0f9e7f1d2b11',
      stationName: 'STADE',
      waterLevel: { value: 412, measuredAt: '2026-10-20T09:45:00Z', state: 'high' },
    },
    countries: ['DE'],
  },
  {
    id: 'c64f3da2-f392-4fb3-8a70-170b5a52b54f',
    name: 'Blömer',
    shortname: 'BLÖMER',
    slipwayCount: 0,
    stationCount: 0,
    position: null,
    gauge: null,
    countries: [],
  },
]

const shapes: WaterShapes = {
  type: 'FeatureCollection',
  features: [
    {
      type: 'Feature',
      id: waters[0].id,
      properties: { name: 'Donau' },
      geometry: { type: 'LineString', coordinates: [[11.9, 48.9], [13.5, 48.6]] },
    },
    {
      type: 'Feature',
      id: waters[1].id,
      properties: { name: 'Schwinge' },
      geometry: { type: 'LineString', coordinates: [[9.4, 53.6], [9.5, 53.65]] },
    },
  ],
}

/** Answers the list and the shapes like the API, each request with a fresh response. */
function stubApi(list: unknown = waters, collection: unknown = shapes) {
  const fetchMock = vi.fn((url: string) =>
    Promise.resolve(new Response(JSON.stringify(url.startsWith('/api/waters/shapes') ? collection : list), { status: 200 })),
  )
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function renderAt(url: string) {
  const router = createMemoryRouter([{ path: '/waters', element: <WatersPage /> }], { initialEntries: [url] })
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  )
}

/** Reports a map view like the map does once it is measured or moved. */
function moveMap(bbox: [number, number, number, number], zoom: number) {
  act(() => mapProps.at(-1)!.onViewChange?.({ bbox, zoom }))
}

describe('WatersPage', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    mapProps.length = 0
  })

  it('lists the waters in the map view with what lies on them', async () => {
    const fetchMock = stubApi()
    renderAt('/waters')
    expect(await screen.findByText(/3 Gewässer auf der Karte/)).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith('/api/waters', expect.anything())

    moveMap([5, 47, 15, 55], 8)

    expect(screen.getByText('2 Gewässer im Kartenausschnitt')).toBeInTheDocument()
    expect(screen.getByText('9 Slipanlagen · 17 Pegel')).toBeInTheDocument()
    expect(screen.getByText('1 Slipanlage · 1 Pegel')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Donau/ })).toHaveAttribute('href', `/waters/${waters[0].id}`)
    // Without a position it is not on the map, so only a search lists it.
    expect(screen.queryByText('Blömer')).not.toBeInTheDocument()
  })

  it('shows the reading of a single gauge', async () => {
    stubApi()
    renderAt('/waters')
    await screen.findByText(/3 Gewässer auf der Karte/)

    moveMap([5, 47, 15, 55], 8)

    expect(screen.getByText('412 cm · hoch')).toHaveClass('level-high')
    expect(screen.getByText(/Pegel STADE/)).toBeInTheDocument()
  })

  it('draws the shapes in the map view instead of bubbles, further out only the large ones', async () => {
    const fetchMock = stubApi()
    renderAt('/waters')
    await screen.findByText(/3 Gewässer auf der Karte/)
    expect(fetchMock).not.toHaveBeenCalledWith(expect.stringContaining('/api/waters/shapes'), expect.anything())

    moveMap([5, 47, 15, 55], 6)

    await waitFor(() => expect(mapProps.at(-1)?.shapes).toHaveLength(2))
    expect(mapProps.at(-1)?.markers).toEqual([])
    expect(mapProps.at(-1)?.shapes?.map((s) => [s.id, s.highlighted, s.popup?.to])).toEqual([
      [waters[0].id, false, `/waters/${waters[0].id}`],
      [waters[1].id, false, `/waters/${waters[1].id}`],
    ])
    expect(fetchMock).toHaveBeenCalledWith('/api/waters/shapes?bbox=-5.625,39.375,22.5,61.875&minExtent=0.4375', expect.anything())
  })

  it('highlights the water whose shape is selected', async () => {
    stubApi()
    renderAt('/waters')
    await screen.findByText(/3 Gewässer auf der Karte/)
    moveMap([5, 47, 15, 55], 6)
    await waitFor(() => expect(mapProps.at(-1)?.shapes).toHaveLength(2))

    act(() => mapProps.at(-1)!.onSelect?.(waters[1].id))
    expect(mapProps.at(-1)?.shapes?.map((s) => s.highlighted)).toEqual([false, true])

    act(() => mapProps.at(-1)!.onSelect?.(null))
    expect(mapProps.at(-1)?.shapes?.map((s) => s.highlighted)).toEqual([false, false])
  })

  it('keeps to the chosen country', async () => {
    stubApi()
    renderAt('/waters')
    await screen.findByText(/3 Gewässer auf der Karte/)
    expect(screen.getByRole('option', { name: 'Deutschland (2)' })).toBeInTheDocument()

    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Land' }), 'AT')
    expect(mapProps.at(-1)?.focus).toEqual({ south: 48.93, west: 11.99, north: 48.93, east: 11.99 })
    moveMap([5, 47, 15, 55], 8)
    expect(screen.getByText('1 Gewässer im Kartenausschnitt')).toBeInTheDocument()
    expect(screen.queryByText('Schwinge')).not.toBeInTheDocument()
    await waitFor(() => expect(mapProps.at(-1)?.shapes?.map((s) => s.id)).toEqual([waters[0].id]))
  })

  it('lists every hit of a search and loads their shapes', async () => {
    const fetchMock = stubApi()

    renderAt('/waters?q=Donau')

    expect(await screen.findByText('3 Treffer')).toBeInTheDocument()
    expect(screen.getByText('Blömer')).toBeInTheDocument()
    expect(screen.getByRole('searchbox')).toHaveValue('Donau')
    expect(fetchMock).toHaveBeenCalledWith('/api/waters?search=Donau', expect.anything())
    expect(fetchMock).toHaveBeenCalledWith('/api/waters/shapes?search=Donau', expect.anything())
  })

  it('shows an error when the API fails', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('', { status: 500, statusText: 'Server Error' })))

    renderAt('/waters')

    expect(await screen.findByText(/konnten nicht geladen werden/)).toBeInTheDocument()
  })
})

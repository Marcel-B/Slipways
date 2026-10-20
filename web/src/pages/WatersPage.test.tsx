import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { act, render, screen, waitFor } from '@testing-library/react'
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
  },
  {
    id: 'c64f3da2-f392-4fb3-8a70-170b5a52b54f',
    name: 'Blömer',
    shortname: 'BLÖMER',
    slipwayCount: 0,
    stationCount: 0,
    position: null,
    gauge: null,
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

describe('WatersPage', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    mapProps.length = 0
  })

  it('lists waters with what lies on them', async () => {
    const fetchMock = stubApi()

    renderAt('/waters')

    expect(await screen.findByText('Donau')).toBeInTheDocument()
    expect(screen.getByText('9 Slipanlagen · 17 Pegel')).toBeInTheDocument()
    expect(screen.getByText('1 Slipanlage · 1 Pegel')).toBeInTheDocument()
    expect(screen.getByText('Keine Slipanlagen oder Pegel')).toBeInTheDocument()
    expect(screen.getByText('3 Gewässer')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Donau/ })).toHaveAttribute('href', `/waters/${waters[0].id}`)
    expect(fetchMock).toHaveBeenCalledWith('/api/waters', expect.anything())
  })

  it('shows the reading of a single gauge', async () => {
    stubApi()

    renderAt('/waters')

    expect(await screen.findByText('412 cm · hoch')).toHaveClass('level-high')
    expect(screen.getByText(/Pegel STADE/)).toBeInTheDocument()
  })

  it('puts waters with a position on the map', async () => {
    stubApi()

    renderAt('/waters')

    await screen.findByText('Donau')
    expect(mapProps.at(-1)?.markers.map((m) => m.title)).toEqual(['Donau', 'Schwinge'])
    expect(mapProps.at(-1)?.markers[0].to).toBe(`/waters/${waters[0].id}`)
  })

  it('draws the shapes of all waters faintly', async () => {
    const fetchMock = stubApi()

    renderAt('/waters')

    await waitFor(() => expect(mapProps.at(-1)?.shapes).toHaveLength(2))
    expect(mapProps.at(-1)?.shapes?.map((s) => [s.id, s.highlighted, s.popup?.to])).toEqual([
      [waters[0].id, false, `/waters/${waters[0].id}`],
      [waters[1].id, false, `/waters/${waters[1].id}`],
    ])
    expect(fetchMock).toHaveBeenCalledWith('/api/waters/shapes', expect.anything())
  })

  it('highlights the water whose marker or shape is selected', async () => {
    stubApi()

    renderAt('/waters')
    await waitFor(() => expect(mapProps.at(-1)?.shapes).toHaveLength(2))

    act(() => mapProps.at(-1)!.onSelect?.(waters[1].id))
    expect(mapProps.at(-1)?.shapes?.map((s) => s.highlighted)).toEqual([false, true])

    act(() => mapProps.at(-1)!.onSelect?.(null))
    expect(mapProps.at(-1)?.shapes?.map((s) => s.highlighted)).toEqual([false, false])
  })

  it('passes the search term from the URL to the API', async () => {
    const fetchMock = stubApi([], { type: 'FeatureCollection', features: [] })

    renderAt('/waters?q=Donau')

    expect(await screen.findByText('0 Gewässer')).toBeInTheDocument()
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

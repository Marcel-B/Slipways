import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { act, render, screen } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { SlipwaySummary } from '../api/types'
import type { MarkerMap } from '../components/MarkerMap'
import type { MapView } from '../components/mapView'
import { SlipwaysPage } from './SlipwaysPage'

const map = vi.hoisted(() => ({ onViewChange: undefined as ((view: MapView) => void) | undefined }))
vi.mock('../components/LazyMarkerMap', () => ({
  LazyMarkerMap: (props: Parameters<typeof MarkerMap>[0]) => {
    map.onViewChange = props.onViewChange
    return null
  },
}))

const aroundKelheim: MapView = { bbox: [11.5, 48.7, 12.5, 49.1], zoom: 10 }
const aroundHamburg: MapView = { bbox: [9.5, 53.3, 10.5, 53.8], zoom: 10 }

const slipways: SlipwaySummary[] = [
  {
    id: 'eac96a93-fd47-4a9a-8771-0005805b9655',
    name: 'Am Jachthafen, Kelheim',
    latitude: 48.93,
    longitude: 11.99,
    postalCode: '93309',
    city: 'Kelheim',
    country: 'DE',
    water: { id: 'd19708ca-ac42-4ad4-8d62-e85722f946f6', name: 'Donau' },
    source: 'manual',
  },
]

function renderAt(url: string) {
  const router = createMemoryRouter([{ path: '/', element: <SlipwaysPage /> }], { initialEntries: [url] })
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  )
}

describe('SlipwaysPage', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('asks to zoom in or search while the map shows the whole country', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify(slipways), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    renderAt('/')

    expect(await screen.findByText('1 Slipanlage auf der Karte.')).toBeInTheDocument()
    expect(screen.queryByText('Am Jachthafen, Kelheim')).not.toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith('/api/slipways', expect.anything())
  })

  it('lists the slipways in the map view', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(slipways), { status: 200 })))
    renderAt('/')
    await screen.findByText('1 Slipanlage auf der Karte.')

    act(() => map.onViewChange?.(aroundKelheim))
    expect(screen.getByText('Am Jachthafen, Kelheim')).toBeInTheDocument()
    expect(screen.getByText('Donau · 93309 Kelheim')).toBeInTheDocument()
    expect(screen.getByText('1 Slipanlage im Kartenausschnitt')).toBeInTheDocument()

    act(() => map.onViewChange?.(aroundHamburg))
    expect(screen.queryByText('Am Jachthafen, Kelheim')).not.toBeInTheDocument()
    expect(screen.getByText(/Hier liegt keine Slipanlage/)).toBeInTheDocument()
  })

  it('lists every search hit, wherever the map is', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify(slipways), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    renderAt('/?q=Kelheim')

    expect(await screen.findByText('Am Jachthafen, Kelheim')).toBeInTheDocument()
    expect(screen.getByText('1 Treffer')).toBeInTheDocument()
    expect(screen.getByRole('searchbox')).toHaveValue('Kelheim')
    expect(fetchMock).toHaveBeenCalledWith('/api/slipways?search=Kelheim', expect.anything())
  })

  it('shows an error when the API fails', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('', { status: 500, statusText: 'Server Error' })))

    renderAt('/')

    expect(await screen.findByText(/konnten nicht geladen werden/)).toBeInTheDocument()
  })
})

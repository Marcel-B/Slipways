import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { act, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { SlipwaySummary } from '../api/types'
import type { MarkerMap } from '../components/MarkerMap'
import type { MapView } from '../components/mapView'
import { SlipwaysPage } from './SlipwaysPage'

type MapProps = Parameters<typeof MarkerMap>[0]
const map = vi.hoisted(() => ({
  onViewChange: undefined as ((view: MapView) => void) | undefined,
  markers: undefined as MapProps['markers'] | undefined,
  focus: undefined as MapProps['focus'],
  home: undefined as MapProps['home'],
}))
vi.mock('../components/LazyMarkerMap', () => ({
  LazyMarkerMap: (props: MapProps) => {
    map.onViewChange = props.onViewChange
    map.markers = props.markers
    map.focus = props.focus
    map.home = props.home
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

  it('narrows map and list to the chosen country', async () => {
    const amsterdam: SlipwaySummary = {
      ...slipways[0],
      id: '0b1f6c1e-3d7a-4b8e-9f2a-5c6d7e8f9a0b',
      name: 'Sloepenhelling',
      latitude: 52.37,
      longitude: 4.9,
      country: 'NL',
    }
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify([...slipways, amsterdam]), { status: 200 })))
    renderAt('/')

    expect(await screen.findByText('2 Slipanlagen auf der Karte.')).toBeInTheDocument()
    const select = screen.getByRole('combobox', { name: 'Land' })
    expect(screen.getByRole('option', { name: 'Deutschland (1)' })).toBeInTheDocument()

    await userEvent.selectOptions(select, 'NL')
    expect(screen.getByText('1 Slipanlage auf der Karte.')).toBeInTheDocument()
    expect(map.markers?.map((m) => m.title)).toEqual(['Sloepenhelling'])
    expect(map.focus).toEqual({ south: 52.37, west: 4.9, north: 52.37, east: 4.9 })
  })

  it('resets search, country and map with one button', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(slipways), { status: 200 })))
    renderAt('/')
    await screen.findByText('1 Slipanlage auf der Karte.')

    const europe: MapView = { bbox: [-20, 35, 30, 62], zoom: 4 }
    act(() => map.onViewChange?.(europe))
    expect(screen.queryByRole('button', { name: /Zurücksetzen/ })).not.toBeInTheDocument()

    act(() => map.onViewChange?.(aroundKelheim))
    await userEvent.type(screen.getByRole('searchbox'), 'Kelheim')
    const home = map.home
    await userEvent.click(screen.getByRole('button', { name: /Zurücksetzen/ }))

    expect(screen.getByRole('searchbox')).toHaveValue('')
    expect(map.home).toBe((home ?? 0) + 1)
    // The map reports its way back to the start, and the button goes away again.
    act(() => map.onViewChange?.(europe))
    expect(screen.queryByRole('button', { name: /Zurücksetzen/ })).not.toBeInTheDocument()
  })

  it('shows an error when the API fails', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('', { status: 500, statusText: 'Server Error' })))

    renderAt('/')

    expect(await screen.findByText(/konnten nicht geladen werden/)).toBeInTheDocument()
  })
})

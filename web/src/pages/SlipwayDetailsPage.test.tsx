import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { SlipwayDetails, Station, WaterDetails } from '../api/types'
import { SlipwayDetailsPage } from './SlipwayDetailsPage'

vi.mock('../components/LazyMarkerMap', () => ({ LazyMarkerMap: () => null }))

const donau = { id: 'd19708ca-ac42-4ad4-8d62-e85722f946f6', name: 'Donau' }

const slipway: SlipwayDetails = {
  id: 'eac96a93-fd47-4a9a-8771-0005805b9655',
  name: 'Am Jachthafen, Kelheim',
  latitude: 48.93,
  longitude: 11.99,
  postalCode: '93309',
  city: 'Kelheim',
  country: 'DE',
  water: donau,
  street: 'Am Jachthafen',
  comment: null,
  rating: 0,
  costs: 0,
  pro: null,
  contra: null,
  marina: null,
  extras: [],
  created: '2020-04-14T00:00:00Z',
  updated: null,
}

function station(name: string, latitude: number, value: number | null): Station {
  return {
    id: `id-${name}`,
    name,
    number: null,
    latitude,
    longitude: 11.99,
    km: 0,
    agency: null,
    water: donau,
    waterLevel: value === null ? null : { value, measuredAt: '2026-10-20T12:00:00Z', state: 'normal' },
  }
}

const water: WaterDetails = {
  ...donau,
  shortname: 'DONAU',
  bounds: null,
  shape: null,
  osmElements: [],
  slipways: [],
  // The closest one has no reading, so the next closest is shown.
  stations: [station('FERN', 49.5, 280), station('OHNE', 48.931, null), station('KELHEIM', 48.95, 312)],
  marinas: [],
}

function renderAt(url: string) {
  const router = createMemoryRouter([{ path: '/slipways/:id', element: <SlipwayDetailsPage /> }], {
    initialEntries: [url],
  })
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  )
}

describe('SlipwayDetailsPage', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('shows the slipway with its costs, a route link and the nearest gauge with a reading', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn((url: string) =>
        Promise.resolve(new Response(JSON.stringify(url.startsWith('/api/waters') ? water : slipway), { status: 200 })),
      ),
    )

    renderAt(`/slipways/${slipway.id}`)

    expect(await screen.findByRole('heading', { name: 'Am Jachthafen, Kelheim' })).toBeInTheDocument()
    expect(screen.getByText('kostenlos')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Route planen/ })).toHaveAttribute(
      'href',
      'https://www.google.com/maps/dir/?api=1&destination=48.93,11.99',
    )
    expect(await screen.findByRole('link', { name: 'KELHEIM' })).toHaveAttribute('href', `/waters/${donau.id}`)
    expect(screen.getByText('312 cm · normal')).toBeInTheDocument()
    expect(screen.getByText(/2,2 km entfernt/)).toBeInTheDocument()
  })

  it('reports an unknown slipway', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('', { status: 404, statusText: 'Not Found' })))

    renderAt(`/slipways/${slipway.id}`)

    expect(await screen.findByText('Diese Slipanlage gibt es nicht.')).toBeInTheDocument()
  })
})

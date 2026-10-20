import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { Marina, MarinaDetails } from '../api/types'
import type { MarkerMap } from '../components/MarkerMap'
import { MarinaDetailsPage } from './MarinaDetailsPage'
import { MarinasPage } from './MarinasPage'

const mapProps = vi.hoisted(() => [] as Parameters<typeof MarkerMap>[0][])
vi.mock('../components/LazyMarkerMap', () => ({
  LazyMarkerMap: (props: Parameters<typeof MarkerMap>[0]) => {
    mapProps.push(props)
    return null
  },
}))

const marina: Marina = {
  id: '4cfba36b-39a9-4f63-b155-32d6279de801',
  name: 'Yachthafen Kelheim',
  latitude: 48.92,
  longitude: 11.88,
  url: 'https://example.org',
  street: 'Am Hafen 1',
  postalCode: '93309',
  city: 'Kelheim',
  phone: '+49 9441 123',
  email: null,
  operator: null,
  fee: 'yes',
  access: null,
  openingHours: 'Mo-Fr 08:00-18:00',
  water: { id: 'd19708ca-ac42-4ad4-8d62-e85722f946f6', name: 'Donau' },
  source: 'manual',
  osmElement: null,
}

const details: MarinaDetails = {
  ...marina,
  // No water, so no gauge is looked up.
  water: null,
  infos: [],
  slipways: [
    {
      id: 'eac96a93-fd47-4a9a-8771-0005805b9655',
      name: 'Am Jachthafen, Kelheim',
      latitude: 48.93,
      longitude: 11.99,
      postalCode: '93309',
      city: 'Kelheim',
      country: 'DE',
      water: null,
      source: 'manual',
    },
  ],
}

function renderAt(url: string, body: unknown) {
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(new Response(JSON.stringify(body), { status: 200 }))))
  const router = createMemoryRouter(
    [
      { path: '/marinas', element: <MarinasPage /> },
      { path: '/marinas/:id', element: <MarinaDetailsPage /> },
    ],
    { initialEntries: [url] },
  )
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  )
}

describe('MarinasPage', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    mapProps.length = 0
  })

  it('lists marinas and puts them on the map', async () => {
    renderAt('/marinas?q=kelheim', [marina])

    expect(await screen.findByRole('link', { name: /Yachthafen Kelheim/ })).toHaveAttribute('href', `/marinas/${marina.id}`)
    expect(screen.getByText('Donau · 93309 Kelheim')).toBeInTheDocument()
    expect(screen.getByText('1 Treffer')).toBeInTheDocument()
    expect(fetch).toHaveBeenCalledWith('/api/marinas?search=kelheim', expect.anything())
    expect(mapProps.at(-1)?.markers.map((m) => m.kind)).toEqual(['marina'])
  })

  it('asks to zoom in instead of listing all marinas of the country', async () => {
    renderAt('/marinas', [marina])

    expect(await screen.findByText('1 Marina auf der Karte.')).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: /Yachthafen Kelheim/ })).not.toBeInTheDocument()
  })

  it('shows a marina with its slipways', async () => {
    renderAt(`/marinas/${marina.id}`, details)

    expect(await screen.findByRole('heading', { name: 'Yachthafen Kelheim' })).toBeInTheDocument()
    expect(screen.getByText('kostenpflichtig')).toBeInTheDocument()
    expect(screen.getByText('Mo-Fr 08:00-18:00')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: '+49 9441 123' })).toHaveAttribute('href', 'tel:+499441123')
    expect(screen.getByRole('link', { name: /Am Jachthafen, Kelheim/ })).toHaveAttribute(
      'href',
      '/slipways/eac96a93-fd47-4a9a-8771-0005805b9655',
    )
    expect(mapProps.at(-1)?.markers.map((m) => m.kind)).toEqual(['marina', 'slipway'])
  })
})

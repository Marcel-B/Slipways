import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { SlipwayDetails } from '../api/types'
import { SlipwayDetailsPage } from './SlipwayDetailsPage'

vi.mock('../components/LazyMarkerMap', () => ({ LazyMarkerMap: () => null }))

const imported: SlipwayDetails = {
  id: '05df3689-6855-41e9-a1d8-f4c923603308',
  name: 'Slipanlage',
  latitude: 47.06824,
  longitude: 15.43536,
  street: null,
  postalCode: null,
  city: null,
  country: 'AT',
  url: null,
  comment: null,
  rating: 0,
  costs: null,
  pro: null,
  contra: null,
  operator: 'Stadt Graz',
  fee: 'no',
  access: 'customers',
  openingHours: null,
  surface: 'concrete',
  water: null,
  marina: null,
  extras: [],
  source: 'osm',
  osmElement: 'way/1050341846',
  created: '2026-10-20T15:05:14Z',
  updated: null,
}

function renderSlipway(slipway: SlipwayDetails) {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(slipway), { status: 200 })))
  const router = createMemoryRouter([{ path: '/slipways/:id', element: <SlipwayDetailsPage /> }], {
    initialEntries: [`/slipways/${slipway.id}`],
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

  it('shows tags of an imported slipway and credits OpenStreetMap', async () => {
    renderSlipway(imported)

    expect(await screen.findByRole('heading', { name: 'Slipanlage' })).toBeInTheDocument()
    expect(screen.queryByText('Gewässer')).not.toBeInTheDocument()
    expect(screen.getByText('kostenlos')).toBeInTheDocument()
    expect(screen.getByText('nur für Kunden')).toBeInTheDocument()
    expect(screen.getByText('Beton')).toBeInTheDocument()
    expect(screen.getByText('Stadt Graz')).toBeInTheDocument()
    expect(screen.getByText(/© OpenStreetMap-Mitwirkende/)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'in OpenStreetMap ansehen' })).toHaveAttribute(
      'href',
      'https://www.openstreetmap.org/way/1050341846',
    )
  })

  it('marks our own slipways as such', async () => {
    renderSlipway({
      ...imported,
      name: 'Am Jachthafen, Kelheim',
      source: 'manual',
      osmElement: null,
      water: { id: 'd19708ca-ac42-4ad4-8d62-e85722f946f6', name: 'Donau' },
    })

    expect(await screen.findByRole('link', { name: 'Donau' })).toHaveAttribute(
      'href',
      '/waters/d19708ca-ac42-4ad4-8d62-e85722f946f6',
    )
    expect(screen.getByText('eigene Erfassung')).toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'in OpenStreetMap ansehen' })).not.toBeInTheDocument()
  })
})

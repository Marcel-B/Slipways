import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { MarinaDetails } from '../api/types'
import { MarinaDetailsPage } from './MarinaDetailsPage'

vi.mock('../components/LazyMarkerMap', () => ({ LazyMarkerMap: () => null }))

const marina: MarinaDetails = {
  id: '4cfba36b-39a9-4f63-b155-32d6279de801',
  name: 'Testhafen',
  latitude: 53.0,
  longitude: 8.0,
  url: 'https://testhafen.example/',
  street: null,
  postalCode: null,
  city: null,
  country: null,
  phone: null,
  email: null,
  operator: null,
  fee: null,
  access: null,
  openingHours: null,
  water: null,
  source: 'osm',
  osmElement: 'way/4001',
  slipways: [],
  infos: [],
}

function renderMarina(details: MarinaDetails) {
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(new Response(JSON.stringify(details), { status: 200 }))))
  const router = createMemoryRouter([{ path: '/marinas/:id', element: <MarinaDetailsPage /> }], {
    initialEntries: [`/marinas/${details.id}`],
  })
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  )
}

const website = { source: 'website', sourceUrl: 'https://testhafen.example/', retrieved: '2026-10-20T12:00:00Z' }

describe('MarinaDetailsPage', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('fills gaps with details from the website and says where they come from', async () => {
    renderMarina({
      ...marina,
      infos: [
        { ...website, field: 'phone', value: '+49 4401 12345' },
        { ...website, field: 'opening_hours', value: 'Mo-Fr 08:00-18:00' },
      ],
    })

    expect(await screen.findByRole('link', { name: '+49 4401 12345' })).toHaveAttribute('href', 'tel:+49440112345')
    expect(screen.getByText('Mo-Fr 08:00-18:00')).toBeInTheDocument()
    expect(screen.getAllByRole('link', { name: 'laut Webseite' })).toHaveLength(2)
    expect(screen.getAllByText(/Stand 20\.10\.2026/)).toHaveLength(2)
  })

  it('prefers the marina’s own values', async () => {
    renderMarina({
      ...marina,
      phone: '0441 999',
      infos: [{ ...website, field: 'phone', value: '+49 4401 12345' }],
    })

    expect(await screen.findByRole('link', { name: '0441 999' })).toBeInTheDocument()
    expect(screen.queryByText('+49 4401 12345')).not.toBeInTheDocument()
    expect(screen.queryByText('laut Webseite')).not.toBeInTheDocument()
  })
})

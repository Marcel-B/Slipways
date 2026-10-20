import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { SlipwaySummary } from '../api/types'
import { SlipwaysPage } from './SlipwaysPage'

vi.mock('../components/LazyMarkerMap', () => ({ LazyMarkerMap: () => null }))

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

  it('lists slipways from the API', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify(slipways), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    renderAt('/')

    expect(await screen.findByText('Am Jachthafen, Kelheim')).toBeInTheDocument()
    expect(screen.getByText('Donau · 93309 Kelheim')).toBeInTheDocument()
    expect(screen.getByText('1 Slipanlagen')).toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith('/api/slipways', expect.anything())
  })

  it('passes the search term from the URL to the API', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('[]', { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    renderAt('/?q=Kelheim')

    expect(await screen.findByText('0 Slipanlagen')).toBeInTheDocument()
    expect(screen.getByRole('searchbox')).toHaveValue('Kelheim')
    expect(fetchMock).toHaveBeenCalledWith('/api/slipways?search=Kelheim', expect.anything())
  })

  it('shows an error when the API fails', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('', { status: 500, statusText: 'Server Error' })))

    renderAt('/')

    expect(await screen.findByText(/konnten nicht geladen werden/)).toBeInTheDocument()
  })
})

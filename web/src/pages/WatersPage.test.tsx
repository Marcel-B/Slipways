import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { WaterSummary } from '../api/types'
import { WatersPage } from './WatersPage'

vi.mock('../components/LazyMarkerMap', () => ({ LazyMarkerMap: () => null }))

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
  afterEach(() => vi.unstubAllGlobals())

  it('lists waters with what lies on them', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify(waters), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

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
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(waters), { status: 200 })))

    renderAt('/waters')

    expect(await screen.findByText('412 cm · hoch')).toHaveClass('level-high')
    expect(screen.getByText(/Pegel STADE/)).toBeInTheDocument()
  })

  it('passes the search term from the URL to the API', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('[]', { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    renderAt('/waters?q=Donau')

    expect(await screen.findByText('0 Gewässer')).toBeInTheDocument()
    expect(screen.getByRole('searchbox')).toHaveValue('Donau')
    expect(fetchMock).toHaveBeenCalledWith('/api/waters?search=Donau', expect.anything())
  })

  it('shows an error when the API fails', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('', { status: 500, statusText: 'Server Error' })))

    renderAt('/waters')

    expect(await screen.findByText(/konnten nicht geladen werden/)).toBeInTheDocument()
  })
})

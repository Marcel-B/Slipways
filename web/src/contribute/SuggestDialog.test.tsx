import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { solve } from './proof'
import { sentFields } from './storage'
import { SuggestDialog, type SuggestMode } from './SuggestDialog'

const place = { type: 'slipway' as const, id: 'slip-1', name: 'Slip am See', source: 'osm' as const }

async function sha256(text: string) {
  const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(text))
  return Array.from(new Uint8Array(digest), (b) => b.toString(16).padStart(2, '0')).join('')
}

async function challenge() {
  return { algorithm: 'SHA-256', challenge: await sha256('salt?expires=1' + 7), maxNumber: 20, salt: 'salt?expires=1', signature: 'sig' }
}

function renderDialog(mode: SuggestMode, current = {}) {
  const router = createMemoryRouter([
    { path: '/', element: <SuggestDialog place={place} mode={mode} current={current} onClose={() => {}} /> },
  ])
  render(<RouterProvider router={router} />)
}

afterEach(() => {
  vi.unstubAllGlobals()
  localStorage.clear()
})

describe('proof of work', () => {
  it('finds the number behind the challenge', async () => {
    expect((await solve(await challenge())).number).toBe(7)
  })
})

describe('SuggestDialog', () => {
  it('sends only changed values with the solved proof and remembers them', async () => {
    const fetch = vi.fn(async (url: string, _init?: RequestInit) =>
      url.endsWith('/challenge')
        ? Response.json(await challenge())
        : Response.json({ id: 'abc', token: 'secret' }, { status: 202, headers: { 'Content-Type': 'application/json' } }),
    )
    vi.stubGlobal('fetch', fetch)
    const user = userEvent.setup()
    renderDialog({ kind: 'edit', fields: ['surface', 'access'] }, { access: 'yes' })

    await user.selectOptions(screen.getByLabelText('Untergrund'), 'concrete')
    expect(screen.getByRole('button', { name: 'Vorschlag senden' })).toBeDisabled()
    expect(screen.getByText(/unter der ODbL/)).toBeInTheDocument()
    await user.click(screen.getByRole('checkbox'))
    await screen.findByText('Spam-Schutz bereit')
    await user.click(screen.getByRole('button', { name: 'Vorschlag senden' }))

    expect(await screen.findByText('Danke!')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Stand meines Vorschlags' })).toHaveAttribute('href', '/beitrag/abc#secret')
    const body = JSON.parse(fetch.mock.calls[1][1]!.body as string)
    expect(body).toMatchObject({ kind: 'edit', placeType: 'slipway', placeId: 'slip-1', fields: { surface: 'concrete' }, homepage: '' })
    expect(body.proof.number).toBe(7)
    expect(sentFields('slipway', 'slip-1', null)).toEqual(new Set(['surface']))
  })

  it('explains when the sender sent too many', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async (url: string) =>
        url.endsWith('/challenge') ? Response.json(await challenge()) : new Response('', { status: 429 }),
      ),
    )
    const user = userEvent.setup()
    renderDialog({ kind: 'tip' })

    await user.type(screen.getByLabelText(/Dein Tipp/), 'Früh kommen.')
    await user.click(screen.getByRole('checkbox'))
    await screen.findByText('Spam-Schutz bereit')
    await user.click(screen.getByRole('button', { name: 'Vorschlag senden' }))

    expect(await screen.findByText(/viele Vorschläge/)).toBeInTheDocument()
  })
})

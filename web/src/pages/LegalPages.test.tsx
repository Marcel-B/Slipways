import { render, screen } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { describe, expect, it } from 'vitest'
import { routes } from '../routes'

function renderAt(url: string) {
  render(<RouterProvider router={createMemoryRouter(routes, { initialEntries: [url] })} />)
}

describe('legal pages', () => {
  it.each([
    ['/impressum', 'Impressum'],
    ['/datenschutz', 'Datenschutzerklärung'],
    ['/support', 'Support'],
  ])('%s shows its heading', async (url, heading) => {
    renderAt(url)
    expect(await screen.findByRole('heading', { level: 1, name: heading })).toBeInTheDocument()
  })

  it('names the operator without placeholders', async () => {
    renderAt('/impressum')
    const address = (await screen.findByText(/Scheiblerstraße 111/)).closest('address')
    expect(address).toHaveTextContent('Marcel BendersScheiblerstraße 11147800 KrefeldDeutschland')
    expect(screen.getByRole('link', { name: 'slipways@marcelbenders.com' })).toHaveAttribute(
      'href',
      'mailto:slipways@marcelbenders.com',
    )
    expect(screen.queryByText(/Telefon/)).not.toBeInTheDocument()
    expect(document.querySelector('mark.placeholder')).toBeNull()
  })

  it.each(['/datenschutz', '/support'])('%s has no placeholders left', async (url) => {
    renderAt(url)
    await screen.findByRole('heading', { level: 1 })
    expect(document.querySelector('mark.placeholder')).toBeNull()
  })

  it('names the license in the footer and the imprint', async () => {
    renderAt('/impressum')
    const footer = (await screen.findByRole('contentinfo')) as HTMLElement
    const license = 'https://github.com/Marcel-B/Slipways/blob/master/LICENSE'
    expect(footer.querySelector(`a[href="${license}"]`)).toHaveTextContent('PolyForm Strict 1.0.0')
    expect(screen.getByRole('heading', { level: 2, name: 'Urheberrecht und Lizenz' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'PolyForm Strict License 1.0.0' })).toHaveAttribute('href', license)
  })

  it('links the legal pages in the footer', async () => {
    renderAt('/impressum')
    const footer = (await screen.findByRole('contentinfo')) as HTMLElement
    for (const name of ['Impressum', 'Datenschutz', 'Support']) {
      expect(footer.querySelector(`a[href="/${name.toLowerCase()}"]`)).not.toBeNull()
    }
  })
})

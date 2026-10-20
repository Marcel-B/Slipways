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

  it('links the legal pages in the footer', async () => {
    renderAt('/impressum')
    const footer = (await screen.findByRole('contentinfo')) as HTMLElement
    for (const name of ['Impressum', 'Datenschutz', 'Support']) {
      expect(footer.querySelector(`a[href="/${name.toLowerCase()}"]`)).not.toBeNull()
    }
  })
})

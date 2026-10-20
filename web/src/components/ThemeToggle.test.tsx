import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it } from 'vitest'
import { ThemeToggle } from './ThemeToggle'

describe('ThemeToggle', () => {
  afterEach(() => {
    localStorage.clear()
    delete document.documentElement.dataset.theme
  })

  it('follows the system until a theme is chosen, and remembers the choice', async () => {
    const { unmount } = render(<ThemeToggle />)
    expect(screen.getByRole('button', { name: 'Wie das System' })).toHaveAttribute('aria-pressed', 'true')
    expect(document.documentElement.dataset.theme).toBe('light')

    await userEvent.click(screen.getByRole('button', { name: 'Dunkel' }))
    expect(document.documentElement.dataset.theme).toBe('dark')
    expect(localStorage.getItem('slipways-theme')).toBe('dark')

    unmount()
    render(<ThemeToggle />)
    expect(screen.getByRole('button', { name: 'Dunkel' })).toHaveAttribute('aria-pressed', 'true')
  })

  it('forgets the choice when going back to the system setting', async () => {
    localStorage.setItem('slipways-theme', 'light')
    render(<ThemeToggle />)

    await userEvent.click(screen.getByRole('button', { name: 'Wie das System' }))
    expect(localStorage.getItem('slipways-theme')).toBeNull()
  })
})

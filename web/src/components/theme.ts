import { useEffect, useState } from 'react'

export type ThemeChoice = 'system' | 'light' | 'dark'

// The same key and resolution as the inline script in index.html, which sets the theme before the first paint.
const STORAGE_KEY = 'slipways-theme'

function storedChoice(): ThemeChoice {
  try {
    const value = localStorage.getItem(STORAGE_KEY)
    return value === 'light' || value === 'dark' ? value : 'system'
  } catch {
    return 'system'
  }
}

function systemPrefersDark(): boolean {
  return window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false
}

function apply(choice: ThemeChoice) {
  const dark = choice === 'dark' || (choice === 'system' && systemPrefersDark())
  document.documentElement.dataset.theme = dark ? 'dark' : 'light'
}

/** The chosen theme, kept in local storage; "system" follows the operating system, also when it changes. */
export function useTheme(): [ThemeChoice, (choice: ThemeChoice) => void] {
  const [choice, setChoice] = useState(storedChoice)

  useEffect(() => {
    apply(choice)
    try {
      if (choice === 'system') localStorage.removeItem(STORAGE_KEY)
      else localStorage.setItem(STORAGE_KEY, choice)
    } catch {
      // Without storage the choice lasts until the page is reloaded.
    }
    if (choice !== 'system' || !window.matchMedia) return
    const query = window.matchMedia('(prefers-color-scheme: dark)')
    const onChange = () => apply('system')
    query.addEventListener('change', onChange)
    return () => query.removeEventListener('change', onChange)
  }, [choice])

  return [choice, setChoice]
}

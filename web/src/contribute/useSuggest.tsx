import { useCallback, useState } from 'react'
import { SuggestDialog, type SuggestMode, type SuggestPlace } from './SuggestDialog'

/** Opens the suggestion form for a place; render `dialog` once on the page. */
export function useSuggest(place: SuggestPlace, current: Record<string, string | null | undefined>) {
  const [mode, setMode] = useState<SuggestMode | null>(null)
  const close = useCallback(() => setMode(null), [])
  return {
    open: setMode,
    dialog: mode && <SuggestDialog place={place} mode={mode} current={current} onClose={close} />,
  }
}


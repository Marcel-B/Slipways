import type { PlaceType, SuggestionCreated, SuggestionKind } from '../api/types'

/** A suggestion sent from this browser; kept only here, so the page can say what is waiting and link to its status. */
export interface SentSuggestion extends SuggestionCreated {
  kind: SuggestionKind
  placeType?: PlaceType
  placeId?: string
  fields: string[]
  sent: string
}

const KEY = 'slipways-suggestions'
// Older entries are decided or deleted on the server by then (see the privacy page).
const KEEP_DAYS = 90

export function sentSuggestions(): SentSuggestion[] {
  try {
    const all = JSON.parse(localStorage.getItem(KEY) ?? '[]') as SentSuggestion[]
    const cutoff = Date.now() - KEEP_DAYS * 24 * 60 * 60 * 1000
    return all.filter((s) => Date.parse(s.sent) > cutoff)
  } catch {
    return []
  }
}

export function rememberSuggestion(suggestion: SentSuggestion) {
  try {
    localStorage.setItem(KEY, JSON.stringify([...sentSuggestions(), suggestion]))
  } catch {
    // Without storage the sender still has the status link on screen.
  }
}

export function forgetSuggestion(id: string) {
  try {
    localStorage.setItem(KEY, JSON.stringify(sentSuggestions().filter((s) => s.id !== id)))
  } catch {
    // Nothing to forget then.
  }
}

// A suggestion still undecided after this long was most likely turned down.
const PENDING_DAYS = 30

/**
 * Fields of a place this browser sent values for that still wait, as far as the page can tell: sent recently, and
 * after the place last changed (`changed`: when it was updated or got a value from users).
 */
export function sentFields(placeType: PlaceType, placeId: string, changed: string | null): Set<string> {
  const cutoff = Date.now() - PENDING_DAYS * 24 * 60 * 60 * 1000
  return new Set(
    sentSuggestions()
      .filter((s) => s.placeType === placeType && s.placeId === placeId)
      .filter((s) => Date.parse(s.sent) > cutoff && (!changed || s.sent > changed))
      .flatMap((s) => s.fields),
  )
}

/** The latest of the given timestamps, for `sentFields`. */
export function latest(...dates: (string | null | undefined)[]): string | null {
  return dates.filter((d): d is string => !!d).sort().at(-1) ?? null
}

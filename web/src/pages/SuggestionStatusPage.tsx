import { useMutation, useQuery } from '@tanstack/react-query'
import { ArrowLeft, CheckCircle2, Clock, XCircle } from 'lucide-react'
import { Link, useLocation, useParams } from 'react-router'
import { api, ApiError } from '../api/client'
import { forgetSuggestion } from '../contribute/storage'

const kinds = {
  new_slipway: 'Neue Slipanlage',
  edit: 'Ergänzung',
  tip: 'Tipp',
  report: 'Hinweis auf ein Problem',
}

/** The state of a suggestion for its sender; the token is in the fragment, so it never shows up in a request line. */
export function SuggestionStatusPage() {
  const { id = '' } = useParams()
  const token = useLocation().hash.slice(1)
  const { data, error, isPending, refetch } = useQuery({
    queryKey: ['suggestion', id, token],
    queryFn: ({ signal }) => api.suggestion(id, token, signal),
    retry: false,
  })
  const withdraw = useMutation({
    mutationFn: () => api.withdrawSuggestion(id, token),
    onSuccess: () => {
      forgetSuggestion(id)
      void refetch()
    },
  })

  const date = (value: string) => new Date(value).toLocaleDateString('de-DE')

  return (
    <article className="details narrow">
      <Link to="/" className="back">
        <ArrowLeft size={16} /> Zur Karte
      </Link>
      <h1>Dein Vorschlag</h1>
      {isPending && <p className="muted">Lade…</p>}
      {error && (
        <p>
          {error instanceof ApiError && error.status === 404
            ? 'Diesen Vorschlag gibt es nicht (mehr). Entschiedene Vorschläge werden nach einiger Zeit gelöscht, zurückgezogene sofort.'
            : 'Der Stand konnte nicht geladen werden.'}
        </p>
      )}
      {data && (
        <section className="panel suggestion-status">
          <p className="muted">
            {kinds[data.kind]}, gesendet am {date(data.created)}
          </p>
          {data.status === 'pending' && (
            <>
              <p className="status-line">
                <Clock size={20} /> Wird geprüft
              </p>
              <button type="button" className="button" onClick={() => withdraw.mutate()} disabled={withdraw.isPending}>
                Vorschlag zurückziehen
              </button>
            </>
          )}
          {data.status === 'accepted' && (
            <p className="status-line accepted">
              <CheckCircle2 size={20} /> Angenommen am {date(data.decided!)}. Danke!
            </p>
          )}
          {data.status === 'rejected' && (
            <p className="status-line rejected">
              <XCircle size={20} /> Nicht übernommen ({date(data.decided!)}). Oft fehlt eine Quelle oder es passt nicht
              zum Eintrag.
            </p>
          )}
        </section>
      )}
    </article>
  )
}

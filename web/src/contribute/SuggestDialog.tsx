import { CheckCircle2, Loader2, ShieldCheck, X } from 'lucide-react'
import { useEffect, useId, useRef, useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { api, ApiError } from '../api/client'
import type { PlaceType, Source, SuggestionCreated } from '../api/types'
import { FIELDS } from './fields'
import { rememberSuggestion } from './storage'
import { useProof } from './useProof'

export type SuggestMode = { kind: 'edit'; fields: string[]; title?: string } | { kind: 'tip' } | { kind: 'report' }

export interface SuggestPlace {
  type: PlaceType
  id: string
  name: string
  source: Source
}

interface SuggestDialogProps {
  place: SuggestPlace
  mode: SuggestMode
  /** The values the place shows now, to fill in the form. */
  current?: Record<string, string | null | undefined>
  onClose: () => void
}

const TIP_LENGTH = 200
const TEXT_LENGTH = 500

const titles = { tip: 'Tipp geben', report: 'Etwas stimmt nicht' }

export function SuggestDialog({ place, mode, current = {}, onClose }: SuggestDialogProps) {
  const titleId = useId()
  const [values, setValues] = useState<Record<string, string>>(() =>
    mode.kind === 'edit' ? Object.fromEntries(mode.fields.map((f) => [f, current[f] ?? ''])) : {},
  )
  const [text, setText] = useState('')
  const [homepage, setHomepage] = useState('')
  const [consent, setConsent] = useState(false)
  const [sending, setSending] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [created, setCreated] = useState<SuggestionCreated | null>(null)
  const proof = useProof(!created)
  const first = useRef<HTMLElement | null>(null)

  useEffect(() => {
    first.current?.focus()
    const onKey = (event: KeyboardEvent) => event.key === 'Escape' && onClose()
    document.addEventListener('keydown', onKey)
    return () => document.removeEventListener('keydown', onKey)
  }, [onClose])

  // Only what the sender changed goes to the server.
  const changed =
    mode.kind === 'edit'
      ? Object.fromEntries(Object.entries(values).filter(([f, v]) => v.trim() !== '' && v !== (current[f] ?? '')))
      : {}
  const hasContent = mode.kind === 'edit' ? Object.keys(changed).length > 0 : text.trim() !== ''
  const canSend = hasContent && consent && proof.solution !== null && !sending

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (!canSend || !proof.solution) return
    setSending(true)
    setError(null)
    try {
      const result = await api.suggest({
        kind: mode.kind,
        placeType: place.type,
        placeId: place.id,
        fields: mode.kind === 'edit' ? changed : undefined,
        text: text.trim() || undefined,
        homepage,
        proof: proof.solution,
      })
      rememberSuggestion({
        ...result,
        kind: mode.kind,
        placeType: place.type,
        placeId: place.id,
        fields: Object.keys(changed),
        sent: new Date().toISOString(),
      })
      setCreated(result)
    } catch (reason) {
      setError(reason instanceof ApiError && reason.status === 429
        ? 'Du hast gerade viele Vorschläge geschickt. Bitte versuch es später noch einmal.'
        : (reason as Error).message)
      proof.renew()
    } finally {
      setSending(false)
    }
  }

  const title = mode.kind === 'edit' ? (mode.title ?? 'Angaben ergänzen') : titles[mode.kind]

  return (
    <div className="sheet-backdrop" onClick={onClose}>
      <div className="sheet" role="dialog" aria-modal="true" aria-labelledby={titleId} onClick={(e) => e.stopPropagation()}>
        <header className="sheet-head">
          <div>
            <h2 id={titleId}>{title}</h2>
            <p className="muted">{place.name}</p>
          </div>
          <button type="button" className="icon-button" onClick={onClose} aria-label="Schließen">
            <X size={20} />
          </button>
        </header>

        {created ? (
          <div className="sheet-done">
            <CheckCircle2 size={32} className="done-icon" />
            <p>
              <strong>Danke!</strong> Dein Vorschlag wird geprüft und erscheint nach der Freigabe.
            </p>
            <p className="muted">
              Unter diesem Link siehst du den Stand und kannst den Vorschlag zurückziehen. Dieser Browser merkt ihn sich,
              sonst gern speichern:
            </p>
            <Link to={`/beitrag/${created.id}#${created.token}`} onClick={onClose}>
              Stand meines Vorschlags
            </Link>
            <div className="sheet-actions">
              <button type="button" className="button button-primary" onClick={onClose}>
                Fertig
              </button>
            </div>
          </div>
        ) : (
          <form onSubmit={submit} className="suggest-form">
            {mode.kind === 'edit' &&
              mode.fields.map((name, index) => {
                const field = FIELDS[name]
                const ref = index === 0 ? (el: HTMLElement | null) => void (first.current = el) : undefined
                const set = (value: string) => setValues((v) => ({ ...v, [name]: value }))
                return (
                  <label key={name} className="field">
                    <span>{field.label}</span>
                    {field.options ? (
                      <select ref={ref} value={values[name]} onChange={(e) => set(e.target.value)}>
                        <option value="">weiß nicht</option>
                        {field.options.map(([value, label]) => (
                          <option key={value} value={value}>
                            {label}
                          </option>
                        ))}
                      </select>
                    ) : (
                      <input
                        ref={ref}
                        value={values[name]}
                        onChange={(e) => set(e.target.value)}
                        placeholder={field.placeholder}
                        inputMode={field.inputMode}
                        maxLength={100}
                      />
                    )}
                  </label>
                )
              })}

            <label className="field">
              <span>
                {mode.kind === 'edit'
                  ? 'Woher weißt du das? (freiwillig)'
                  : mode.kind === 'tip'
                    ? 'Dein Tipp'
                    : 'Was stimmt nicht?'}
              </span>
              <textarea
                ref={mode.kind === 'edit' ? undefined : (el) => void (first.current = el)}
                value={text}
                onChange={(e) => setText(e.target.value)}
                maxLength={mode.kind === 'tip' ? TIP_LENGTH : TEXT_LENGTH}
                rows={mode.kind === 'edit' ? 2 : 4}
                placeholder={
                  mode.kind === 'tip'
                    ? 'z. B. „Am Wochenende früh kommen, der Parkplatz ist klein.“'
                    : mode.kind === 'report'
                      ? 'z. B. „Die Slipanlage ist seit Sommer gesperrt.“'
                      : 'z. B. „War im August dort.“'
                }
              />
              {mode.kind === 'tip' && (
                <small className="muted">
                  {text.length}/{TIP_LENGTH} Zeichen. Bitte sachlich, ohne Werbung und ohne Links.
                </small>
              )}
            </label>

            {/* For bots only: people never see it. */}
            <label className="visually-hidden" aria-hidden="true">
              Homepage
              <input tabIndex={-1} autoComplete="off" value={homepage} onChange={(e) => setHomepage(e.target.value)} />
            </label>

            <label className="consent">
              <input type="checkbox" checked={consent} onChange={(e) => setConsent(e.target.checked)} />
              <span>
                Ich weiß das aus eigener Erfahrung und habe es nicht aus anderen Verzeichnissen abgeschrieben. Slipways
                darf den Beitrag veröffentlichen
                {place.source === 'osm' && ', als Ergänzung zu OpenStreetMap unter der ODbL'}.{' '}
                <Link to="/support#mitmachen" onClick={onClose}>
                  Mehr dazu
                </Link>
              </span>
            </label>

            {error && <p className="error">{error}</p>}
            {proof.error && <p className="error">{proof.error}</p>}

            <div className="sheet-actions">
              <small className="proof-state muted">
                {proof.solution ? (
                  <>
                    <ShieldCheck size={14} /> Spam-Schutz bereit
                  </>
                ) : (
                  <>
                    <Loader2 size={14} className="spin" /> Spam-Schutz wird berechnet…
                  </>
                )}
              </small>
              <button type="submit" className="button button-primary" disabled={!canSend}>
                {sending ? 'Wird gesendet…' : 'Vorschlag senden'}
              </button>
            </div>
            <p className="muted small">Alles wird vor der Veröffentlichung von Hand geprüft. Kein Konto, keine Namen.</p>
          </form>
        )}
      </div>
    </div>
  )
}

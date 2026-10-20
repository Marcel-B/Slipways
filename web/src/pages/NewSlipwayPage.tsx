import { ArrowLeft, CheckCircle2, Loader2, MapPin, Send, ShieldCheck } from 'lucide-react'
import { useCallback, useState, type FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router'
import { api, ApiError, NearbySlipwaysError } from '../api/client'
import type { NearbySlipway, Position, SuggestionCreated } from '../api/types'
import { FACTS, FIELDS } from '../contribute/fields'
import { rememberSuggestion } from '../contribute/storage'
import { useProof } from '../contribute/useProof'
import { LazyPickMap } from '../components/LazyPickMap'

const GERMANY = { latitude: 51.2, longitude: 10.4 }

/** Where the pin starts: the middle of the map the sender came from, else Germany. */
function start(params: URLSearchParams): { position: Position; zoom: number } {
  const latitude = Number(params.get('lat'))
  const longitude = Number(params.get('lon'))
  const zoom = Number(params.get('z'))
  return latitude && longitude
    ? { position: { latitude, longitude }, zoom: zoom >= 3 && zoom <= 19 ? zoom : 14 }
    : { position: GERMANY, zoom: 6 }
}

export function NewSlipwayPage() {
  const [params] = useSearchParams()
  const [initial] = useState(() => start(params))
  const [position, setPosition] = useState<Position>(initial.position)
  const [moved, setMoved] = useState(params.has('lat'))
  const [name, setName] = useState('')
  const [fields, setFields] = useState<Record<string, string>>({})
  const [text, setText] = useState('')
  const [homepage, setHomepage] = useState('')
  const [consent, setConsent] = useState(false)
  const [nearby, setNearby] = useState<NearbySlipway[] | null>(null)
  const [sending, setSending] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [created, setCreated] = useState<SuggestionCreated | null>(null)
  const proof = useProof(!created)

  const move = useCallback((next: Position) => {
    setPosition(next)
    setMoved(true)
    setNearby(null)
  }, [])

  const set = (field: string, value: string) => setFields((f) => ({ ...f, [field]: value }))
  const filled = Object.fromEntries(Object.entries(fields).filter(([, v]) => v.trim() !== ''))
  const canSend = name.trim() !== '' && moved && consent && proof.solution !== null && !sending

  async function submit(event: FormEvent, confirmNew = false) {
    event.preventDefault()
    if (!canSend || !proof.solution) return
    setSending(true)
    setError(null)
    try {
      const result = await api.suggest({
        kind: 'new_slipway',
        name: name.trim(),
        latitude: Number(position.latitude.toFixed(6)),
        longitude: Number(position.longitude.toFixed(6)),
        fields: filled,
        text: text.trim() || undefined,
        confirmNew,
        homepage,
        proof: proof.solution,
      })
      rememberSuggestion({ ...result, kind: 'new_slipway', fields: [], sent: new Date().toISOString() })
      setCreated(result)
    } catch (reason) {
      if (reason instanceof NearbySlipwaysError) setNearby(reason.slipways)
      else
        setError(
          reason instanceof ApiError && reason.status === 429
            ? 'Du hast gerade viele Vorschläge geschickt. Bitte versuch es später noch einmal.'
            : (reason as Error).message,
        )
      proof.renew()
    } finally {
      setSending(false)
    }
  }

  if (created)
    return (
      <article className="details narrow">
        <section className="panel sheet-done">
          <CheckCircle2 size={32} className="done-icon" />
          <h1>Danke!</h1>
          <p>Deine Slipanlage wird geprüft und erscheint nach der Freigabe auf der Karte.</p>
          <p>
            <Link to={`/beitrag/${created.id}#${created.token}`}>Stand meines Vorschlags</Link>
          </p>
          <Link to="/" className="button">
            Zurück zur Karte
          </Link>
        </section>
      </article>
    )

  return (
    <article className="details">
      <title>Slipanlage vorschlagen · Slipways</title>
      <Link to="/" className="back">
        <ArrowLeft size={16} /> Alle Slipanlagen
      </Link>
      <header className="details-head">
        <span className="details-icon">
          <MapPin size={26} />
        </span>
        <div>
          <h1>Slipanlage fehlt?</h1>
          <p className="muted">
            Setze den Pin auf die Rampe und erzähl kurz, was du weißt. Alles wird vor der Veröffentlichung geprüft.
          </p>
        </div>
      </header>

      <div className="details-grid">
        <form className="details-side suggest-form panel" onSubmit={(e) => submit(e)}>
          <label className="field">
            <span>Name</span>
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="z. B. Slipanlage am Yachthafen Musterstadt"
              maxLength={120}
              required
            />
          </label>
          <p className="muted small">
            {moved
              ? `Position: ${position.latitude.toFixed(5)}, ${position.longitude.toFixed(5)}`
              : 'Tippe auf die Karte oder zieh den Pin an die richtige Stelle.'}
          </p>
          {['fee', 'surface', 'access'].map((field) => (
            <label key={field} className="field">
              <span>{FIELDS[field].label}</span>
              <select value={fields[field] ?? ''} onChange={(e) => set(field, e.target.value)}>
                <option value="">weiß nicht</option>
                {FIELDS[field].options!.map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </label>
          ))}
          <fieldset className="field facts-picker">
            <legend>Was gibt es dort?</legend>
            {FACTS.map((field) => (
              <label key={field} className="check">
                <input
                  type="checkbox"
                  checked={fields[field] === 'yes'}
                  onChange={(e) => set(field, e.target.checked ? 'yes' : '')}
                />
                {FIELDS[field].label}
              </label>
            ))}
          </fieldset>
          <label className="field">
            <span>Woher weißt du das? (freiwillig)</span>
            <textarea value={text} onChange={(e) => setText(e.target.value)} maxLength={500} rows={2} />
          </label>

          <label className="visually-hidden" aria-hidden="true">
            Homepage
            <input tabIndex={-1} autoComplete="off" value={homepage} onChange={(e) => setHomepage(e.target.value)} />
          </label>

          <label className="consent">
            <input type="checkbox" checked={consent} onChange={(e) => setConsent(e.target.checked)} />
            <span>
              Ich kenne die Slipanlage selbst und habe die Angaben nicht aus anderen Verzeichnissen abgeschrieben.
              Slipways darf sie veröffentlichen. <Link to="/support#mitmachen">Mehr dazu</Link>
            </span>
          </label>

          {nearby && (
            <div className="nearby">
              <p>
                <strong>Ist es eine von diesen?</strong> In der Nähe kennen wir schon:
              </p>
              <ul>
                {nearby.map((s) => (
                  <li key={s.id}>
                    <Link to={`/slipways/${s.id}`}>{s.name}</Link> <span className="muted">({s.distance} m)</span>
                  </li>
                ))}
              </ul>
              <button type="button" className="button" disabled={!canSend} onClick={(e) => submit(e, true)}>
                Nein, trotzdem neu eintragen
              </button>
            </div>
          )}
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
            {!nearby && (
              <button type="submit" className="button button-primary" disabled={!canSend}>
                <Send size={16} /> {sending ? 'Wird gesendet…' : 'Vorschlag senden'}
              </button>
            )}
          </div>
        </form>
        <div className="details-map">
          <LazyPickMap position={position} zoom={initial.zoom} onChange={move} />
        </div>
      </div>
    </article>
  )
}

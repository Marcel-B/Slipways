import { Check, ExternalLink, Flag, Lightbulb, ListChecks, Pencil, Plus, X } from 'lucide-react'
import type { ReactNode } from 'react'
import type { PlaceInfo } from '../api/types'
import { FACTS, FIELDS, fromUsers, monthYear, osmEditUrl, WANTED } from './fields'
import type { SuggestMode, SuggestPlace } from './SuggestDialog'

type Values = Record<string, string | null | undefined>
/** A small pencil next to a value that opens the form for that one field. */
export function EditButton({ field, onEdit }: { field: string; onEdit: (mode: SuggestMode) => void }) {
  const label = FIELDS[field]?.label ?? field
  return (
    <button
      type="button"
      className="edit-button"
      onClick={() => onEdit({ kind: 'edit', fields: [field], title: `${label} korrigieren` })}
      aria-label={`${label} korrigieren`}
      title={`${label} korrigieren`}
    >
      <Pencil size={14} />
    </button>
  )
}

/** A value with where it comes from when users sent it in, and a note when this browser sent one that waits. */
export function Contributed({ info, pending, children }: { info?: PlaceInfo; pending?: boolean; children: ReactNode }) {
  return (
    <>
      {children}
      {info && <small className="fact-source">von Nutzern ergänzt, Stand {monthYear(info.retrieved)}</small>}
      {pending && <small className="fact-source pending">Dein Vorschlag wird geprüft</small>}
    </>
  )
}

interface ContributeProps {
  place: SuggestPlace
  /** The values the place shows now, from any source; empty ones count as missing. */
  values: Values
  infos: PlaceInfo[]
  /** Like "node/123" for places from or linked to OpenStreetMap. */
  osmElement: string | null
  /** Fields this browser sent values for that still wait for moderation. */
  sent: Set<string>
  onSuggest: (mode: SuggestMode) => void
}

/** Missing details, facts, tips and the ways to contribute, below the facts of a place. */
export function Contribute({ place, values, infos, osmElement, sent, onSuggest }: ContributeProps) {
  const missing = WANTED[place.type].filter((f) => !values[f] && !sent.has(f))
  const tips = infos.filter((i) => i.field === 'tip' && i.source === 'user')
  const facts = place.type === 'slipway' ? FACTS.map((f) => [f, fromUsers(infos, f)] as const).filter(([, i]) => i) : []

  return (
    <>
      {place.type === 'slipway' && (
        <section className="panel contribute-facts">
          <h2>Was gibt es dort?</h2>
          {facts.length > 0 ? (
            <ul className="fact-list">
              {facts.map(([field, info]) => (
                <li key={field} className={info!.value === 'yes' ? 'yes' : 'no'}>
                  {info!.value === 'yes' ? <Check size={16} /> : <X size={16} />}
                  {info!.value === 'yes' ? FIELDS[field].label : `${FIELDS[field].label}: nein`}
                </li>
              ))}
            </ul>
          ) : (
            <p className="muted">Noch keine Angaben.</p>
          )}
          {facts.length > 0 && (
            <small className="fact-source">von Nutzern ergänzt, Stand {monthYear(facts[0][1]!.retrieved)}</small>
          )}
          <button
            type="button"
            className="button button-small"
            onClick={() => onSuggest({ kind: 'edit', fields: FACTS, title: 'Was gibt es dort?' })}
          >
            <ListChecks size={16} /> Angaben ankreuzen
          </button>
        </section>
      )}

      {missing.length > 0 && (
        <section className="panel missing">
          <h2>Fehlt noch</h2>
          <p className="muted">Weißt du mehr? Ergänze es, es wird vor der Veröffentlichung geprüft.</p>
          <ul className="missing-list">
            {missing.map((field) => (
              <li key={field}>
                <button
                  type="button"
                  className="button button-small"
                  onClick={() => onSuggest({ kind: 'edit', fields: [field], title: `${FIELDS[field].label} ergänzen` })}
                >
                  <Plus size={16} /> {FIELDS[field].label}
                </button>
              </li>
            ))}
          </ul>
        </section>
      )}

      {tips.length > 0 && (
        <section className="panel">
          <h2>Tipps</h2>
          <ul className="tips">
            {tips.map((tip) => (
              <li key={`${tip.retrieved}-${tip.value}`}>
                <p>{tip.value}</p>
                <small className="fact-source">{monthYear(tip.retrieved)}</small>
              </li>
            ))}
          </ul>
        </section>
      )}

      <div className="contribute-actions">
        <button type="button" className="button button-small" onClick={() => onSuggest({ kind: 'tip' })}>
          <Lightbulb size={16} /> Tipp geben
        </button>
        <button type="button" className="button button-small" onClick={() => onSuggest({ kind: 'report' })}>
          <Flag size={16} /> Etwas stimmt nicht
        </button>
        {osmElement && (
          <a className="button button-small" href={osmEditUrl(osmElement)} target="_blank" rel="noreferrer">
            <ExternalLink size={16} /> In OpenStreetMap bearbeiten
          </a>
        )}
      </div>
    </>
  )
}

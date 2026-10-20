import { useQuery } from '@tanstack/react-query'
import {
  Anchor,
  BadgeCheck,
  Building2,
  Clock,
  DoorOpen,
  Globe,
  Layers,
  ArrowLeft,
  Coins,
  Crosshair,
  ExternalLink,
  MapPin,
  Navigation,
  Pencil,
  Sailboat,
  Sparkles,
  ThumbsDown,
  ThumbsUp,
  Waves,
  type LucideIcon,
} from 'lucide-react'
import { Fragment } from 'react'
import { Link, useParams } from 'react-router'
import { api, ApiError } from '../api/client'
import type { PlaceInfo, SlipwayDetails } from '../api/types'
import { Contribute, Contributed, EditButton } from '../contribute/Contribute'
import { useSuggest } from '../contribute/useSuggest'
import { fromUsers } from '../contribute/fields'
import { latest, sentFields } from '../contribute/storage'
import { LazyMarkerMap } from '../components/LazyMarkerMap'
import { slipwayMarker } from '../components/mapMarkers'
import { NearestGauge } from '../components/NearestGauge'
import { accessLabel, feeLabel, osmUrl, surfaceLabel } from '../components/osmTags'
import { placeTitle } from './pageTitle'

const currency = new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' })

/** Costs win over the fee; values from users win over the slipway's own, which for imports come from OpenStreetMap. */
function costs(value: number | null, fee: string | null): { label: string; tone: string } {
  if (value === 0 || (value === null && fee === 'no')) return { label: 'kostenlos', tone: 'free' }
  if (value !== null) return { label: currency.format(value), tone: 'paid' }
  if (fee) return { label: feeLabel(fee), tone: 'paid' }
  return { label: 'Kosten unbekannt', tone: 'neutral' }
}

export function SlipwayDetailsPage() {
  const { id = '' } = useParams()
  const { data: slipway, error, isPending } = useQuery({
    queryKey: ['slipway', id],
    queryFn: ({ signal }) => api.slipway(id, signal),
  })

  if (isPending) return <p className="muted">Lade…</p>
  if (error instanceof ApiError && error.status === 404) return <p>Diese Slipanlage gibt es nicht.</p>
  if (error || !slipway) return <p className="error">Slipanlage konnte nicht geladen werden.</p>
  return <SlipwayDetailsView slipway={slipway} />
}

function SlipwayDetailsView({ slipway }: { slipway: SlipwayDetails }) {
  const user = (field: string): PlaceInfo | undefined => fromUsers(slipway.infos, field)
  const own: Record<string, string | null> = {
    fee: slipway.fee,
    costs: slipway.costs === null ? null : String(slipway.costs),
    operator: slipway.operator,
    access: slipway.access,
    opening_hours: slipway.openingHours,
    surface: slipway.surface,
  }
  const values = Object.fromEntries(Object.entries(own).map(([f, v]) => [f, user(f)?.value ?? v]))
  const place = { type: 'slipway' as const, id: slipway.id, name: slipway.name, source: slipway.source }
  const suggest = useSuggest(place, values)
  const sent = sentFields('slipway', slipway.id, latest(slipway.updated, ...slipway.infos.map((i) => i.retrieved)))

  const address = [slipway.street, [slipway.postalCode, slipway.city].filter(Boolean).join(' '), slipway.country]
    .filter(Boolean)
    .join(', ')
  const cost = costs(values.costs === null ? null : Number(values.costs), values.fee)
  const facts: [LucideIcon, string, string, string | null][] = [
    [Building2, 'Betreiber', 'operator', values.operator],
    [DoorOpen, 'Zugang', 'access', values.access && accessLabel(values.access)],
    [Clock, 'Öffnungszeiten', 'opening_hours', values.opening_hours],
    [Layers, 'Untergrund', 'surface', values.surface && surfaceLabel(values.surface)],
  ]
  const position = `${slipway.latitude},${slipway.longitude}`

  return (
    <article className="details">
      <title>{`${placeTitle(slipway.name, slipway.city)} · Slipways`}</title>
      <Link to="/" className="back">
        <ArrowLeft size={16} /> Alle Slipanlagen
      </Link>
      <header className="details-head">
        <span className="details-icon">
          <Sailboat size={26} />
        </span>
        <div>
          <h1>{slipway.name}</h1>
          <div className="chips">
            {slipway.water && (
              <Link to={`/waters/${slipway.water.id}`} className="chip chip-water">
                <Waves size={14} /> {slipway.water.name}
              </Link>
            )}
            <span className={`chip chip-${cost.tone}`}>
              <Coins size={14} /> {cost.label}
            </span>
            {(values.fee || values.costs) && (
              <button
                type="button"
                className="edit-button"
                onClick={() => suggest.open({ kind: 'edit', fields: ['fee', 'costs'], title: 'Kosten korrigieren' })}
                aria-label="Kosten korrigieren"
                title="Kosten korrigieren"
              >
                <Pencil size={14} />
              </button>
            )}
          </div>
        </div>
      </header>

      <div className="details-grid">
        <div className="details-side">
          <dl className="facts panel">
            {address && (
              <>
                <dt>
                  <MapPin size={16} /> Adresse
                </dt>
                <dd>{address}</dd>
              </>
            )}
            {facts
              .filter(([, , , value]) => value)
              .map(([Icon, label, field, value]) => (
                <Fragment key={label}>
                  <dt>
                    <Icon size={16} /> {label}
                  </dt>
                  <dd>
                    <span className="fact-value">
                      <span>
                        <Contributed info={user(field)} pending={sent.has(field)}>
                          {value}
                        </Contributed>
                      </span>
                      <EditButton field={field} onEdit={suggest.open} />
                    </span>
                  </dd>
                </Fragment>
              ))}
            {slipway.url && (
              <>
                <dt>
                  <Globe size={16} /> Webseite
                </dt>
                <dd>
                  <a href={slipway.url} target="_blank" rel="noreferrer">
                    {slipway.url}
                  </a>
                </dd>
              </>
            )}
            {slipway.marina && (
              <>
                <dt>
                  <Anchor size={16} /> Marina
                </dt>
                <dd>
                  <Link to={`/marinas/${slipway.marina.id}`}>{slipway.marina.name}</Link>
                </dd>
              </>
            )}
            {slipway.extras.length > 0 && (
              <>
                <dt>
                  <Sparkles size={16} /> Extras
                </dt>
                <dd>{slipway.extras.join(', ')}</dd>
              </>
            )}
            <dt>
              <Crosshair size={16} /> Koordinaten
            </dt>
            <dd>
              {slipway.latitude.toFixed(5)}, {slipway.longitude.toFixed(5)}
            </dd>
            <dt>
              <BadgeCheck size={16} /> Quelle
            </dt>
            <dd>
              {slipway.source === 'osm' ? '© OpenStreetMap-Mitwirkende (ODbL)' : 'eigene Erfassung'}
              {slipway.osmElement && (
                <>
                  ,{' '}
                  <a href={osmUrl(slipway.osmElement)} target="_blank" rel="noreferrer">
                    in OpenStreetMap ansehen
                  </a>
                </>
              )}
            </dd>
          </dl>

          {slipway.comment && <p className="comment">{slipway.comment}</p>}
          {(slipway.pro || slipway.contra) && (
            <ul className="pro-contra">
              {slipway.pro && (
                <li className="pro">
                  <ThumbsUp size={16} /> {slipway.pro}
                </li>
              )}
              {slipway.contra && (
                <li className="contra">
                  <ThumbsDown size={16} /> {slipway.contra}
                </li>
              )}
            </ul>
          )}

          <Contribute
            place={place}
            values={{ ...values, fee: values.fee ?? values.costs }}
            infos={slipway.infos}
            osmElement={slipway.osmElement}
            sent={sent}
            onSuggest={suggest.open}
          />

          {slipway.water && <NearestGauge position={slipway} waterId={slipway.water.id} />}

          <div className="actions">
            <a
              className="button button-primary"
              href={`https://www.google.com/maps/dir/?api=1&destination=${position}`}
              target="_blank"
              rel="noreferrer"
            >
              <Navigation size={16} /> Route planen
            </a>
            <a
              className="button"
              href={`https://www.openstreetmap.org/?mlat=${slipway.latitude}&mlon=${slipway.longitude}#map=16/${slipway.latitude}/${slipway.longitude}`}
              target="_blank"
              rel="noreferrer"
            >
              <ExternalLink size={16} /> In OpenStreetMap
            </a>
          </div>
        </div>
        <div className="details-map">
          <LazyMarkerMap markers={[slipwayMarker(slipway)]} center={[slipway.latitude, slipway.longitude]} zoom={14} introZoom />
        </div>
      </div>
      {suggest.dialog}
    </article>
  )
}

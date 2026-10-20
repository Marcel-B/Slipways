import { useQuery } from '@tanstack/react-query'
import {
  Anchor,
  ArrowLeft,
  BadgeCheck,
  Building2,
  ChevronRight,
  Clock,
  Coins,
  Crosshair,
  DoorOpen,
  ExternalLink,
  Globe,
  Mail,
  MapPin,
  Navigation,
  Phone,
  Sailboat,
  Waves,
  type LucideIcon,
} from 'lucide-react'
import { Fragment, type ReactNode } from 'react'
import { Link, useParams } from 'react-router'
import { api, ApiError } from '../api/client'
import type { MarinaDetails, PlaceInfo } from '../api/types'
import { Contribute, Contributed, EditButton } from '../contribute/Contribute'
import { useSuggest } from '../contribute/useSuggest'
import { fromUsers } from '../contribute/fields'
import { latest, sentFields } from '../contribute/storage'
import { LazyMarkerMap } from '../components/LazyMarkerMap'
import { marinaMarker, slipwayMarker } from '../components/mapMarkers'
import { NearestGauge } from '../components/NearestGauge'
import { accessLabel, feeLabel, osmUrl } from '../components/osmTags'
import { count } from './count'

export function MarinaDetailsPage() {
  const { id = '' } = useParams()
  const {
    data: marina,
    error,
    isPending,
  } = useQuery({
    queryKey: ['marina', id],
    queryFn: ({ signal }) => api.marina(id, signal),
  })

  if (isPending) return <p className="muted">Lade…</p>
  if (error instanceof ApiError && error.status === 404) return <p>Diese Marina gibt es nicht.</p>
  if (error || !marina) return <p className="error">Marina konnte nicht geladen werden.</p>
  return <MarinaDetailsView marina={marina} />
}

function MarinaDetailsView({ marina }: { marina: MarinaDetails }) {
  // Values from users win; details from the marina's website only fill gaps after our own and OpenStreetMap's.
  const fromWebsite = (field: string) => marina.infos.find((i) => i.field === field && i.source === 'website')
  const pick = (field: string, own: string | null): string | PlaceInfo | null =>
    fromUsers(marina.infos, field) ?? own ?? fromWebsite(field) ?? null
  const openingHours = pick('opening_hours', marina.openingHours)
  const phone = pick('phone', marina.phone)
  const email = pick('email', marina.email)
  const operator = pick('operator', marina.operator)
  const access = pick('access', marina.access)
  const fee = pick('fee', marina.fee)
  const text = (value: string | PlaceInfo | null) => (typeof value === 'string' ? value : (value?.value ?? null))
  const values = {
    opening_hours: text(openingHours),
    phone: text(phone),
    email: text(email),
    operator: text(operator),
    access: text(access),
    fee: text(fee),
  }
  const place = { type: 'marina' as const, id: marina.id, name: marina.name, source: marina.source }
  const suggest = useSuggest(place, values)
  const sent = sentFields('marina', marina.id, latest(...marina.infos.filter((i) => i.source === 'user').map((i) => i.retrieved)))
  const edit = (field: string) => <EditButton field={field} onEdit={suggest.open} />
  const sourced = (field: string, value: string | PlaceInfo, render: (v: string) => ReactNode = (v) => v) => (
    <span className="fact-value">
      <span>
        <Sourced value={value} pending={sent.has(field)} render={render} />
      </span>
      {edit(field)}
    </span>
  )

  const address = [marina.street, [marina.postalCode, marina.city].filter(Boolean).join(' ')].filter(Boolean).join(', ')
  const facts: [LucideIcon, string, ReactNode][] = [
    [MapPin, 'Adresse', address || null],
    [Building2, 'Betreiber', operator && sourced('operator', operator)],
    [DoorOpen, 'Zugang', access && sourced('access', access, accessLabel)],
    [Clock, 'Öffnungszeiten', openingHours && sourced('opening_hours', openingHours)],
  ]
  const position = `${marina.latitude},${marina.longitude}`

  return (
    <article className="details">
      <Link to="/marinas" className="back">
        <ArrowLeft size={16} /> Alle Marinas
      </Link>
      <header className="details-head">
        <span className="details-icon">
          <Anchor size={26} />
        </span>
        <div>
          <h1>{marina.name}</h1>
          <div className="chips">
            {marina.water && (
              <Link to={`/waters/${marina.water.id}`} className="chip chip-water">
                <Waves size={14} /> {marina.water.name}
              </Link>
            )}
            {values.fee && (
              <span className={`chip chip-${values.fee === 'no' ? 'free' : 'paid'}`}>
                <Coins size={14} /> {feeLabel(values.fee)}
              </span>
            )}
            {values.fee && edit('fee')}
          </div>
        </div>
      </header>

      <div className="details-grid">
        <div className="details-side">
          <dl className="facts panel">
            {facts
              .filter(([, , value]) => value)
              .map(([Icon, label, value]) => (
                <Fragment key={label}>
                  <dt>
                    <Icon size={16} /> {label}
                  </dt>
                  <dd>{value}</dd>
                </Fragment>
              ))}
            {phone && (
              <>
                <dt>
                  <Phone size={16} /> Telefon
                </dt>
                <dd>{sourced('phone', phone, (v) => <a href={`tel:${v.replace(/[^\d+]/g, '')}`}>{v}</a>)}</dd>
              </>
            )}
            {email && (
              <>
                <dt>
                  <Mail size={16} /> E-Mail
                </dt>
                <dd>{sourced('email', email, (v) => <a href={`mailto:${v}`}>{v}</a>)}</dd>
              </>
            )}
            {marina.url && (
              <>
                <dt>
                  <Globe size={16} /> Webseite
                </dt>
                <dd>
                  <a href={marina.url} target="_blank" rel="noreferrer">
                    {marina.url}
                  </a>
                </dd>
              </>
            )}
            <dt>
              <Crosshair size={16} /> Koordinaten
            </dt>
            <dd>
              {marina.latitude.toFixed(5)}, {marina.longitude.toFixed(5)}
            </dd>
            <dt>
              <BadgeCheck size={16} /> Quelle
            </dt>
            <dd>
              {marina.source === 'osm' ? '© OpenStreetMap-Mitwirkende (ODbL)' : 'eigene Erfassung'}
              {marina.osmElement && (
                <>
                  ,{' '}
                  <a href={osmUrl(marina.osmElement)} target="_blank" rel="noreferrer">
                    in OpenStreetMap ansehen
                  </a>
                </>
              )}
            </dd>
          </dl>

          {marina.slipways.length > 0 && (
            <section className="panel">
              <h2>{count(marina.slipways.length, 'Slipanlage', 'Slipanlagen')}</h2>
              <ul className="item-list">
                {marina.slipways.map((s) => (
                  <li key={s.id}>
                    <Link to={`/slipways/${s.id}`} className="item">
                      <span className="item-icon">
                        <Sailboat size={18} />
                      </span>
                      <span className="item-text">
                        <strong>{s.name}</strong>
                        <span className="muted">{[s.postalCode, s.city].filter(Boolean).join(' ')}</span>
                      </span>
                      <ChevronRight size={18} className="item-chevron" />
                    </Link>
                  </li>
                ))}
              </ul>
            </section>
          )}

          <Contribute
            place={place}
            values={values}
            infos={marina.infos}
            osmElement={marina.osmElement}
            sent={sent}
            onSuggest={suggest.open}
          />

          {marina.water && <NearestGauge position={marina} waterId={marina.water.id} />}

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
              href={`https://www.openstreetmap.org/?mlat=${marina.latitude}&mlon=${marina.longitude}#map=16/${marina.latitude}/${marina.longitude}`}
              target="_blank"
              rel="noreferrer"
            >
              <ExternalLink size={16} /> In OpenStreetMap
            </a>
          </div>
        </div>
        <div className="details-map">
          <LazyMarkerMap
            markers={[marinaMarker(marina), ...marina.slipways.map(slipwayMarker)]}
            center={[marina.latitude, marina.longitude]}
            zoom={14}
            highlightedId={marina.id}
            introZoom
          />
        </div>
      </div>
      {suggest.dialog}
    </article>
  )
}

/** A value of the marina itself, or one from users or its website with where and when it was read. */
function Sourced({
  value,
  pending,
  render,
}: {
  value: string | PlaceInfo
  pending: boolean
  render: (value: string) => ReactNode
}) {
  if (typeof value === 'string' || value.source === 'user')
    return (
      <Contributed info={typeof value === 'string' ? undefined : value} pending={pending}>
        {render(typeof value === 'string' ? value : value.value)}
      </Contributed>
    )
  const date = new Date(value.retrieved).toLocaleDateString('de-DE')
  return (
    <>
      {render(value.value)}
      <small className="fact-source">
        {value.sourceUrl ? (
          <a href={value.sourceUrl} target="_blank" rel="noreferrer">
            laut Webseite
          </a>
        ) : (
          'laut Webseite'
        )}
        , Stand {date}
      </small>
    </>
  )
}

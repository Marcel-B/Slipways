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
import { Fragment } from 'react'
import { Link, useParams } from 'react-router'
import { api, ApiError } from '../api/client'
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

  const address = [marina.street, [marina.postalCode, marina.city].filter(Boolean).join(' ')].filter(Boolean).join(', ')
  const facts: [LucideIcon, string, string | null][] = [
    [MapPin, 'Adresse', address || null],
    [Building2, 'Betreiber', marina.operator],
    [DoorOpen, 'Zugang', marina.access && accessLabel(marina.access)],
    [Clock, 'Öffnungszeiten', marina.openingHours],
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
            {marina.fee && (
              <span className={`chip chip-${marina.fee === 'no' ? 'free' : 'paid'}`}>
                <Coins size={14} /> {feeLabel(marina.fee)}
              </span>
            )}
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
            {marina.phone && (
              <>
                <dt>
                  <Phone size={16} /> Telefon
                </dt>
                <dd>
                  <a href={`tel:${marina.phone.replace(/[^\d+]/g, '')}`}>{marina.phone}</a>
                </dd>
              </>
            )}
            {marina.email && (
              <>
                <dt>
                  <Mail size={16} /> E-Mail
                </dt>
                <dd>
                  <a href={`mailto:${marina.email}`}>{marina.email}</a>
                </dd>
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
          />
        </div>
      </div>
    </article>
  )
}

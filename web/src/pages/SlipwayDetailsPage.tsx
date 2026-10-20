import { useQuery } from '@tanstack/react-query'
import {
  Anchor,
  ArrowLeft,
  Coins,
  Crosshair,
  ExternalLink,
  MapPin,
  Navigation,
  Ruler,
  Sailboat,
  Sparkles,
  ThumbsDown,
  ThumbsUp,
  Waves,
} from 'lucide-react'
import { Link, useParams } from 'react-router'
import { api, ApiError } from '../api/client'
import type { SlipwayDetails } from '../api/types'
import { LazyMarkerMap } from '../components/LazyMarkerMap'
import { slipwayMarker } from '../components/mapMarkers'
import { formatMeasuredAt } from '../components/waterLevel'
import { WaterLevelText } from '../components/WaterLevelText'
import { distanceKm } from './distance'

const currency = new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' })
const kilometres = new Intl.NumberFormat('de-DE', { maximumFractionDigits: 1 })

function costs(value: number | null): { label: string; tone: string } {
  if (value === null) return { label: 'Kosten unbekannt', tone: 'neutral' }
  if (value === 0) return { label: 'kostenlos', tone: 'free' }
  return { label: currency.format(value), tone: 'paid' }
}

export function SlipwayDetailsPage() {
  const { id = '' } = useParams()
  const {
    data: slipway,
    error,
    isPending,
  } = useQuery({
    queryKey: ['slipway', id],
    queryFn: ({ signal }) => api.slipway(id, signal),
  })

  if (isPending) return <p className="muted">Lade…</p>
  if (error instanceof ApiError && error.status === 404) return <p>Diese Slipanlage gibt es nicht.</p>
  if (error || !slipway) return <p className="error">Slipanlage konnte nicht geladen werden.</p>

  const address = [slipway.street, [slipway.postalCode, slipway.city].filter(Boolean).join(' '), slipway.country]
    .filter(Boolean)
    .join(', ')
  const cost = costs(slipway.costs)
  const position = `${slipway.latitude},${slipway.longitude}`

  return (
    <article className="details">
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
            <Link to={`/waters/${slipway.water.id}`} className="chip chip-water">
              <Waves size={14} /> {slipway.water.name}
            </Link>
            <span className={`chip chip-${cost.tone}`}>
              <Coins size={14} /> {cost.label}
            </span>
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
            {slipway.marina && (
              <>
                <dt>
                  <Anchor size={16} /> Marina
                </dt>
                <dd>
                  {slipway.marina.url ? (
                    <a href={slipway.marina.url} target="_blank" rel="noreferrer">
                      {slipway.marina.name}
                    </a>
                  ) : (
                    slipway.marina.name
                  )}
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

          <NearestGauge slipway={slipway} />

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
          <LazyMarkerMap markers={[slipwayMarker(slipway)]} center={[slipway.latitude, slipway.longitude]} zoom={14} />
        </div>
      </div>
    </article>
  )
}

/** The gauge with a current reading closest to the slipway, from the water the slipway is on. */
function NearestGauge({ slipway }: { slipway: SlipwayDetails }) {
  // Same query as the water's page, so going there afterwards needs no new request.
  const { data: water } = useQuery({
    queryKey: ['water', slipway.water.id],
    queryFn: ({ signal }) => api.water(slipway.water.id, signal),
  })

  const nearest = (water?.stations ?? [])
    .flatMap((s) => (s.waterLevel ? [{ station: s, level: s.waterLevel, km: distanceKm(slipway, s) }] : []))
    .sort((a, b) => a.km - b.km)[0]
  if (!nearest) return null

  const { station, level, km } = nearest
  return (
    <section className="gauge-card panel" aria-label="Nächster Pegel">
      <span className="gauge-icon">
        <Ruler size={20} />
      </span>
      <div>
        <p className="gauge-title">
          Nächster Pegel: <Link to={`/waters/${slipway.water.id}`}>{station.name}</Link>
        </p>
        <p className="gauge-value">
          <WaterLevelText level={level} />
        </p>
        <p className="muted gauge-meta">
          {kilometres.format(km)} km entfernt · gemessen {formatMeasuredAt(level)}
        </p>
      </div>
    </section>
  )
}

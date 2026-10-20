import { useQuery } from '@tanstack/react-query'
import { Fragment } from 'react'
import { Link, useParams } from 'react-router'
import { api, ApiError } from '../api/client'
import { LazyMarkerMap } from '../components/LazyMarkerMap'
import { slipwayMarker } from '../components/mapMarkers'
import { accessLabel, feeLabel, osmUrl, surfaceLabel } from '../components/osmTags'

const currency = new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' })

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

  const costs =
    slipway.costs !== null
      ? slipway.costs === 0
        ? 'kostenlos'
        : currency.format(slipway.costs)
      : slipway.fee
        ? feeLabel(slipway.fee)
        : 'unbekannt'
  const facts: [string, string | null][] = [
    ['Betreiber', slipway.operator],
    ['Zugang', slipway.access && accessLabel(slipway.access)],
    ['Öffnungszeiten', slipway.openingHours],
    ['Untergrund', slipway.surface && surfaceLabel(slipway.surface)],
  ]

  const address = [slipway.street, [slipway.postalCode, slipway.city].filter(Boolean).join(' '), slipway.country]
    .filter(Boolean)
    .join(', ')

  return (
    <article className="details">
      <p>
        <Link to="/">← Alle Slipanlagen</Link>
      </p>
      <h1>{slipway.name}</h1>
      <dl className="facts">
        {slipway.water && (
          <>
            <dt>Gewässer</dt>
            <dd>
              <Link to={`/waters/${slipway.water.id}`}>{slipway.water.name}</Link>
            </dd>
          </>
        )}
        {address && (
          <>
            <dt>Adresse</dt>
            <dd>{address}</dd>
          </>
        )}
        <dt>Kosten</dt>
        <dd>{costs}</dd>
        {facts
          .filter(([, value]) => value)
          .map(([label, value]) => (
            <Fragment key={label}>
              <dt>{label}</dt>
              <dd>{value}</dd>
            </Fragment>
          ))}
        {slipway.url && (
          <>
            <dt>Webseite</dt>
            <dd>
              <a href={slipway.url} target="_blank" rel="noreferrer">
                {slipway.url}
              </a>
            </dd>
          </>
        )}
        {slipway.marina && (
          <>
            <dt>Marina</dt>
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
            <dt>Extras</dt>
            <dd>{slipway.extras.join(', ')}</dd>
          </>
        )}
        <dt>Koordinaten</dt>
        <dd>
          {slipway.latitude.toFixed(5)}, {slipway.longitude.toFixed(5)}
        </dd>
        <dt>Quelle</dt>
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
      <LazyMarkerMap markers={[slipwayMarker(slipway)]} center={[slipway.latitude, slipway.longitude]} zoom={14} />
    </article>
  )
}

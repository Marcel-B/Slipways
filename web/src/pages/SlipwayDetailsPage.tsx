import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router'
import { api, ApiError } from '../api/client'
import { LazySlipwayMap } from '../components/LazySlipwayMap'

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
        <dt>Gewässer</dt>
        <dd>{slipway.water.name}</dd>
        {address && (
          <>
            <dt>Adresse</dt>
            <dd>{address}</dd>
          </>
        )}
        <dt>Kosten</dt>
        <dd>{slipway.costs === null ? 'unbekannt' : slipway.costs === 0 ? 'kostenlos' : currency.format(slipway.costs)}</dd>
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
      </dl>
      {slipway.comment && <p className="comment">{slipway.comment}</p>}
      <LazySlipwayMap slipways={[slipway]} center={[slipway.latitude, slipway.longitude]} zoom={14} />
    </article>
  )
}

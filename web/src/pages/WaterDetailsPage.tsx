import { useQuery } from '@tanstack/react-query'
import { Anchor, ArrowLeft, ChevronRight, Ruler, Sailboat, Waves } from 'lucide-react'
import { Link, useParams } from 'react-router'
import { api, ApiError } from '../api/client'
import type { WaterDetails } from '../api/types'
import { LazyMarkerMap } from '../components/LazyMarkerMap'
import { markerColors, shapeStyle, slipwayMarker, type MapMarker, type MarkerKind } from '../components/mapMarkers'
import { formatMeasuredAt, formatWaterLevel } from '../components/waterLevel'
import { WaterLevelText } from '../components/WaterLevelText'
import { count } from './count'

const kilometres = new Intl.NumberFormat('de-DE', { maximumFractionDigits: 3 })

const legend: [MarkerKind, string][] = [
  ['slipway', 'Slipanlage'],
  ['station', 'Pegel'],
  ['marina', 'Marina'],
]

function markers(water: WaterDetails): MapMarker[] {
  return [
    ...water.slipways.map(slipwayMarker),
    ...water.stations.map(
      (s): MapMarker => ({
        id: s.id,
        kind: 'station',
        latitude: s.latitude,
        longitude: s.longitude,
        title: `Pegel ${s.name}`,
        subtitle: s.waterLevel ? formatWaterLevel(s.waterLevel) : 'kein aktueller Wasserstand',
      }),
    ),
    ...water.marinas.map(
      (m): MapMarker => ({
        id: m.id,
        kind: 'marina',
        latitude: m.latitude,
        longitude: m.longitude,
        title: m.name,
        subtitle: 'Marina',
        to: `/marinas/${m.id}`,
      }),
    ),
  ]
}

export function WaterDetailsPage() {
  const { id = '' } = useParams()
  const {
    data: water,
    error,
    isPending,
  } = useQuery({
    queryKey: ['water', id],
    queryFn: ({ signal }) => api.water(id, signal),
  })

  if (isPending) return <p className="muted">Lade…</p>
  if (error instanceof ApiError && error.status === 404) return <p>Dieses Gewässer gibt es nicht.</p>
  if (error || !water) return <p className="error">Gewässer konnte nicht geladen werden.</p>

  const mapMarkers = markers(water)

  return (
    <article className="details">
      <title>{`${water.name} · Slipways`}</title>
      <Link to="/waters" className="back">
        <ArrowLeft size={16} /> Alle Gewässer
      </Link>
      <header className="details-head">
        <span className="details-icon details-icon-water">
          <Waves size={26} />
        </span>
        <div>
          <h1>{water.name}</h1>
          {water.shortname && water.shortname.toLowerCase() !== water.name.toLowerCase() && (
            <p className="muted details-sub">Kurzname {water.shortname}</p>
          )}
        </div>
      </header>
      <div className={water.bounds ? 'details-grid details-grid-water' : undefined}>
        <div className="details-side">
          <dl className="stats">
            <div>
              <dt>
                <Sailboat size={16} /> Slipanlagen
              </dt>
              <dd>{water.slipways.length}</dd>
            </div>
            <div>
              <dt>
                <Ruler size={16} /> Pegel
              </dt>
              <dd>{water.stations.length}</dd>
            </div>
            <div>
              <dt>
                <Anchor size={16} /> Marinas
              </dt>
              <dd>{water.marinas.length}</dd>
            </div>
          </dl>

          {!water.bounds && <p className="muted">Zu diesem Gewässer sind noch keine Orte bekannt.</p>}
          {water.slipways.length > 0 && (
            <section className="panel">
              <h2>{count(water.slipways.length, 'Slipanlage', 'Slipanlagen')}</h2>
              <ul className="item-list">
                {water.slipways.map((s) => (
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

          {water.stations.length > 0 && (
            <section className="panel">
              <h2>{count(water.stations.length, 'Pegel', 'Pegel')}</h2>
              <div className="table-scroll">
                <table className="table">
                  <thead>
                    <tr>
                      <th>Pegel</th>
                      <th>km</th>
                      <th>Wasserstand</th>
                      <th>Gemessen</th>
                    </tr>
                  </thead>
                  <tbody>
                    {water.stations.map((s) => (
                      <tr key={s.id}>
                        <td>{s.name}</td>
                        <td>{kilometres.format(s.km)}</td>
                        <td>{s.waterLevel ? <WaterLevelText level={s.waterLevel} /> : <span className="muted">–</span>}</td>
                        <td>{s.waterLevel ? formatMeasuredAt(s.waterLevel) : <span className="muted">–</span>}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>
          )}

          {water.marinas.length > 0 && (
            <section className="panel">
              <h2>{count(water.marinas.length, 'Marina', 'Marinas')}</h2>
              <ul className="item-list">
                {water.marinas.map((m) => (
                  <li key={m.id}>
                    <Link to={`/marinas/${m.id}`} className="item">
                      <span className="item-icon item-icon-marina">
                        <Anchor size={18} />
                      </span>
                      <span className="item-text">
                        <strong>{m.name}</strong>
                        <span className="muted">
                          {[m.street, [m.postalCode, m.city].filter(Boolean).join(' ')].filter(Boolean).join(', ')}
                        </span>
                      </span>
                      <ChevronRight size={18} className="item-chevron" />
                    </Link>
                  </li>
                ))}
              </ul>
            </section>
          )}
        </div>
        {water.bounds && (
          <div className="details-map">
            <LazyMarkerMap
              markers={mapMarkers}
              bounds={water.bounds}
              shapes={water.shape ? [{ id: water.id, geometry: water.shape, highlighted: true }] : []}
              cluster
              introZoom
            />
            <ul className="legend muted">
              {water.shape && (
                <li>
                  <span className="legend-line" style={{ background: shapeStyle.color }} />
                  Verlauf aus{' '}
                  <a href={`https://www.openstreetmap.org/${water.osmElements[0]}`} target="_blank" rel="noreferrer">
                    OpenStreetMap
                  </a>
                </li>
              )}
              {legend
                .filter(([kind]) => mapMarkers.some((m) => m.kind === kind))
                .map(([kind, label]) => (
                  <li key={kind}>
                    <span className="legend-dot" style={{ background: markerColors[kind].color }} />
                    {label}
                  </li>
                ))}
            </ul>
          </div>
        )}
      </div>
    </article>
  )
}

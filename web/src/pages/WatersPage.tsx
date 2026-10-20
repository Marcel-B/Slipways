import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useDeferredValue, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { api } from '../api/client'
import type { WaterShapes, WaterSummary } from '../api/types'
import { LazyMarkerMap } from '../components/LazyMarkerMap'
import type { MapMarker, MapShape } from '../components/mapMarkers'
import { WaterLevelText } from '../components/WaterLevelText'
import { count } from './count'

function contents(water: WaterSummary): string {
  const parts = [
    water.slipwayCount > 0 && count(water.slipwayCount, 'Slipanlage', 'Slipanlagen'),
    water.stationCount > 0 && count(water.stationCount, 'Pegel', 'Pegel'),
  ].filter(Boolean)
  return parts.length > 0 ? parts.join(' · ') : 'Keine Slipanlagen oder Pegel'
}

function popup(water: WaterSummary) {
  return { title: water.name, subtitle: contents(water), to: `/waters/${water.id}` }
}

function waterMarkers(waters: WaterSummary[]): MapMarker[] {
  return waters.flatMap((w) => (w.position ? [{ id: w.id, kind: 'water', ...w.position, ...popup(w) }] : []))
}

function waterShapes(shapes: WaterShapes | undefined, waters: WaterSummary[], selectedId: string | null): MapShape[] {
  const byId = new Map(waters.map((w) => [w.id, w]))
  return (shapes?.features ?? []).flatMap((f) => {
    // Shapes and list are fetched separately; draw only what the list shows, too.
    const water = byId.get(String(f.id))
    return water ? [{ id: water.id, geometry: f.geometry, highlighted: water.id === selectedId, popup: popup(water) }] : []
  })
}

export function WatersPage() {
  const [params, setParams] = useSearchParams()
  const search = params.get('q') ?? ''
  const deferredSearch = useDeferredValue(search.trim())
  const {
    data: waters,
    error,
    isPending,
  } = useQuery({
    queryKey: ['waters', deferredSearch],
    queryFn: ({ signal }) => api.waters(deferredSearch, signal),
    placeholderData: keepPreviousData,
  })
  const { data: shapes } = useQuery({
    queryKey: ['water-shapes', deferredSearch],
    queryFn: ({ signal }) => api.waterShapes(deferredSearch, signal),
    placeholderData: keepPreviousData,
  })
  const [selectedId, setSelectedId] = useState<string | null>(null)

  return (
    <div className="overview">
      <section className="panel">
        <h1>Gewässer</h1>
        <input
          type="search"
          className="search"
          placeholder="Nach Gewässer suchen"
          aria-label="Gewässer durchsuchen"
          value={search}
          onChange={(e) => setParams(e.target.value ? { q: e.target.value } : {}, { replace: true })}
        />
        {error && <p className="error">Gewässer konnten nicht geladen werden: {error.message}</p>}
        {isPending && <p className="muted">Lade…</p>}
        {waters && (
          <>
            <p className="muted">{count(waters.length, 'Gewässer', 'Gewässer')}</p>
            <ul className="item-list">
              {waters.map((w) => (
                <li key={w.id}>
                  <Link to={`/waters/${w.id}`}>
                    <strong>{w.name}</strong>
                    <span className="muted">{contents(w)}</span>
                    {w.gauge && (
                      <span>
                        Pegel {w.gauge.stationName}: <WaterLevelText level={w.gauge.waterLevel} />
                      </span>
                    )}
                  </Link>
                </li>
              ))}
            </ul>
          </>
        )}
      </section>
      <section className="map-panel">
        <LazyMarkerMap
          markers={waterMarkers(waters ?? [])}
          shapes={waterShapes(shapes, waters ?? [], selectedId)}
          onSelect={setSelectedId}
        />
      </section>
    </div>
  )
}

import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useDeferredValue, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { api } from '../api/client'
import type { WaterSummary } from '../api/types'
import { LazyMarkerMap } from '../components/LazyMarkerMap'
import type { MapMarker } from '../components/mapMarkers'
import { WaterLevelText } from '../components/WaterLevelText'
import { count } from './count'

function contents(water: WaterSummary): string {
  const parts = [
    water.slipwayCount > 0 && count(water.slipwayCount, 'Slipanlage', 'Slipanlagen'),
    water.stationCount > 0 && count(water.stationCount, 'Pegel', 'Pegel'),
  ].filter(Boolean)
  return parts.length > 0 ? parts.join(' · ') : 'Keine Slipanlagen oder Pegel'
}

function waterMarkers(waters: WaterSummary[]): MapMarker[] {
  return waters.flatMap((w) =>
    w.position
      ? [{ id: w.id, kind: 'water', ...w.position, title: w.name, subtitle: contents(w), to: `/waters/${w.id}` }]
      : [],
  )
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
  // The shape comes with the details, which the details page then finds in the cache.
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const { data: selected } = useQuery({
    queryKey: ['water', selectedId],
    queryFn: ({ signal }) => api.water(selectedId!, signal),
    enabled: selectedId !== null,
  })
  const shape = selected?.id === selectedId && selected?.shape ? { id: selected.id, geometry: selected.shape } : undefined

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
          shape={shape}
          onSelect={(marker) => setSelectedId(marker?.id ?? null)}
        />
      </section>
    </div>
  )
}

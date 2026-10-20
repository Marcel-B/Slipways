import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { ChevronRight, Waves } from 'lucide-react'
import { useDeferredValue, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { api } from '../api/client'
import type { WaterShapes, WaterSummary } from '../api/types'
import { LazyMarkerMap } from '../components/LazyMarkerMap'
import { ListSkeleton } from '../components/ListSkeleton'
import type { MapMarker, MapShape } from '../components/mapMarkers'
import { Overview } from '../components/Overview'
import { SearchField } from '../components/SearchField'
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
    <>
      <title>Gewässer · Slipways</title>
      <section className="hero">
        <h1>Gewässer</h1>
        <p className="hero-lead">Flüsse, Seen und Kanäle mit ihren Slipanlagen und Pegeln.</p>
        <SearchField
          value={search}
          onChange={(value) => setParams(value ? { q: value } : {}, { replace: true })}
          placeholder="Nach Gewässer suchen"
          label="Gewässer durchsuchen"
        />
      </section>
      <Overview
        list={
          <>
            {error && <p className="error">Gewässer konnten nicht geladen werden: {error.message}</p>}
            {isPending && <ListSkeleton />}
            {waters && (
              <>
                <p className="list-count">{count(waters.length, 'Gewässer', 'Gewässer')}</p>
                {waters.length === 0 && <p className="empty">Kein Gewässer passt zu dieser Suche.</p>}
                <ul className="item-list">
                  {waters.map((w) => (
                    <li key={w.id}>
                      <Link to={`/waters/${w.id}`} className="item">
                        <span className="item-icon item-icon-water">
                          <Waves size={18} />
                        </span>
                        <span className="item-text">
                          <strong>{w.name}</strong>
                          <span className="muted">{contents(w)}</span>
                          {w.gauge && (
                            <span className="item-gauge">
                              Pegel {w.gauge.stationName}: <WaterLevelText level={w.gauge.waterLevel} />
                            </span>
                          )}
                        </span>
                        <ChevronRight size={18} className="item-chevron" />
                      </Link>
                    </li>
                  ))}
                </ul>
              </>
            )}
          </>
        }
        map={
          <LazyMarkerMap
            markers={waterMarkers(waters ?? [])}
            shapes={waterShapes(shapes, waters ?? [], selectedId)}
            onSelect={setSelectedId}
            cluster
          />
        }
      />
    </>
  )
}

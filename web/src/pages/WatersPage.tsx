import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { ChevronRight, Waves } from 'lucide-react'
import { useDeferredValue, useMemo, useState } from 'react'
import { Link } from 'react-router'
import { api } from '../api/client'
import type { WaterShapes, WaterSummary } from '../api/types'
import { coreBounds } from '../components/countries'
import { CountrySelect } from '../components/CountrySelect'
import { LazyMarkerMap } from '../components/LazyMarkerMap'
import { ListSkeleton } from '../components/ListSkeleton'
import type { MapMarker, MapShape } from '../components/mapMarkers'
import { Overview } from '../components/Overview'
import { SearchField } from '../components/SearchField'
import { WaterLevelText } from '../components/WaterLevelText'
import { count } from './count'
import { useQueryParams } from './searchParams'

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
  return waters.flatMap((w) =>
    w.position ? [{ id: w.id, kind: 'water', ...w.position, ...popup(w), country: w.countries[0] ?? null }] : [],
  )
}

/** How many waters touch each country, most first; a river through three countries counts in all three. */
function countryOptions(waters: WaterSummary[]): { code: string; count: number }[] {
  const counts = new Map<string, number>()
  for (const code of waters.flatMap((w) => w.countries)) counts.set(code, (counts.get(code) ?? 0) + 1)
  return [...counts]
    .map(([code, n]) => ({ code, count: n }))
    .sort((a, b) => b.count - a.count || a.code.localeCompare(b.code))
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
  const [params, setParam] = useQueryParams()
  const search = params.get('q') ?? ''
  const country = params.get('land')
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
  // Counted over all waters the search finds, so the choice keeps its numbers.
  const countries = useMemo(() => countryOptions(waters ?? []), [waters])
  const shown = useMemo(
    () => (country && waters ? waters.filter((w) => w.countries.includes(country)) : waters),
    [waters, country],
  )
  // A chosen country moves the map to it.
  const focus = useMemo(
    () => (country && shown ? coreBounds(shown.flatMap((w) => (w.position ? [w.position] : []))) : null),
    [shown, country],
  )

  return (
    <>
      <title>Gewässer · Slipways</title>
      <section className="toolbar">
        <h1>Gewässer finden</h1>
        <SearchField
          value={search}
          onChange={(value) => setParam('q', value)}
          placeholder="Nach Gewässer suchen"
          label="Gewässer durchsuchen"
        />
        <CountrySelect options={countries} value={country} onChange={(code) => setParam('land', code)} />
      </section>
      <Overview
        list={
          <>
            {error && <p className="error">Gewässer konnten nicht geladen werden: {error.message}</p>}
            {isPending && <ListSkeleton />}
            {shown && (
              <>
                <p className="list-count">{count(shown.length, 'Gewässer', 'Gewässer')}</p>
                {shown.length === 0 && <p className="empty">Kein Gewässer passt zu dieser Suche.</p>}
                <ul className="item-list">
                  {shown.map((w) => (
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
            markers={waterMarkers(shown ?? [])}
            shapes={waterShapes(shapes, shown ?? [], selectedId)}
            onSelect={setSelectedId}
            cluster
            focus={focus}
          />
        }
      />
    </>
  )
}

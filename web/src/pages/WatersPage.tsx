import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { ChevronRight, Waves } from 'lucide-react'
import { memo, useDeferredValue, useMemo, useState } from 'react'
import { Link } from 'react-router'
import { api } from '../api/client'
import type { WaterShapes, WaterSummary } from '../api/types'
import { coreBounds } from '../components/countries'
import { CountrySelect } from '../components/CountrySelect'
import { LazyMarkerMap } from '../components/LazyMarkerMap'
import { ListSkeleton } from '../components/ListSkeleton'
import type { MapShape } from '../components/mapMarkers'
import { boundsOf, listedInView, shapeArea, type MapView } from '../components/mapView'
import { Overview } from '../components/Overview'
import { SearchField } from '../components/SearchField'
import { WaterLevelText } from '../components/WaterLevelText'
import { ZoomHint } from '../components/ZoomHint'
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

// Starts with every country on screen, like the slipways and marinas.
const EUROPE: [number, number] = [50, 5]

/** Waters with a position, placed there for the list of what is in the map view. */
function placed(waters: WaterSummary[]) {
  return waters.flatMap((w) => (w.position ? [{ ...w, ...w.position }] : []))
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
    isPlaceholderData,
  } = useQuery({
    queryKey: ['waters', deferredSearch],
    queryFn: ({ signal }) => api.waters(deferredSearch, signal),
    placeholderData: keepPreviousData,
  })
  const [view, setView] = useState<MapView | null>(null)
  // The map shows waters as their shapes, not as bubbles. Thousands of shapes are too much to load at once, so a
  // search loads those of its hits and otherwise the map loads those in its view, further out only the large ones.
  const area = useMemo(() => (view ? shapeArea(view) : null), [view])
  const shapeFilter = deferredSearch ? { search: deferredSearch } : area
  const { data: shapes } = useQuery({
    queryKey: ['water-shapes', shapeFilter],
    queryFn: ({ signal }) => api.waterShapes(shapeFilter!, signal),
    enabled: shapeFilter !== null,
    placeholderData: keepPreviousData,
  })
  const [selectedId, setSelectedId] = useState<string | null>(null)
  // Counted over all waters the search finds, so the choice keeps its numbers.
  const countries = useMemo(() => countryOptions(waters ?? []), [waters])
  const shown = useMemo(
    () => (country && waters ? waters.filter((w) => w.countries.includes(country)) : waters),
    [waters, country],
  )
  // While a new search loads, the previous results stand in; they only count as hits once they belong to it.
  const searching = deferredSearch !== '' && !isPlaceholderData
  const listed = useMemo(
    () => shown && (searching ? shown : listedInView(placed(shown), view, false)),
    [shown, view, searching],
  )
  // A search moves the map to its hits, a chosen country to that country.
  const focus = useMemo(() => {
    if (!shown || !(searching || country)) return null
    const positions = shown.flatMap((w) => (w.position ? [w.position] : []))
    return searching ? boundsOf(positions) : coreBounds(positions)
  }, [shown, searching, country])
  const mapShapes = useMemo(() => waterShapes(shapes, shown ?? [], selectedId), [shapes, shown, selectedId])

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
            {shown && !listed && <ZoomHint total={shown.length} singular="Gewässer" plural="Gewässer" />}
            {listed && (
              <>
                <p className="list-count">
                  {searching
                    ? count(listed.length, 'Treffer', 'Treffer')
                    : `${count(listed.length, 'Gewässer', 'Gewässer')} im Kartenausschnitt`}
                </p>
                {listed.length === 0 && (
                  <p className="empty">
                    {searching
                      ? 'Kein Gewässer passt zu dieser Suche.'
                      : 'Hier liegt kein Gewässer. Verschiebe die Karte oder zoome heraus.'}
                  </p>
                )}
                <WaterList waters={listed} />
              </>
            )}
          </>
        }
        initialView="map"
        map={
          <LazyMarkerMap
            markers={[]}
            center={EUROPE}
            zoom={4}
            shapes={mapShapes}
            onSelect={setSelectedId}
            onViewChange={setView}
            focus={focus}
          />
        }
      />
    </>
  )
}

// Up to hundreds of entries: memoised, so selecting a shape (which re-renders the page for the map) leaves the list.
const WaterList = memo(function WaterList({ waters }: { waters: WaterSummary[] }) {
  return (
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
  )
})

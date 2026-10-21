import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Anchor, ChevronRight } from 'lucide-react'
import { memo, useDeferredValue, useMemo, useState } from 'react'
import { Link } from 'react-router'
import { api } from '../api/client'
import type { Marina } from '../api/types'
import { LazyMarkerMap } from '../components/LazyMarkerMap'
import { ListSkeleton } from '../components/ListSkeleton'
import { byCountry, coreBounds } from '../components/countries'
import { CountrySelect } from '../components/CountrySelect'
import { marinaMarker } from '../components/mapMarkers'
import { boundsOf, listedInView, type MapView } from '../components/mapView'
import { Overview } from '../components/Overview'
import { SearchField } from '../components/SearchField'
import { ZoomHint } from '../components/ZoomHint'
import { count } from './count'
import { useQueryParams } from './searchParams'

// Starts with every country on screen, one bubble each, like the slipways.
const EUROPE: [number, number] = [50, 5]

export function MarinasPage() {
  const [params, setParam] = useQueryParams()
  const search = params.get('q') ?? ''
  const country = params.get('land')
  const deferredSearch = useDeferredValue(search.trim())
  const {
    data: marinas,
    error,
    isPending,
    isPlaceholderData,
  } = useQuery({
    queryKey: ['marinas', deferredSearch],
    queryFn: ({ signal }) => api.marinas(deferredSearch, signal),
    placeholderData: keepPreviousData,
  })
  // Counted over all marinas, so the choice keeps its numbers while a search narrows the map.
  const countries = useMemo(() => byCountry(marinas ?? []), [marinas])
  const shown = useMemo(
    () => (country && marinas ? marinas.filter((m) => m.country === country) : marinas),
    [marinas, country],
  )
  const markers = useMemo(() => (shown ?? []).map(marinaMarker), [shown])
  const [hoveredId, setHoveredId] = useState<string | null>(null)
  const [view, setView] = useState<MapView | null>(null)
  // While a new search loads, the previous results stand in; they only count as hits once they belong to it.
  const searching = deferredSearch !== '' && !isPlaceholderData
  const listed = useMemo(() => shown && listedInView(shown, view, searching), [shown, view, searching])
  // A search moves the map to its hits, so they are on screen next to the list; a chosen country to that country.
  const focus = useMemo(
    () => (shown && searching ? boundsOf(shown) : shown && country ? coreBounds(shown) : null),
    [shown, searching, country],
  )

  return (
    <>
      <title>Marinas · Slipways</title>
      <section className="toolbar">
        <h1>Marinas finden</h1>
        <SearchField
          value={search}
          onChange={(value) => setParam('q', value)}
          placeholder="Nach Name, Ort oder Gewässer suchen"
          label="Marinas durchsuchen"
        />
        <CountrySelect
          options={countries.map(({ code, items }) => ({ code, count: items.length }))}
          value={country}
          onChange={(code) => setParam('land', code)}
        />
      </section>
      <Overview
        list={
          <>
            {error && <p className="error">Marinas konnten nicht geladen werden: {error.message}</p>}
            {isPending && <ListSkeleton />}
            {shown && !listed && <ZoomHint total={shown.length} singular="Marina" plural="Marinas" />}
            {listed && (
              <>
                <p className="list-count">
                  {searching
                    ? count(listed.length, 'Treffer', 'Treffer')
                    : `${count(listed.length, 'Marina', 'Marinas')} im Kartenausschnitt`}
                </p>
                {listed.length === 0 && (
                  <p className="empty">
                    {searching
                      ? 'Keine Marina passt zu dieser Suche.'
                      : 'Hier liegt keine Marina. Verschiebe die Karte oder zoome heraus.'}
                  </p>
                )}
                <MarinaList marinas={listed} onHover={setHoveredId} />
              </>
            )}
          </>
        }
        initialView="map"
        map={
          <LazyMarkerMap
            markers={markers}
            center={EUROPE}
            zoom={4}
            cluster
            highlightedId={hoveredId}
            onViewChange={setView}
            focus={focus}
          />
        }
      />
    </>
  )
}

// Thousands of entries: memoised, so hovering one (which re-renders the page for the map) does not re-render the list.
const MarinaList = memo(function MarinaList({
  marinas,
  onHover,
}: {
  marinas: Marina[]
  onHover: (id: string | null) => void
}) {
  return (
    <ul className="item-list">
      {marinas.map((m) => (
        <li key={m.id} onMouseEnter={() => onHover(m.id)} onMouseLeave={() => onHover(null)}>
          <Link to={`/marinas/${m.id}`} className="item">
            <span className="item-icon item-icon-marina">
              <Anchor size={18} />
            </span>
            <span className="item-text">
              <strong>{m.name}</strong>
              <span className="muted">
                {[m.water?.name, [m.postalCode, m.city].filter(Boolean).join(' ')].filter(Boolean).join(' · ')}
              </span>
            </span>
            <ChevronRight size={18} className="item-chevron" />
          </Link>
        </li>
      ))}
    </ul>
  )
})

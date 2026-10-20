import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Anchor, ChevronRight } from 'lucide-react'
import { memo, useDeferredValue, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { api } from '../api/client'
import type { Marina } from '../api/types'
import { LazyMarkerMap } from '../components/LazyMarkerMap'
import { ListSkeleton } from '../components/ListSkeleton'
import { marinaMarker } from '../components/mapMarkers'
import { boundsOf, listedInView, type MapView } from '../components/mapView'
import { Overview } from '../components/Overview'
import { SearchField } from '../components/SearchField'
import { ZoomHint } from '../components/ZoomHint'
import { count } from './count'

export function MarinasPage() {
  const [params, setParams] = useSearchParams()
  const search = params.get('q') ?? ''
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
  const markers = useMemo(() => (marinas ?? []).map(marinaMarker), [marinas])
  const [hoveredId, setHoveredId] = useState<string | null>(null)
  const [view, setView] = useState<MapView | null>(null)
  // While a new search loads, the previous results stand in; they only count as hits once they belong to it.
  const searching = deferredSearch !== '' && !isPlaceholderData
  const listed = useMemo(() => marinas && listedInView(marinas, view, searching), [marinas, view, searching])
  // A search moves the map to its hits, so they are on screen next to the list.
  const focus = useMemo(() => (searching && marinas ? boundsOf(marinas) : null), [marinas, searching])

  return (
    <>
      <section className="hero">
        <h1>Marinas</h1>
        <p className="hero-lead">Häfen und Marinas an Flüssen, Seen und Kanälen, mit den Slipanlagen, die zu ihnen gehören.</p>
        <SearchField
          value={search}
          onChange={(value) => setParams(value ? { q: value } : {}, { replace: true })}
          placeholder="Nach Name, Ort oder Gewässer suchen"
          label="Marinas durchsuchen"
        />
      </section>
      <Overview
        list={
          <>
            {error && <p className="error">Marinas konnten nicht geladen werden: {error.message}</p>}
            {isPending && <ListSkeleton />}
            {marinas && !listed && <ZoomHint total={marinas.length} singular="Marina" plural="Marinas" />}
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
          <LazyMarkerMap markers={markers} cluster highlightedId={hoveredId} onViewChange={setView} focus={focus} />
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

import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { ChevronRight, MapPinPlus, Sailboat } from 'lucide-react'
import { memo, useDeferredValue, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { api } from '../api/client'
import type { SlipwaySummary } from '../api/types'
import { LazyMarkerMap } from '../components/LazyMarkerMap'
import { ListSkeleton } from '../components/ListSkeleton'
import { slipwayMarker } from '../components/mapMarkers'
import { boundsOf, listedInView, type MapView } from '../components/mapView'
import { Overview } from '../components/Overview'
import { SearchField } from '../components/SearchField'
import { ZoomHint } from '../components/ZoomHint'
import { count } from './count'

export function SlipwaysPage() {
  const [params, setParams] = useSearchParams()
  const search = params.get('q') ?? ''
  const deferredSearch = useDeferredValue(search.trim())
  const {
    data: slipways,
    error,
    isPending,
    isPlaceholderData,
  } = useQuery({
    queryKey: ['slipways', deferredSearch],
    queryFn: ({ signal }) => api.slipways(deferredSearch, signal),
    placeholderData: keepPreviousData,
  })
  const markers = useMemo(() => (slipways ?? []).map(slipwayMarker), [slipways])
  const [hoveredId, setHoveredId] = useState<string | null>(null)
  const [view, setView] = useState<MapView | null>(null)
  // While a new search loads, the previous results stand in; they only count as hits once they belong to it.
  const searching = deferredSearch !== '' && !isPlaceholderData
  const listed = useMemo(() => slipways && listedInView(slipways, view, searching), [slipways, view, searching])
  // A search moves the map to its hits, so they are on screen next to the list.
  const focus = useMemo(() => (searching && slipways ? boundsOf(slipways) : null), [slipways, searching])
  // A new slipway starts where the sender is looking, if they zoomed in far enough to mean a place.
  const newSlipway =
    view && view.zoom >= 10
      ? `/slipways/neu?lat=${((view.bbox[1] + view.bbox[3]) / 2).toFixed(5)}&lon=${((view.bbox[0] + view.bbox[2]) / 2).toFixed(5)}&z=${view.zoom}`
      : '/slipways/neu'

  return (
    <>
      <title>Slipways · Slipanlagen, Marinas und Pegel</title>
      {/* A slim bar instead of a hero band, so the map starts high and fits the screen. */}
      <section className="toolbar">
        <h1>Slipanlagen finden</h1>
        <SearchField
          value={search}
          onChange={(value) => setParams(value ? { q: value } : {}, { replace: true })}
          placeholder="Nach Name, Ort oder Gewässer suchen"
          label="Slipanlagen durchsuchen"
        />
        <Link to={newSlipway} className="toolbar-link" aria-label="Slipanlage fehlt? Vorschlagen">
          <MapPinPlus size={18} /> <span>Slipanlage fehlt?</span>
        </Link>
      </section>
      <Overview
        list={
          <>
            {error && <p className="error">Slipanlagen konnten nicht geladen werden: {error.message}</p>}
            {isPending && <ListSkeleton />}
            {slipways && !listed && <ZoomHint total={slipways.length} singular="Slipanlage" plural="Slipanlagen" />}
            {listed && (
              <>
                <p className="list-count">
                  {searching
                    ? count(listed.length, 'Treffer', 'Treffer')
                    : `${count(listed.length, 'Slipanlage', 'Slipanlagen')} im Kartenausschnitt`}
                </p>
                {listed.length === 0 && (
                  <p className="empty">
                    {searching
                      ? 'Keine Slipanlage passt zu dieser Suche.'
                      : 'Hier liegt keine Slipanlage. Verschiebe die Karte oder zoome heraus.'}
                  </p>
                )}
                <SlipwayList slipways={listed} onHover={setHoveredId} />
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
const SlipwayList = memo(function SlipwayList({
  slipways,
  onHover,
}: {
  slipways: SlipwaySummary[]
  onHover: (id: string | null) => void
}) {
  return (
    <ul className="item-list">
      {slipways.map((s) => (
        <li key={s.id} onMouseEnter={() => onHover(s.id)} onMouseLeave={() => onHover(null)}>
          <Link to={`/slipways/${s.id}`} className="item">
            <span className="item-icon">
              <Sailboat size={18} />
            </span>
            <span className="item-text">
              <strong>{s.name}</strong>
              <span className="muted">
                {[s.water?.name, [s.postalCode, s.city].filter(Boolean).join(' ')].filter(Boolean).join(' · ')}
              </span>
            </span>
            <ChevronRight size={18} className="item-chevron" />
          </Link>
        </li>
      ))}
    </ul>
  )
})

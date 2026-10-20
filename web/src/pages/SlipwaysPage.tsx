import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { ChevronRight, Sailboat } from 'lucide-react'
import { memo, useDeferredValue, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router'
import { api } from '../api/client'
import type { SlipwaySummary } from '../api/types'
import { LazyMarkerMap } from '../components/LazyMarkerMap'
import { ListSkeleton } from '../components/ListSkeleton'
import { slipwayMarker } from '../components/mapMarkers'
import { Overview } from '../components/Overview'
import { SearchField } from '../components/SearchField'

export function SlipwaysPage() {
  const [params, setParams] = useSearchParams()
  const search = params.get('q') ?? ''
  const deferredSearch = useDeferredValue(search.trim())
  const {
    data: slipways,
    error,
    isPending,
  } = useQuery({
    queryKey: ['slipways', deferredSearch],
    queryFn: ({ signal }) => api.slipways(deferredSearch, signal),
    placeholderData: keepPreviousData,
  })
  const markers = useMemo(() => (slipways ?? []).map(slipwayMarker), [slipways])
  const [hoveredId, setHoveredId] = useState<string | null>(null)

  return (
    <>
      <section className="hero">
        <h1>Slipanlagen finden</h1>
        <p className="hero-lead">
          Slipanlagen an Flüssen, Seen und Kanälen in Deutschland, mit aktuellen Pegelständen von PEGELONLINE.
        </p>
        <SearchField
          value={search}
          onChange={(value) => setParams(value ? { q: value } : {}, { replace: true })}
          placeholder="Nach Name, Ort oder Gewässer suchen"
          label="Slipanlagen durchsuchen"
        />
      </section>
      <Overview
        list={
          <>
            {error && <p className="error">Slipanlagen konnten nicht geladen werden: {error.message}</p>}
            {isPending && <ListSkeleton />}
            {slipways && (
              <>
                <p className="list-count">{slipways.length} Slipanlagen</p>
                {slipways.length === 0 && <p className="empty">Keine Slipanlage passt zu dieser Suche.</p>}
                <SlipwayList slipways={slipways} onHover={setHoveredId} />
              </>
            )}
          </>
        }
        map={<LazyMarkerMap markers={markers} cluster highlightedId={hoveredId} />}
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

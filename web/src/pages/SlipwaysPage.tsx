import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { useDeferredValue } from 'react'
import { Link, useSearchParams } from 'react-router'
import { api } from '../api/client'
import { LazyMarkerMap } from '../components/LazyMarkerMap'
import { slipwayMarker } from '../components/mapMarkers'

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

  return (
    <div className="overview">
      <section className="panel">
        <h1>Slipanlagen</h1>
        <input
          type="search"
          className="search"
          placeholder="Nach Name, Ort oder Gewässer suchen"
          aria-label="Slipanlagen durchsuchen"
          value={search}
          onChange={(e) => setParams(e.target.value ? { q: e.target.value } : {}, { replace: true })}
        />
        {error && <p className="error">Slipanlagen konnten nicht geladen werden: {error.message}</p>}
        {isPending && <p className="muted">Lade…</p>}
        {slipways && (
          <>
            <p className="muted">{slipways.length} Slipanlagen</p>
            <ul className="item-list">
              {slipways.map((s) => (
                <li key={s.id}>
                  <Link to={`/slipways/${s.id}`}>
                    <strong>{s.name}</strong>
                    <span className="muted">
                      {[s.water.name, [s.postalCode, s.city].filter(Boolean).join(' ')].filter(Boolean).join(' · ')}
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          </>
        )}
      </section>
      <section className="map-panel">
        <LazyMarkerMap markers={(slipways ?? []).map(slipwayMarker)} />
      </section>
    </div>
  )
}

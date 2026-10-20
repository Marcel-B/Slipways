import { List, MapIcon } from 'lucide-react'
import { useState, type ReactNode } from 'react'

/** List and map side by side; on narrow screens one at a time, with a switch between them. */
export function Overview({
  list,
  map,
  initialView = 'list',
}: {
  list: ReactNode
  map: ReactNode
  /** Which of the two a narrow screen starts with. */
  initialView?: 'list' | 'map'
}) {
  const [view, setView] = useState(initialView)

  return (
    <div className="overview" data-view={view}>
      <div className="view-toggle" role="group" aria-label="Ansicht">
        <button type="button" aria-pressed={view === 'list'} onClick={() => setView('list')}>
          <List size={16} /> Liste
        </button>
        <button type="button" aria-pressed={view === 'map'} onClick={() => setView('map')}>
          <MapIcon size={16} /> Karte
        </button>
      </div>
      <section className="panel list-panel">{list}</section>
      <section className="map-panel">{map}</section>
    </div>
  )
}

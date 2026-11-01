import { useCallback, useState } from 'react'
import { sameView, type MapView } from './mapView'

/**
 * The map's view, whether it has left the view it started with, and a way back there. `home` counts the requests;
 * `MarkerMap` flies back to its start whenever it changes.
 */
export function useMapHome() {
  const [view, setView] = useState<MapView | null>(null)
  const [start, setStart] = useState<MapView | null>(null)
  const [home, setHome] = useState(0)
  const onViewChange = useCallback((next: MapView) => {
    setStart((first) => first ?? next)
    setView(next)
  }, [])
  const goHome = useCallback(() => setHome((count) => count + 1), [])
  const moved = view !== null && start !== null && !sameView(view, start)
  return { view, onViewChange, moved, home, goHome }
}

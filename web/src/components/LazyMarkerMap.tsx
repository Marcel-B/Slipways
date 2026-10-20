import { lazy, Suspense, type ComponentProps } from 'react'

// Leaflet is the largest dependency, so it loads in its own chunk.
const MarkerMap = lazy(() => import('./MarkerMap').then((m) => ({ default: m.MarkerMap })))

export function LazyMarkerMap(props: ComponentProps<typeof MarkerMap>) {
  return (
    <Suspense fallback={<div className="map" />}>
      <MarkerMap {...props} />
    </Suspense>
  )
}

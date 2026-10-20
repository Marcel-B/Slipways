import { lazy, Suspense, type ComponentProps } from 'react'

// Leaflet loads in its own chunk, as for the other maps.
const PickMap = lazy(() => import('./PickMap').then((m) => ({ default: m.PickMap })))

export function LazyPickMap(props: ComponentProps<typeof PickMap>) {
  return (
    <Suspense fallback={<div className="map" />}>
      <PickMap {...props} />
    </Suspense>
  )
}

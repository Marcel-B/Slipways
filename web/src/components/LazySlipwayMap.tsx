import { lazy, Suspense, type ComponentProps } from 'react'

// Leaflet is the largest dependency, so it loads in its own chunk.
const SlipwayMap = lazy(() => import('./SlipwayMap').then((m) => ({ default: m.SlipwayMap })))

export function LazySlipwayMap(props: ComponentProps<typeof SlipwayMap>) {
  return (
    <Suspense fallback={<div className="map" />}>
      <SlipwayMap {...props} />
    </Suspense>
  )
}

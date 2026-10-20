import { useQuery } from '@tanstack/react-query'
import { Ruler } from 'lucide-react'
import { Link } from 'react-router'
import { api } from '../api/client'
import type { Position } from '../api/types'
import { distanceKm } from './distance'
import { formatMeasuredAt } from './waterLevel'
import { WaterLevelText } from './WaterLevelText'

const kilometres = new Intl.NumberFormat('de-DE', { maximumFractionDigits: 1 })

/** The gauge with a current reading closest to a place, from the water the place is on. */
export function NearestGauge({ position, waterId }: { position: Position; waterId: string }) {
  // Same query as the water's page, so going there afterwards needs no new request.
  const { data: water } = useQuery({
    queryKey: ['water', waterId],
    queryFn: ({ signal }) => api.water(waterId, signal),
  })

  const nearest = (water?.stations ?? [])
    .flatMap((s) => (s.waterLevel ? [{ station: s, level: s.waterLevel, km: distanceKm(position, s) }] : []))
    .sort((a, b) => a.km - b.km)[0]
  if (!nearest) return null

  const { station, level, km } = nearest
  return (
    <section className="gauge-card panel" aria-label="Nächster Pegel">
      <span className="gauge-icon">
        <Ruler size={20} />
      </span>
      <div>
        <p className="gauge-title">
          Nächster Pegel: <Link to={`/waters/${waterId}`}>{station.name}</Link>
        </p>
        <p className="gauge-value">
          <WaterLevelText level={level} />
        </p>
        <p className="muted gauge-meta">
          {kilometres.format(km)} km entfernt · gemessen {formatMeasuredAt(level)}
        </p>
      </div>
    </section>
  )
}

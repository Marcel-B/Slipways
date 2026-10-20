import type { WaterLevel } from '../api/types'
import { formatMeasuredAt, formatWaterLevel } from './waterLevel'

export function WaterLevelText({ level }: { level: WaterLevel }) {
  return (
    <span className={`level level-${level.state ?? 'unknown'}`} title={`gemessen ${formatMeasuredAt(level)}`}>
      {formatWaterLevel(level)}
    </span>
  )
}

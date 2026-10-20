import { ZoomIn } from 'lucide-react'
import { count } from '../pages/count'

/** Stands in for the list while the map shows too large an area to list everything on it. */
export function ZoomHint({ total, singular, plural }: { total: number; singular: string; plural: string }) {
  return (
    <div className="zoom-hint">
      <ZoomIn size={28} />
      <p>
        <strong>{count(total, singular, plural)} auf der Karte.</strong> Zoome in die Karte hinein oder suche oben, dann
        erscheinen sie hier als Liste.
      </p>
    </div>
  )
}

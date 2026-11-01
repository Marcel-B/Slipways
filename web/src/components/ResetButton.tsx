import { RotateCcw } from 'lucide-react'

/** Clears the search and the country and brings the map back to the start; hidden while there is nothing to reset. */
export function ResetButton({ visible, onReset }: { visible: boolean; onReset: () => void }) {
  if (!visible) return null
  return (
    <button
      type="button"
      className="toolbar-button"
      onClick={onReset}
      aria-label="Zurücksetzen"
      title="Suche und Land aufheben, Karte auf Europa"
    >
      <RotateCcw size={18} /> <span>Zurücksetzen</span>
    </button>
  )
}

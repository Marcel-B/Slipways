import { countryName } from './countries'

interface CountrySelectProps {
  /** Countries to choose from, each with the number of places in it. */
  options: { code: string; count: number }[]
  value: string | null
  onChange: (code: string) => void
}

/** Chooses one country of the toolbar's list, or all; hidden while there is nothing to choose between. */
export function CountrySelect({ options, value, onChange }: CountrySelectProps) {
  if (options.length < 2) return null
  return (
    <select className="toolbar-select" aria-label="Land" value={value ?? ''} onChange={(e) => onChange(e.target.value)}>
      <option value="">Alle Länder</option>
      {options.map(({ code, count }) => (
        <option key={code} value={code}>
          {countryName(code)} ({count.toLocaleString('de-DE')})
        </option>
      ))}
    </select>
  )
}

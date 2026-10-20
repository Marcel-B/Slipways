import { Search } from 'lucide-react'

interface SearchFieldProps {
  value: string
  onChange: (value: string) => void
  placeholder: string
  label: string
}

export function SearchField({ value, onChange, placeholder, label }: SearchFieldProps) {
  return (
    <label className="search">
      <Search size={20} className="search-icon" />
      <input
        type="search"
        placeholder={placeholder}
        aria-label={label}
        value={value}
        onChange={(e) => onChange(e.target.value)}
      />
    </label>
  )
}

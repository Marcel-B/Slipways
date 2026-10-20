import { Monitor, Moon, Sun } from 'lucide-react'
import { useTheme, type ThemeChoice } from './theme'

const options: { value: ThemeChoice; label: string; Icon: typeof Sun }[] = [
  { value: 'light', label: 'Hell', Icon: Sun },
  { value: 'dark', label: 'Dunkel', Icon: Moon },
  { value: 'system', label: 'Wie das System', Icon: Monitor },
]

export function ThemeToggle() {
  const [choice, setChoice] = useTheme()

  return (
    <div className="theme-toggle" role="group" aria-label="Darstellung">
      {options.map(({ value, label, Icon }) => (
        <button
          key={value}
          type="button"
          aria-pressed={choice === value}
          aria-label={label}
          title={label}
          onClick={() => setChoice(value)}
        >
          <Icon size={16} />
        </button>
      ))}
    </div>
  )
}

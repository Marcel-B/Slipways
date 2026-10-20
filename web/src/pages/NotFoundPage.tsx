import { Compass } from 'lucide-react'
import { Link } from 'react-router'

export function NotFoundPage() {
  return (
    <section className="not-found panel">
      <Compass size={40} className="not-found-icon" />
      <h1>Hier liegt kein Boot</h1>
      <p>Diese Seite gibt es nicht.</p>
      <Link to="/" className="button button-primary">
        Zu den Slipanlagen
      </Link>
    </section>
  )
}

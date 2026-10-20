import { Link } from 'react-router'

export function NotFoundPage() {
  return (
    <p>
      Diese Seite gibt es nicht. <Link to="/">Zu den Slipanlagen</Link>
    </p>
  )
}

import { Link, NavLink, Outlet } from 'react-router'
import { useAuth } from '../auth/useAuth'

export function Layout() {
  const { user, logout } = useAuth()

  return (
    <>
      <header className="header">
        <Link to="/" className="brand">
          <img src="/favicon.svg" alt="" width={28} height={28} />
          Slipways
        </Link>
        <nav className="nav">
          <NavLink to="/" end>
            Slipanlagen
          </NavLink>
          {user ? (
            <>
              <span className="nav-user">{user.displayName ?? user.email}</span>
              <button type="button" className="link-button" onClick={logout}>
                Abmelden
              </button>
            </>
          ) : (
            <>
              <NavLink to="/login">Anmelden</NavLink>
              <NavLink to="/register">Registrieren</NavLink>
            </>
          )}
        </nav>
      </header>
      <main className="main">
        <Outlet />
      </main>
    </>
  )
}

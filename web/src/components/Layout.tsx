import { Link, NavLink, Outlet } from 'react-router'
import { ThemeToggle } from './ThemeToggle'

export function Layout() {
  return (
    <>
      <header className="header">
        <div className="header-inner">
          <Link to="/" className="brand">
            <span className="brand-logo">
              <img src="/favicon.svg" alt="" width={24} height={24} />
            </span>
            <span className="brand-name">Slipways</span>
          </Link>
          <nav className="nav">
            <NavLink to="/" end>
              Slipanlagen
            </NavLink>
            <NavLink to="/waters">Gewässer</NavLink>
            <NavLink to="/marinas">Marinas</NavLink>
          </nav>
          <ThemeToggle />
        </div>
      </header>
      <main className="main">
        <Outlet />
      </main>
      <footer className="footer">
        <div className="footer-inner">
          <nav className="footer-links">
            <Link to="/impressum">Impressum</Link>
            <Link to="/datenschutz">Datenschutz</Link>
            <Link to="/support">Support</Link>
          </nav>
          <p className="muted">
            Kartendaten ©{' '}
            <a href="https://www.openstreetmap.org/copyright">OpenStreetMap-Mitwirkende</a> (ODbL) · Pegelstände{' '}
            <a href="https://www.pegelonline.wsv.de/">PEGELONLINE</a> der WSV (DL-DE-&gt;Zero-2.0)
          </p>
        </div>
      </footer>
    </>
  )
}

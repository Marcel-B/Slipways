import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { useAuth } from '../auth/useAuth'

export function AuthPage({ mode }: { mode: 'login' | 'register' }) {
  const { login, register } = useAuth()
  const navigate = useNavigate()
  const [error, setError] = useState<string>()
  const [busy, setBusy] = useState(false)
  const isLogin = mode === 'login'

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    const email = String(form.get('email'))
    const password = String(form.get('password'))
    setBusy(true)
    setError(undefined)
    try {
      await (isLogin ? login(email, password) : register(email, password))
      navigate('/')
    } catch (e) {
      setError(isLogin ? 'E-Mail oder Passwort ist falsch.' : (e as Error).message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <form className="auth-form panel" onSubmit={handleSubmit}>
      <h1>{isLogin ? 'Anmelden' : 'Registrieren'}</h1>
      <label>
        E-Mail
        <input name="email" type="email" autoComplete="email" required />
      </label>
      <label>
        Passwort
        <input
          name="password"
          type="password"
          autoComplete={isLogin ? 'current-password' : 'new-password'}
          minLength={8}
          required
        />
      </label>
      {error && <p className="error">{error}</p>}
      <button type="submit" disabled={busy}>
        {isLogin ? 'Anmelden' : 'Konto anlegen'}
      </button>
      <p className="muted">
        {isLogin ? (
          <>
            Noch kein Konto? <Link to="/register">Registrieren</Link>
          </>
        ) : (
          <>
            Schon registriert? <Link to="/login">Anmelden</Link>
          </>
        )}
      </p>
    </form>
  )
}

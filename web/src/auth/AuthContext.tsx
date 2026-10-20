import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api, getStoredToken, storeToken } from '../api/client'
import type { User } from '../api/types'
import { AuthContext } from './context'


export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)

  useEffect(() => {
    if (!getStoredToken()) return
    api.me().then(setUser).catch(() => storeToken(null))
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const token = await api.login(email, password)
    storeToken(token.accessToken)
    setUser(await api.me())
  }, [])

  const register = useCallback(
    async (email: string, password: string) => {
      await api.register(email, password)
      await login(email, password)
    },
    [login],
  )

  const logout = useCallback(() => {
    storeToken(null)
    setUser(null)
  }, [])

  const value = useMemo(() => ({ user, login, register, logout }), [user, login, register, logout])
  return <AuthContext value={value}>{children}</AuthContext>
}

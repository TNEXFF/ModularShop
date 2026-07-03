import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { api, type AuthUser } from '../api'

interface AuthState {
  user: AuthUser | null
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string, displayName: string) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthState | undefined>(undefined)

/**
 * Holds the signed-in user. On mount it asks the server who we are (GET /api/auth/me); a 401 simply
 * means "not signed in". Because auth is a cookie, there is no token to store on the client.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    api.me().then(setUser).catch(() => setUser(null)).finally(() => setLoading(false))
  }, [])

  const login = async (email: string, password: string) => setUser(await api.login(email, password))
  const register = async (email: string, password: string, displayName: string) =>
    setUser(await api.register(email, password, displayName))
  const logout = async () => { await api.logout().catch(() => {}); setUser(null) }

  return (
    <AuthContext.Provider value={{ user, loading, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider')
  return ctx
}

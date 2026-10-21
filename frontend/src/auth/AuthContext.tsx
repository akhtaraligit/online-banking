import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import * as authApi from '../api/auth'
import type { Customer } from '../types/auth'

type AuthContextValue = {
  customer: Customer | null
  token: string | null
  loading: boolean
  login: (username: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)
const TOKEN_KEY = 'ob_token'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem(TOKEN_KEY))
  const [customer, setCustomer] = useState<Customer | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false

    async function bootstrap() {
      if (!token) {
        setLoading(false)
        return
      }

      try {
        const me = await authApi.getMe(token)
        if (!cancelled) {
          setCustomer(me)
        }
      } catch {
        localStorage.removeItem(TOKEN_KEY)
        if (!cancelled) {
          setToken(null)
          setCustomer(null)
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    void bootstrap()
    return () => {
      cancelled = true
    }
  }, [token])

  const login = useCallback(async (username: string, password: string) => {
    const result = await authApi.login(username, password)
    localStorage.setItem(TOKEN_KEY, result.token)
    setToken(result.token)
    setCustomer(result.customer)
  }, [])

  const logout = useCallback(async () => {
    if (token) {
      try {
        await authApi.logout(token)
      } catch {
        // still clear local session
      }
    }
    localStorage.removeItem(TOKEN_KEY)
    setToken(null)
    setCustomer(null)
  }, [token])

  const value = useMemo(
    () => ({ customer, token, loading, login, logout }),
    [customer, token, loading, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider')
  }
  return context
}

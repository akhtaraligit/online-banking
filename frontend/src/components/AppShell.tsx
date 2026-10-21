import { Link, NavLink } from 'react-router-dom'
import type { ReactNode } from 'react'
import { useAuth } from '../auth/AuthContext'

export function AppShell({ children }: { children: ReactNode }) {
  const { customer, logout } = useAuth()

  return (
    <main className="page wide">
      <header className="topbar">
        <div className="topbar-left">
          <Link to="/" className="brand-link">
            <p className="brand">Feather Bank</p>
          </Link>
          <nav className="nav-links">
            <NavLink to="/" end>
              Accounts
            </NavLink>
            <NavLink to="/transfer">Transfer</NavLink>
          </nav>
          {customer ? <span className="customer-chip">{customer.fullName}</span> : null}
        </div>
        <button type="button" onClick={() => void logout()}>
          Sign out
        </button>
      </header>
      {children}
    </main>
  )
}

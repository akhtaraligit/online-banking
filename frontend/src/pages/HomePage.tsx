import { useEffect, useState } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { getAccounts } from '../api/accounts'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { AppShell } from '../components/AppShell'
import type { AccountSummary } from '../types/accounts'
import { accountTypeLabel, formatMoney, maskAccountNumber } from '../utils/format'

export function HomePage() {
  const { customer, token, loading } = useAuth()
  const [accounts, setAccounts] = useState<AccountSummary[]>([])
  const [error, setError] = useState<string | null>(null)
  const [loadingAccounts, setLoadingAccounts] = useState(true)

  useEffect(() => {
    if (!token) return

    let cancelled = false

    async function load() {
      setLoadingAccounts(true)
      setError(null)
      try {
        const data = await getAccounts(token!)
        if (!cancelled) setAccounts(data)
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : 'Failed to load accounts.')
        }
      } finally {
        if (!cancelled) setLoadingAccounts(false)
      }
    }

    void load()
    return () => {
      cancelled = true
    }
  }, [token])

  if (loading) {
    return <main className="page">Loading session…</main>
  }

  if (!customer || !token) {
    return <Navigate to="/login" replace />
  }

  return (
    <AppShell>
      <section className="panel">
        <div className="panel-heading">
          <div>
            <h1>Your accounts</h1>
            <p className="muted">Balances for checking and savings you can open for history.</p>
          </div>
          <Link to="/transfer" className="button-link">
            Transfer money
          </Link>
        </div>

        {error ? <p className="error">{error}</p> : null}
        {loadingAccounts ? <p className="muted">Loading accounts…</p> : null}

        {!loadingAccounts && accounts.length === 0 ? (
          <p className="muted">No accounts found for this customer.</p>
        ) : null}

        <div className="account-grid">
          {accounts.map((account) => (
            <Link key={account.id} to={`/accounts/${account.id}`} className="account-card">
              <div className="account-card-top">
                <span className="pill">{accountTypeLabel(account.type)}</span>
                <span className="muted">{maskAccountNumber(account.accountNumber)}</span>
              </div>
              <h2>{account.name}</h2>
              <p className="balance">{formatMoney(account.balance, account.currency)}</p>
            </Link>
          ))}
        </div>
      </section>
    </AppShell>
  )
}

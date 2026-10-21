import { useEffect, useState, type FormEvent } from 'react'
import { Link, Navigate, useParams } from 'react-router-dom'
import { getAccount, getTransactions } from '../api/accounts'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { AppShell } from '../components/AppShell'
import type { AccountDetail, PagedTransactions } from '../types/accounts'
import {
  accountTypeLabel,
  formatDate,
  formatMoney,
  isDebit,
  maskAccountNumber,
} from '../utils/format'

type Filters = {
  from: string
  to: string
  type: 'Debit' | 'Credit' | ''
  search: string
}

const emptyFilters: Filters = { from: '', to: '', type: '', search: '' }

export function AccountDetailPage() {
  const { accountId } = useParams<{ accountId: string }>()
  const { customer, token, loading } = useAuth()

  const [account, setAccount] = useState<AccountDetail | null>(null)
  const [transactions, setTransactions] = useState<PagedTransactions | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loadingData, setLoadingData] = useState(true)

  const [draft, setDraft] = useState<Filters>(emptyFilters)
  const [filters, setFilters] = useState<Filters>(emptyFilters)
  const [page, setPage] = useState(1)

  useEffect(() => {
    if (!token || !accountId) return

    let cancelled = false

    async function load() {
      setLoadingData(true)
      setError(null)

      try {
        const [accountData, txData] = await Promise.all([
          getAccount(token!, accountId!),
          getTransactions(token!, accountId!, {
            from: filters.from || undefined,
            to: filters.to || undefined,
            type: filters.type || undefined,
            search: filters.search || undefined,
            page,
            pageSize: 5,
          }),
        ])

        if (!cancelled) {
          setAccount(accountData)
          setTransactions(txData)
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : 'Failed to load account.')
          setAccount(null)
          setTransactions(null)
        }
      } finally {
        if (!cancelled) setLoadingData(false)
      }
    }

    void load()
    return () => {
      cancelled = true
    }
  }, [token, accountId, filters, page])

  if (loading) {
    return <main className="page">Loading session…</main>
  }

  if (!customer || !token) {
    return <Navigate to="/login" replace />
  }

  function handleFilterSubmit(event: FormEvent) {
    event.preventDefault()
    setPage(1)
    setFilters({
      ...draft,
      search: draft.search.trim(),
    })
  }

  return (
    <AppShell>
      <p className="back-link">
        <Link to="/">← All accounts</Link>
      </p>

      {error ? <p className="error">{error}</p> : null}
      {loadingData && !account ? <p className="muted">Loading account…</p> : null}

      {account ? (
        <>
          <section className="panel">
            <div className="account-card-top">
              <span className="pill">{accountTypeLabel(account.type)}</span>
              <span className="muted">{maskAccountNumber(account.accountNumber)}</span>
            </div>
            <h1>{account.name}</h1>
            <p className="balance">{formatMoney(account.balance, account.currency)}</p>
          </section>

          <section className="panel">
            <h2>Transaction history</h2>
            <p className="muted">Newest first. Filter by date, type, or description.</p>

            <form className="filters" onSubmit={handleFilterSubmit}>
              <label>
                From
                <input
                  type="date"
                  value={draft.from}
                  onChange={(e) => setDraft((prev) => ({ ...prev, from: e.target.value }))}
                />
              </label>
              <label>
                To
                <input
                  type="date"
                  value={draft.to}
                  onChange={(e) => setDraft((prev) => ({ ...prev, to: e.target.value }))}
                />
              </label>
              <label>
                Type
                <select
                  value={draft.type}
                  onChange={(e) =>
                    setDraft((prev) => ({
                      ...prev,
                      type: e.target.value as Filters['type'],
                    }))
                  }
                >
                  <option value="">All</option>
                  <option value="Credit">Credit</option>
                  <option value="Debit">Debit</option>
                </select>
              </label>
              <label className="grow">
                Search
                <input
                  value={draft.search}
                  onChange={(e) => setDraft((prev) => ({ ...prev, search: e.target.value }))}
                  placeholder="e.g. grocery"
                />
              </label>
              <button type="submit" className="secondary">
                Apply
              </button>
            </form>

            {loadingData ? <p className="muted">Updating history…</p> : null}

            {transactions && transactions.items.length === 0 ? (
              <p className="muted">No transactions match these filters.</p>
            ) : null}

            <div className="tx-list">
              {transactions?.items.map((tx) => {
                const debit = isDebit(tx.type)
                return (
                  <article key={tx.id} className="tx-row">
                    <div>
                      <strong>{tx.description}</strong>
                      <p className="muted">{formatDate(tx.occurredAt)}</p>
                    </div>
                    <div className="tx-amounts">
                      <span className={debit ? 'debit' : 'credit'}>
                        {debit ? '−' : '+'}
                        {formatMoney(tx.amount, account.currency)}
                      </span>
                      {tx.runningBalance != null ? (
                        <span className="muted">
                          Bal {formatMoney(tx.runningBalance, account.currency)}
                        </span>
                      ) : null}
                    </div>
                  </article>
                )
              })}
            </div>

            {transactions && transactions.totalPages > 1 ? (
              <div className="pager">
                <button
                  type="button"
                  className="secondary"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                >
                  Previous
                </button>
                <span className="muted">
                  Page {transactions.page} of {transactions.totalPages}
                </span>
                <button
                  type="button"
                  className="secondary"
                  disabled={page >= transactions.totalPages}
                  onClick={() => setPage((p) => p + 1)}
                >
                  Next
                </button>
              </div>
            ) : null}
          </section>
        </>
      ) : null}
    </AppShell>
  )
}

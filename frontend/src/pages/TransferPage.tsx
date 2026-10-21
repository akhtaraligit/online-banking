import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Link, Navigate } from 'react-router-dom'
import { getAccounts } from '../api/accounts'
import { ApiError } from '../api/client'
import { createTransfer, getPayees } from '../api/transfers'
import { useAuth } from '../auth/AuthContext'
import { AppShell } from '../components/AppShell'
import type { AccountSummary } from '../types/accounts'
import type { Payee, TransferDestinationType, TransferResult } from '../types/transfers'
import { formatMoney, maskAccountNumber } from '../utils/format'

type Step = 'form' | 'confirm' | 'result'

type FormState = {
  sourceAccountId: string
  destinationType: TransferDestinationType
  destinationAccountId: string
  payeeId: string
  amount: string
  note: string
}

const emptyForm: FormState = {
  sourceAccountId: '',
  destinationType: 'OwnAccount',
  destinationAccountId: '',
  payeeId: '',
  amount: '',
  note: '',
}

function newIdempotencyKey() {
  return crypto.randomUUID()
}

export function TransferPage() {
  const { customer, token, loading } = useAuth()
  const [accounts, setAccounts] = useState<AccountSummary[]>([])
  const [payees, setPayees] = useState<Payee[]>([])
  const [loadError, setLoadError] = useState<string | null>(null)
  const [form, setForm] = useState<FormState>(emptyForm)
  const [step, setStep] = useState<Step>('form')
  const [idempotencyKey, setIdempotencyKey] = useState(newIdempotencyKey)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<TransferResult | null>(null)

  useEffect(() => {
    if (!token) return

    let cancelled = false

    async function load() {
      setLoadError(null)
      try {
        const [accountData, payeeData] = await Promise.all([
          getAccounts(token!),
          getPayees(token!),
        ])
        if (cancelled) return

        setAccounts(accountData)
        setPayees(payeeData)
        setForm((prev) => ({
          ...prev,
          sourceAccountId: prev.sourceAccountId || accountData[0]?.id || '',
          destinationAccountId:
            prev.destinationAccountId ||
            accountData.find((a) => a.id !== (prev.sourceAccountId || accountData[0]?.id))?.id ||
            '',
          payeeId: prev.payeeId || payeeData[0]?.id || '',
        }))
      } catch (err) {
        if (!cancelled) {
          setLoadError(err instanceof ApiError ? err.message : 'Failed to load transfer data.')
        }
      }
    }

    void load()
    return () => {
      cancelled = true
    }
  }, [token])

  const sourceAccount = useMemo(
    () => accounts.find((a) => a.id === form.sourceAccountId) ?? null,
    [accounts, form.sourceAccountId],
  )

  const destinationAccount = useMemo(
    () => accounts.find((a) => a.id === form.destinationAccountId) ?? null,
    [accounts, form.destinationAccountId],
  )

  const selectedPayee = useMemo(
    () => payees.find((p) => p.id === form.payeeId) ?? null,
    [payees, form.payeeId],
  )

  const destinationOptions = useMemo(
    () => accounts.filter((a) => a.id !== form.sourceAccountId),
    [accounts, form.sourceAccountId],
  )

  const parsedAmount = Number(form.amount)

  if (loading) {
    return <main className="page">Loading session…</main>
  }

  if (!customer || !token) {
    return <Navigate to="/login" replace />
  }

  function validateForm(): string | null {
    if (!form.sourceAccountId) return 'Choose a source account.'
    if (!Number.isFinite(parsedAmount) || parsedAmount <= 0) {
      return 'Enter an amount greater than zero.'
    }
    if (Math.round(parsedAmount * 100) / 100 !== parsedAmount) {
      return 'Amount can have at most two decimal places.'
    }
    if (sourceAccount && parsedAmount > sourceAccount.balance) {
      return 'Insufficient funds.'
    }
    if (form.destinationType === 'OwnAccount') {
      if (!form.destinationAccountId) return 'Choose a destination account.'
      if (form.destinationAccountId === form.sourceAccountId) {
        return 'Source and destination must be different.'
      }
    } else if (!form.payeeId) {
      return 'Choose a saved payee.'
    }
    return null
  }

  function handleReview(event: FormEvent) {
    event.preventDefault()
    const validationError = validateForm()
    if (validationError) {
      setError(validationError)
      return
    }
    setError(null)
    setStep('confirm')
  }

  async function handleConfirm() {
    if (submitting) return

    const validationError = validateForm()
    if (validationError) {
      setError(validationError)
      setStep('form')
      return
    }

    setSubmitting(true)
    setError(null)

    try {
      const response = await createTransfer(token!, {
        sourceAccountId: form.sourceAccountId,
        destinationType: form.destinationType,
        destinationAccountId:
          form.destinationType === 'OwnAccount' ? form.destinationAccountId : undefined,
        payeeId: form.destinationType === 'Payee' ? form.payeeId : undefined,
        amount: parsedAmount,
        note: form.note.trim() || undefined,
        idempotencyKey,
      })
      setResult(response)
      setStep('result')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Transfer failed.')
      setStep('form')
    } finally {
      setSubmitting(false)
    }
  }

  function startAnother() {
    setForm((prev) => ({
      ...emptyForm,
      sourceAccountId: accounts[0]?.id || '',
      destinationAccountId: accounts[1]?.id || '',
      payeeId: payees[0]?.id || '',
      destinationType: prev.destinationType,
    }))
    setResult(null)
    setError(null)
    setIdempotencyKey(newIdempotencyKey())
    setStep('form')
  }

  return (
    <AppShell>
      <p className="back-link">
        <Link to="/">← All accounts</Link>
      </p>

      <section className="panel">
        <h1>Transfer money</h1>
        <p className="muted">
          Move funds between your accounts or send to a saved payee. You will confirm before
          anything is submitted.
        </p>

        {loadError ? <p className="error">{loadError}</p> : null}
        {error ? <p className="error">{error}</p> : null}

        {step === 'form' ? (
          <form className="transfer-form" onSubmit={handleReview}>
            <label>
              From account
              <select
                value={form.sourceAccountId}
                onChange={(e) => {
                  const sourceAccountId = e.target.value
                  setForm((prev) => ({
                    ...prev,
                    sourceAccountId,
                    destinationAccountId:
                      prev.destinationAccountId === sourceAccountId
                        ? accounts.find((a) => a.id !== sourceAccountId)?.id || ''
                        : prev.destinationAccountId,
                  }))
                }}
                required
              >
                {accounts.map((account) => (
                  <option key={account.id} value={account.id}>
                    {account.name} — {formatMoney(account.balance, account.currency)}
                  </option>
                ))}
              </select>
            </label>

            <fieldset className="dest-type">
              <legend>Destination</legend>
              <label className="radio">
                <input
                  type="radio"
                  name="destinationType"
                  checked={form.destinationType === 'OwnAccount'}
                  onChange={() => setForm((prev) => ({ ...prev, destinationType: 'OwnAccount' }))}
                />
                My other account
              </label>
              <label className="radio">
                <input
                  type="radio"
                  name="destinationType"
                  checked={form.destinationType === 'Payee'}
                  onChange={() => setForm((prev) => ({ ...prev, destinationType: 'Payee' }))}
                />
                Saved payee
              </label>
            </fieldset>

            {form.destinationType === 'OwnAccount' ? (
              <label>
                To account
                <select
                  value={form.destinationAccountId}
                  onChange={(e) =>
                    setForm((prev) => ({ ...prev, destinationAccountId: e.target.value }))
                  }
                  required
                >
                  {destinationOptions.map((account) => (
                    <option key={account.id} value={account.id}>
                      {account.name} — {formatMoney(account.balance, account.currency)}
                    </option>
                  ))}
                </select>
              </label>
            ) : (
              <label>
                Payee
                <select
                  value={form.payeeId}
                  onChange={(e) => setForm((prev) => ({ ...prev, payeeId: e.target.value }))}
                  required
                >
                  {payees.map((payee) => (
                    <option key={payee.id} value={payee.id}>
                      {payee.name} ({payee.bankName})
                    </option>
                  ))}
                </select>
              </label>
            )}

            <label>
              Amount
              <input
                type="number"
                min="0.01"
                step="0.01"
                value={form.amount}
                onChange={(e) => setForm((prev) => ({ ...prev, amount: e.target.value }))}
                placeholder="0.00"
                required
              />
            </label>

            <label>
              Note (optional)
              <input
                value={form.note}
                onChange={(e) => setForm((prev) => ({ ...prev, note: e.target.value }))}
                maxLength={200}
                placeholder="e.g. Monthly savings"
              />
            </label>

            <button type="submit">Review transfer</button>
          </form>
        ) : null}

        {step === 'confirm' && sourceAccount ? (
          <div className="confirm-box">
            <h2>Confirm transfer</h2>
            <dl className="detail-list">
              <div>
                <dt>From</dt>
                <dd>
                  {sourceAccount.name} ({maskAccountNumber(sourceAccount.accountNumber)})
                </dd>
              </div>
              <div>
                <dt>To</dt>
                <dd>
                  {form.destinationType === 'OwnAccount' && destinationAccount
                    ? `${destinationAccount.name} (${maskAccountNumber(destinationAccount.accountNumber)})`
                    : selectedPayee
                      ? `${selectedPayee.name} — ${selectedPayee.bankName}`
                      : '—'}
                </dd>
              </div>
              <div>
                <dt>Amount</dt>
                <dd>{formatMoney(parsedAmount, sourceAccount.currency)}</dd>
              </div>
              {form.note.trim() ? (
                <div>
                  <dt>Note</dt>
                  <dd>{form.note.trim()}</dd>
                </div>
              ) : null}
            </dl>

            <div className="actions">
              <button
                type="button"
                className="secondary"
                disabled={submitting}
                onClick={() => setStep('form')}
              >
                Back
              </button>
              <button type="button" disabled={submitting} onClick={() => void handleConfirm()}>
                {submitting ? 'Submitting…' : 'Confirm & send'}
              </button>
            </div>
          </div>
        ) : null}

        {step === 'result' && result ? (
          <div className="result-box">
            <h2>{result.alreadyProcessed ? 'Already processed' : 'Transfer complete'}</h2>
            <p className={result.alreadyProcessed ? 'muted' : 'success'}>{result.message}</p>
            <dl className="detail-list">
              <div>
                <dt>Amount</dt>
                <dd>{formatMoney(result.amount, result.sourceAccount.currency)}</dd>
              </div>
              <div>
                <dt>From</dt>
                <dd>
                  {result.sourceAccount.name} — balance{' '}
                  {formatMoney(result.sourceAccount.balance, result.sourceAccount.currency)}
                </dd>
              </div>
              <div>
                <dt>To</dt>
                <dd>
                  {result.destinationAccount
                    ? `${result.destinationAccount.name} — balance ${formatMoney(result.destinationAccount.balance, result.destinationAccount.currency)}`
                    : result.payee
                      ? `${result.payee.name} (${result.payee.bankName})`
                      : '—'}
                </dd>
              </div>
            </dl>
            <div className="actions">
              <Link className="button-link" to={`/accounts/${result.sourceAccount.id}`}>
                View source history
              </Link>
              <button type="button" onClick={startAnother}>
                New transfer
              </button>
            </div>
          </div>
        ) : null}
      </section>
    </AppShell>
  )
}

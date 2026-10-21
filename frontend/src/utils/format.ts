import type { AccountType, TransactionType } from '../types/accounts'

export function formatMoney(amount: number, currency = 'USD') {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
  }).format(amount)
}

export function formatDate(value: string) {
  return new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  }).format(new Date(value))
}

export function accountTypeLabel(type: AccountType) {
  if (type === 0 || type === 'Checking') return 'Checking'
  if (type === 1 || type === 'Savings') return 'Savings'
  return String(type)
}

export function isDebit(type: TransactionType) {
  return type === 0 || type === 'Debit'
}

export function maskAccountNumber(accountNumber: string) {
  if (accountNumber.length <= 4) return accountNumber
  return `•••• ${accountNumber.slice(-4)}`
}

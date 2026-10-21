export type AccountType = 'Checking' | 'Savings' | 0 | 1

export type TransactionType = 'Debit' | 'Credit' | 0 | 1

export type AccountSummary = {
  id: string
  name: string
  type: AccountType
  accountNumber: string
  balance: number
  currency: string
}

export type AccountDetail = AccountSummary & {
  createdAt: string
}

export type Transaction = {
  id: string
  occurredAt: string
  description: string
  amount: number
  type: TransactionType
  runningBalance?: number | null
}

export type PagedTransactions = {
  items: Transaction[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type TransactionFilters = {
  from?: string
  to?: string
  type?: 'Debit' | 'Credit' | ''
  search?: string
  page?: number
  pageSize?: number
}

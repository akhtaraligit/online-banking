import { apiRequest } from './client'
import type {
  AccountDetail,
  AccountSummary,
  PagedTransactions,
  TransactionFilters,
} from '../types/accounts'

export function getAccounts(token: string) {
  return apiRequest<AccountSummary[]>('/api/accounts', { method: 'GET' }, token)
}

export function getAccount(token: string, accountId: string) {
  return apiRequest<AccountDetail>(`/api/accounts/${accountId}`, { method: 'GET' }, token)
}

export function getTransactions(
  token: string,
  accountId: string,
  filters: TransactionFilters = {},
) {
  const params = new URLSearchParams()

  if (filters.from) params.set('from', filters.from)
  if (filters.to) params.set('to', filters.to)
  if (filters.type) params.set('type', filters.type)
  if (filters.search) params.set('search', filters.search)
  if (filters.page) params.set('page', String(filters.page))
  if (filters.pageSize) params.set('pageSize', String(filters.pageSize))

  const query = params.toString()
  const path = `/api/accounts/${accountId}/transactions${query ? `?${query}` : ''}`

  return apiRequest<PagedTransactions>(path, { method: 'GET' }, token)
}

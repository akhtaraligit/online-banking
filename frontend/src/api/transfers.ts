import { apiRequest } from './client'
import type { CreateTransferRequest, Payee, TransferResult } from '../types/transfers'

export function getPayees(token: string) {
  return apiRequest<Payee[]>('/api/payees', { method: 'GET' }, token)
}

export function createTransfer(token: string, body: CreateTransferRequest) {
  return apiRequest<TransferResult>(
    '/api/transfers',
    {
      method: 'POST',
      body: JSON.stringify(body),
    },
    token,
  )
}

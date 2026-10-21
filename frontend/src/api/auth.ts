import { apiRequest } from './client'
import type { AuthResponse, Customer } from '../types/auth'

export function login(username: string, password: string) {
  return apiRequest<AuthResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ username, password }),
  })
}

export function logout(token: string) {
  return apiRequest<{ message: string }>(
    '/api/auth/logout',
    { method: 'POST' },
    token,
  )
}

export function getMe(token: string) {
  return apiRequest<Customer>('/api/auth/me', { method: 'GET' }, token)
}

export type Customer = {
  id: string
  username: string
  fullName: string
  email: string
}

export type AuthResponse = {
  token: string
  expiresAt: string
  customer: Customer
}

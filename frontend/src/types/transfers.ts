export type TransferDestinationType = 'OwnAccount' | 'Payee'

export type Payee = {
  id: string
  name: string
  accountNumber: string
  bankName: string
}

export type AccountSnapshot = {
  id: string
  name: string
  accountNumber: string
  balance: number
  currency: string
}

export type CreateTransferRequest = {
  sourceAccountId: string
  destinationType: TransferDestinationType
  destinationAccountId?: string
  payeeId?: string
  amount: number
  note?: string
  idempotencyKey: string
}

export type TransferResult = {
  transferId: string
  transferGroupId: string
  alreadyProcessed: boolean
  amount: number
  note?: string | null
  createdAt: string
  destinationType: TransferDestinationType
  sourceAccount: AccountSnapshot
  destinationAccount?: AccountSnapshot | null
  payee?: Payee | null
  message: string
}

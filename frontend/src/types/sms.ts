export type SmsSendStatus = 'Pending' | 'Sending' | 'Sent' | 'Failed' | 'Cancelled'

export interface SmsSendJob {
  id: string
  leadId: string
  status: SmsSendStatus
  scheduledAt: string
  sentAt?: string
  attemptCount: number
  maxAttempts: number
  nextAttemptAt?: string
  errorMessage?: string
  brevoMessageId?: string
  toPhone: string
  toName?: string
  message: string
  createdAt: string
}

export interface SendSmsRequest {
  toPhone: string
  message: string
  scheduledAt?: string
}

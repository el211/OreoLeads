export type EmailDraftStatus = 'Generated' | 'Edited' | 'Approved' | 'Rejected' | 'Sent'
export type EmailStyle = 'Professional' | 'Friendly' | 'Premium' | 'Short' | 'Luxury' | 'French' | 'English'
export type EmailLength = 'Short' | 'Normal' | 'Long'
export type EmailType = 'FirstContact' | 'FollowUp' | 'Reply' | 'Proposal' | 'AfterMeeting' | 'LastFollowUp'
export type AiProviderType = 'Claude' | 'OpenAI' | 'Ollama' | 'GenericOpenAI'

export interface AiProviderInfo {
  name: string
  type: AiProviderType
  requiresApiKey: boolean
  requiresBaseUrl: boolean
  defaultModels: string[]
  description: string
}

export interface AiConfiguration {
  id?: string
  providerType: AiProviderType
  hasApiKey: boolean
  model?: string
  temperature: number
  maxTokens: number
  timeoutSeconds: number
  isEnabled: boolean
  baseUrl?: string
  updatedAt?: string
}

export interface UpdateAiConfiguration {
  providerType: AiProviderType
  apiKey?: string
  model?: string
  temperature: number
  maxTokens: number
  timeoutSeconds: number
  isEnabled: boolean
  baseUrl?: string
}

export interface AiTestResult {
  success: boolean
  message: string
  latencyMs: number
  model?: string
}

export interface EmailDraft {
  id: string
  leadId: string
  companyName: string
  subject: string
  body: string
  summary?: string
  callToAction?: string
  status: EmailDraftStatus
  statusLabel: string
  style: EmailStyle
  length: EmailLength
  type: EmailType
  typeLabel: string
  providerUsed: string
  modelUsed: string
  promptTokens: number
  completionTokens: number
  totalTokens: number
  generationMs: number
  currentVersion: number
  businessScore?: number
  createdAt: string
  updatedAt?: string
}

export interface EmailDraftVersion {
  id: string
  emailDraftId: string
  version: number
  subject: string
  body: string
  providerUsed: string
  modelUsed: string
  generationMs: number
  createdAt: string
}

export interface EmailDraftWithVersions {
  draft: EmailDraft
  versions: EmailDraftVersion[]
}

export interface GenerateEmailRequest {
  style: EmailStyle
  length: EmailLength
  type: EmailType
  customInstructions?: string
}

export interface PromptTemplate {
  id: string
  name: string
  key: string
  content: string
  description?: string
  emailType?: EmailType
  isSystem: boolean
  updatedAt: string
}

export interface AiStats {
  totalGenerations: number
  totalTokens: number
  averageGenerationMs: number
  generationsByProvider: Record<string, number>
}

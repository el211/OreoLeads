export type EnrichmentStatus = 'Pending' | 'Running' | 'Completed' | 'NeedsReview' | 'Failed'

export interface WebsiteCandidate {
  url: string
  score: number
  category: string
  signals: string[]
}

export interface ExternalProfile {
  url: string
  category: string
}

export interface LeadEnrichment {
  id: string
  leadId: string
  status: EnrichmentStatus
  scheduledAt: string
  startedAt?: string
  completedAt?: string
  attemptCount: number
  errorMessage?: string

  chosenWebsiteUrl?: string
  websiteConfidence?: number
  matchedSignals: string[]
  candidates: WebsiteCandidate[]
  externalProfiles: ExternalProfile[]
  autoApplied: boolean

  discoveredEmail?: string
  emailSourceUrl?: string
  emailSourceType?: string
  emailKind: string
  emailConfidence?: number
  guessedEmail?: string

  searchQueriesUsed: number
  validatedAt?: string
  createdAt: string
}

export interface EnrichmentValidateRequest {
  website?: string
  email?: string
  acceptWebsite: boolean
  acceptEmail: boolean
}

export interface EnrichmentQueueResult {
  enrichmentId: string
  status: string
}

export interface WebsiteAnalysisDto {
  id: string
  leadId: string
  url: string
  lastAnalysis: string
  createdAt: string

  // HTTP
  httpStatus: number
  responseTimeMs: number
  usesHttps: boolean
  certificateValid: boolean
  redirectCount: number

  // SEO
  pageTitle?: string
  metaDescription?: string
  hasViewport: boolean

  // Features
  hasContactForm: boolean
  hasQuoteForm: boolean
  hasBookingSystem: boolean
  hasChatWidget: boolean

  // Visibilité
  hasEmailVisible: boolean
  hasPhoneVisible: boolean
  hasAddressVisible: boolean

  // Conformité
  hasPrivacyPolicy: boolean
  hasLegalNotice: boolean

  // Technologies
  cmsDetected?: string
  technologies: string[]

  // Score
  businessScore: number
  summary?: string
  opportunities: string[]
  oreoServicesRecommended: string[]
  analysisError?: string
}

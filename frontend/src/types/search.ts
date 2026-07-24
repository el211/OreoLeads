export interface CompanySearchRequest {
  keywords?: string
  region?: string
  department?: string
  city?: string
  postalCode?: string
  industry?: string
  nafCode?: string
  activeOnly: boolean
  maxResults: number
  // Filtres entrepreneurs individuels / petites structures
  includeIndividualEntrepreneurs?: boolean
  onlyIndividualEntrepreneurs?: boolean
  includeNoEmployees?: boolean
  natureJuridiqueCodes?: string[]
  createdAfter?: string
  createdBefore?: string
}

export interface CompanySearchResult {
  companyName: string
  siren?: string
  siret?: string
  industry?: string
  nafCode?: string
  address?: string
  postalCode?: string
  city?: string
  department?: string
  region?: string
  phone?: string
  email?: string
  website?: string
  latitude?: number
  longitude?: number
  employeeCount?: number
  isActive: boolean
  provider: string
  alreadyExists: boolean
  existingLeadId?: string
  // Entrepreneurs individuels
  tradeName?: string
  isIndividualEntrepreneur?: boolean
  entrepreneurFirstName?: string
  entrepreneurLastName?: string
  natureJuridique?: string
  creationDate?: string
  isNonDiffusible?: boolean
}

export interface AiSearchParseResult {
  request: CompanySearchRequest
  wantsWebsite?: boolean | null
  wantsEmail?: boolean | null
  interpretation?: string | null
}

export interface CompanySearchResponse {
  searchId: string
  totalFound: number
  page: number
  totalPages: number
  results: CompanySearchResult[]
  provider: string
  durationMs: number
}

export interface SearchImportRequest {
  searchId?: string
  companies: CompanySearchResult[]
}

export interface SearchImportResult {
  newLeads: number
  updatedLeads: number
  duplicates: number
  errors: number
  newLeadIds: string[]
}

export interface SearchHistory {
  id: string
  keywords?: string
  region?: string
  department?: string
  city?: string
  postalCode?: string
  industry?: string
  nafCode?: string
  activeOnly: boolean
  maxResults: number
  provider: string
  durationMs: number
  totalFound: number
  newLeads: number
  updatedLeads: number
  duplicates: number
  errors: number
  status: string
  createdAt: string
}

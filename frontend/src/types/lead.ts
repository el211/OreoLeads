export type LeadStatus =
  | 'New' | 'Qualified' | 'ReadyToContact' | 'EmailPrepared'
  | 'EmailSent' | 'FollowUp1' | 'FollowUp2' | 'Meeting'
  | 'ProposalSent' | 'Client' | 'Rejected' | 'DoNotContact'

export type LeadPriority = 'Low' | 'Medium' | 'High' | 'Urgent'

export type ActivityType =
  | 'Created' | 'Updated' | 'StatusChanged' | 'EmailGenerated'
  | 'EmailSent' | 'PhoneCall' | 'Meeting' | 'WebsiteAnalyzed'
  | 'NoteAdded' | 'NoteUpdated' | 'NoteDeleted' | 'FollowUpCreated'
  | 'Import' | 'Export'

export type FollowUpStatus = 'Pending' | 'Done' | 'Cancelled' | 'Rescheduled'

export interface Tag {
  id: string
  name: string
  color: string
}

export interface LeadSummary {
  id: string
  companyName: string
  tradeName?: string
  industry?: string
  city?: string
  department?: string
  region?: string
  email?: string
  phone?: string
  website?: string
  status: LeadStatus
  statusLabel: string
  priority: LeadPriority
  priorityLabel: string
  score: number
  tags: Tag[]
  createdAt: string
  updatedAt?: string
  lastEmailSentAt?: string
}

export interface Lead extends LeadSummary {
  description?: string
  address?: string
  postalCode?: string
  country: string
  latitude?: number
  longitude?: number
  siren?: string
  siret?: string
  nafCode?: string
  employeeCount?: number
  assignedUserId?: string
  notesCount: number
  activitiesCount: number
  followUpsCount: number
}

export interface LeadActivity {
  id: string
  leadId: string
  type: ActivityType
  typeLabel: string
  description: string
  userId?: string
  metadata?: string
  createdAt: string
}

export interface LeadNote {
  id: string
  leadId: string
  title: string
  content: string
  authorId?: string
  authorName: string
  createdAt: string
  updatedAt?: string
}

export interface FollowUp {
  id: string
  leadId: string
  companyName?: string
  scheduledAt: string
  userId?: string
  userName: string
  comment?: string
  status: FollowUpStatus
  statusLabel: string
  priority: LeadPriority
  priorityLabel: string
  completedAt?: string
  isOverdue: boolean
  createdAt: string
  updatedAt?: string
}

export interface LeadFilter {
  search?: string
  city?: string
  industry?: string
  legalForm?: string
  status?: LeadStatus
  priority?: LeadPriority
  hasWebsite?: boolean
  hasEmail?: boolean
  minDaysSinceEmail?: number
  sortBy?: string
  sortDesc?: boolean
  page: number
  pageSize: number
}

export interface LeadsMeta {
  industries: string[]
  legalForms: string[]
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export interface CreateLeadDto {
  companyName: string
  tradeName?: string
  industry?: string
  description?: string
  website?: string
  email?: string
  phone?: string
  address?: string
  postalCode?: string
  city?: string
  department?: string
  region?: string
  country?: string
  siren?: string
  siret?: string
  nafCode?: string
  employeeCount?: number
  status?: LeadStatus
  priority?: LeadPriority
  score?: number
  tagIds?: string[]
}

export interface DashboardStats {
  totalLeads: number
  newLeads: number
  clients: number
  emailsSent: number
  pendingFollowUps: number
  leadsThisMonth: number
  statusDistribution: { status: string; statusLabel: string; count: number }[]
  industryDistribution: { industry: string; count: number }[]
  cityDistribution: { city: string; count: number }[]
  recentLeads: LeadSummary[]
}

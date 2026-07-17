export type SyncDirection = 'OreoLeadsToAirtable' | 'AirtableToOreoLeads' | 'Bidirectional'
export type ConflictStrategy = 'OreoLeadsWins' | 'AirtableWins' | 'MostRecentWins' | 'ManualResolution'
export type AirtableSyncJobStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed' | 'Conflict' | 'Cancelled'
export type AirtableFieldType =
  | 'SingleLineText'
  | 'MultilineText'
  | 'Email'
  | 'PhoneNumber'
  | 'Url'
  | 'Number'
  | 'Checkbox'
  | 'SingleSelect'
  | 'MultipleSelects'
  | 'Date'
  | 'DateTime'

export interface AirtableConfiguration {
  id: string
  connectionName: string
  hasAccessToken: boolean
  baseId: string
  tableIdOrName: string
  isEnabled: boolean
  syncDirection: SyncDirection
  conflictStrategy: ConflictStrategy
  lastSyncAt: string | null
  hasWebhook: boolean
  webhookExpiresAt: string | null
  createdAt: string
  updatedAt: string | null
}

export interface UpdateAirtableConfiguration {
  accessToken?: string
  connectionName: string
  baseId: string
  tableIdOrName: string
  isEnabled: boolean
  syncDirection: SyncDirection
  conflictStrategy: ConflictStrategy
}

export interface AirtableTestResult {
  success: boolean
  message: string
  workspaceName: string | null
  baseName: string | null
}

export interface AirtableTable {
  id: string
  name: string
  description: string | null
}

export interface AirtableField {
  id: string
  name: string
  type: AirtableFieldType
}

export interface AirtableFieldMapping {
  id: string
  airtableConfigurationId: string
  oreoLeadsField: string
  airtableFieldName: string
  airtableFieldType: AirtableFieldType
  direction: SyncDirection
  isRequired: boolean
  defaultValue: string | null
  transformation: string | null
  sortOrder: number
}

export interface SaveAirtableFieldMapping {
  oreoLeadsField: string
  airtableFieldName: string
  airtableFieldType: AirtableFieldType
  direction: SyncDirection
  isRequired: boolean
  defaultValue?: string
  transformation?: string
  sortOrder: number
}

export interface AirtableSyncJob {
  id: string
  airtableConfigurationId: string
  status: AirtableSyncJobStatus
  direction: SyncDirection
  triggerReason: string | null
  isFullSync: boolean
  leadId: string | null
  totalRecords: number
  processedRecords: number
  successRecords: number
  failedRecords: number
  conflictRecords: number
  attemptCount: number
  maxAttempts: number
  startedAt: string | null
  completedAt: string | null
  nextAttemptAt: string | null
  errorMessage: string | null
  createdAt: string
}

export interface AirtableSyncLog {
  id: string
  airtableSyncJobId: string
  leadId: string | null
  airtableRecordId: string | null
  action: string
  details: string | null
  errorMessage: string | null
  success: boolean
  occurredAt: string
}

export interface AirtableConflict {
  id: string
  leadId: string
  leadName: string | null
  airtableConfigurationId: string
  airtableRecordId: string
  lastSyncedAt: string | null
  conflictDetectedAt: string | null
  conflictOreoLeadsData: string | null
  conflictAirtableData: string | null
  airtableModifiedAt: string | null
}

export interface AirtableSyncStats {
  totalSyncs: number
  successfulSyncs: number
  failedSyncs: number
  totalExported: number
  totalImported: number
  activeConflicts: number
  lastSyncAt: string | null
}

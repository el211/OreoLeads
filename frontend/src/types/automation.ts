// Enums
export type TriggerType =
  | 'LeadCreated' | 'LeadUpdated' | 'LeadDeleted' | 'StatusChanged'
  | 'TagAdded' | 'TagRemoved' | 'LeadAssigned' | 'FollowUpCreated' | 'FollowUpDue'
  | 'FollowUpCompleted' | 'EmailSent' | 'EmailDelivered' | 'EmailOpened' | 'EmailClicked'
  | 'EmailReplied' | 'EmailBounced' | 'EmailFailed' | 'AirtableSync' | 'WebhookReceived'
  | 'DateTime' | 'Cron' | 'Delay' | 'Manual' | 'Api'

export type ActionType =
  | 'SendEmail' | 'CreateFollowUp' | 'ChangeStatus' | 'AddTag' | 'RemoveTag'
  | 'CreateNote' | 'CreateActivity' | 'ExportAirtable' | 'ImportAirtable' | 'HttpRequest'
  | 'WebhookPost' | 'WebhookGet' | 'Wait' | 'SetVariable' | 'UpdateVariable'
  | 'CreateLead' | 'UpdateLead' | 'DeleteLead' | 'ExecuteWorkflow' | 'CancelWorkflow'

export type WorkflowStatus = 'Draft' | 'Active' | 'Paused' | 'Archived'
export type ExecutionStatus = 'Pending' | 'Running' | 'Waiting' | 'Completed' | 'Failed' | 'Cancelled' | 'TimedOut' | 'Skipped'
export type QueueItemStatus = 'Pending' | 'Running' | 'Waiting' | 'Retrying' | 'Completed' | 'Failed' | 'Cancelled' | 'TimedOut' | 'Skipped' | 'DeadLetter'

// DTOs
export interface WorkflowSummary {
  id: string
  name: string
  description: string | null
  status: WorkflowStatus
  isEnabled: boolean
  executionCount: number
  lastExecutedAt: string | null
  createdAt: string
}

export interface AutomationWorkflow {
  id: string
  name: string
  description: string | null
  organizationId: string | null
  folderId: string | null
  status: WorkflowStatus
  isEnabled: boolean
  version: number
  maxExecutions: number | null
  concurrencyLimit: number
  timeoutSeconds: number
  tags: string | null
  triggerJson: string | null
  actionsJson: string | null
  variablesJson: string | null
  executionCount: number
  lastExecutedAt: string | null
  createdAt: string
  updatedAt: string | null
}

export interface CreateAutomationWorkflow {
  name: string
  description?: string | null
  folderId?: string | null
  triggerJson?: string | null
  actionsJson?: string | null
  variablesJson?: string | null
  tags?: string | null
  concurrencyLimit?: number
  timeoutSeconds?: number
}

export interface UpdateAutomationWorkflow {
  name: string
  description?: string | null
  folderId?: string | null
  isEnabled: boolean
  triggerJson?: string | null
  actionsJson?: string | null
  variablesJson?: string | null
  tags?: string | null
  concurrencyLimit?: number
  timeoutSeconds?: number
}

export interface ExecutionSummary {
  id: string
  workflowId: string
  workflowName: string
  triggerType: TriggerType
  status: ExecutionStatus
  startedAt: string | null
  completedAt: string | null
  durationMs: number | null
  retryCount: number
}

export interface AutomationExecution {
  id: string
  workflowId: string
  workflowName: string
  triggerType: TriggerType
  status: ExecutionStatus
  startedAt: string | null
  completedAt: string | null
  durationMs: number | null
  errorMessage: string | null
  retryCount: number
  logs: ExecutionLog[]
  errors: ExecutionError[]
}

export interface ExecutionLog {
  id: string
  executionId: string
  actionName: string | null
  message: string
  level: string
  timestamp: string
  data: string | null
}

export interface ExecutionError {
  id: string
  executionId: string
  actionName: string | null
  errorType: string
  message: string
  stackTrace: string | null
  occurredAt: string
  isRetryable: boolean
}

export interface AutomationExecutionResult {
  success: boolean
  executionId: string | null
  message: string
  errors: string[]
}

export interface MonitoringStats {
  totalWorkflows: number
  activeWorkflows: number
  totalExecutions: number
  successfulExecutions: number
  failedExecutions: number
  averageSuccessRate: number
  averageDurationMs: number
  queueDepth: number
  activeJobs: number
  failedJobs: number
  deadLetterCount: number
}

export interface AutomationTemplate {
  id: string
  name: string
  description: string | null
  category: string
  triggerJson: string | null
  actionsJson: string | null
  isBuiltIn: boolean
  iconName: string | null
  tags: string | null
}

export interface AutomationVersion {
  id: string
  workflowId: string
  versionNumber: number
  snapshot: string
  comment: string | null
  createdBy: string | null
  createdAt: string
}

export interface AutomationQueueItem {
  id: string
  workflowId: string
  status: QueueItemStatus
  priority: number
  payload: string | null
  scheduledAt: string
  startedAt: string | null
  completedAt: string | null
  retryCount: number
  maxRetries: number
  errorMessage: string | null
}

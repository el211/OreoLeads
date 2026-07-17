// ── Enums ────────────────────────────────────────────────────────────────────

export type DateRangePreset = 'Today' | 'Yesterday' | 'Last7Days' | 'Last30Days' | 'Last90Days' | 'ThisYear' | 'Custom'
export type WidgetType = 'KpiCard' | 'LineChart' | 'AreaChart' | 'BarChart' | 'PieChart' | 'DonutChart' | 'FunnelChart' | 'Table' | 'Heatmap'
export type ReportFrequency = 'Daily' | 'Weekly' | 'Monthly'
export type ReportFormat = 'Csv' | 'Excel' | 'Pdf'
export type ReportStatus = 'Pending' | 'Running' | 'Completed' | 'Failed'

// ── Executive Dashboard ──────────────────────────────────────────────────────

export interface ExecutiveDashboard {
  leads: LeadStats
  emails: EmailStats
  automation: AutomationStats
  airtable: AirtableStats
  pendingFollowUps: number
  userActivity: TopUserActivity[]
  generatedAt: string
}

export interface LeadStats {
  today: number
  thisWeek: number
  thisMonth: number
  thisYear: number
  newProspects: number
  converted: number
  conversionRate: number
}

export interface EmailStats {
  sent: number
  delivered: number
  opened: number
  clicked: number
  replied: number
  bounced: number
  unsubscribed: number
  openRate: number
  clickRate: number
  replyRate: number
  bounceRate: number
}

export interface AutomationStats {
  totalExecutions: number
  successful: number
  failed: number
  retried: number
  successRate: number
  averageDurationMs: number
}

export interface AirtableStats {
  totalSyncs: number
  successful: number
  failed: number
  conflicts: number
  successRate: number
}

export interface TopUserActivity {
  userId: string
  userName: string | null
  actionCount: number
}

// ── KPI ──────────────────────────────────────────────────────────────────────

export interface KpiSummary {
  conversionRate: number
  replyRate: number
  openRate: number
  clickRate: number
  bounceRate: number
  leadVelocity: number
  averageResponseTimeHours: number
  averageConversionTimeDays: number
  automationSuccessRate: number
  automationFailureRate: number
  automationRetryRate: number
  airtableSyncSuccess: number
  emailsPerDay: number
  leadsPerDay: number
  workflowsPerDay: number
}

// ── Email Analytics ──────────────────────────────────────────────────────────

export interface EmailAnalytics {
  openRate: number
  clickRate: number
  replyRate: number
  bounceRate: number
  spamRate: number
  unsubscribeRate: number
  topCampaigns: CampaignStats[]
  bestHourOfDay: number | null
  bestDayOfWeek: string | null
  averageMinutesToOpen: number
  averageMinutesToReply: number
  dailyStats: TimeSeriesPoint[]
}

export interface CampaignStats {
  name: string
  sent: number
  opened: number
  clicked: number
  openRate: number
}

// ── Automation Analytics ─────────────────────────────────────────────────────

export interface AutomationAnalytics {
  totalExecutions: number
  successful: number
  failed: number
  retried: number
  averageDurationMs: number
  topActions: ActionUsage[]
  topTriggers: TriggerUsage[]
  mostActiveWorkflows: WorkflowStats[]
  slowestWorkflows: WorkflowStats[]
}

export interface ActionUsage { actionType: string; count: number }
export interface TriggerUsage { triggerType: string; count: number }
export interface WorkflowStats { id: string; name: string; executionCount: number; averageDurationMs: number }

// ── Airtable Analytics ───────────────────────────────────────────────────────

export interface AirtableAnalytics {
  totalImports: number
  totalExports: number
  conflicts: number
  retries: number
  averageDurationMs: number
  recentHistory: SyncHistory[]
}

export interface SyncHistory { syncedAt: string; direction: string; status: string; durationMs: number }

// ── Funnel ───────────────────────────────────────────────────────────────────

export interface Funnel { stages: FunnelStage[] }
export interface FunnelStage { name: string; count: number; conversionRate: number; averageDaysInStage: number; dropoffRate: number }

// ── Time Series ──────────────────────────────────────────────────────────────

export interface TimeSeriesPoint { date: string; value: number; label: string | null }

// ── Monitoring ───────────────────────────────────────────────────────────────

export interface MonitoringStats {
  averageApiResponseMs: number
  averageWorkflowDurationMs: number
  averageSyncDurationMs: number
  averageEmailSendMs: number
  activeBackgroundServices: number
  queueDepth: number
  activeJobs: number
  failedJobs: number
  generatedAt: string
}

// ── Forecast ─────────────────────────────────────────────────────────────────

export interface ForecastPoint { date: string; value: number; confidenceLow: number; confidenceHigh: number }
export interface ForecastSummary {
  leadsForecast: ForecastPoint[]
  conversionsForecast: ForecastPoint[]
  emailsForecast: ForecastPoint[]
  projectedLeadsNextMonth: number
  projectedConversionsNextMonth: number
}

// ── Widget / Dashboard ───────────────────────────────────────────────────────

export interface AnalyticsDashboard {
  id: string
  name: string
  description: string | null
  organizationId: string | null
  userId: string | null
  isDefault: boolean
  layoutJson: string | null
  createdAt: string
  updatedAt: string | null
}

export interface AnalyticsWidget {
  id: string
  dashboardId: string
  title: string
  type: WidgetType
  configJson: string | null
  positionJson: string | null
  sortOrder: number
  isVisible: boolean
  createdAt: string
  updatedAt: string | null
}

export interface SaveDashboardDto { name: string; description?: string; isDefault: boolean }
export interface AddWidgetDto { dashboardId: string; title: string; type: WidgetType; configJson?: string; sortOrder: number }
export interface UpdateWidgetDto { title: string; configJson?: string; positionJson?: string; isVisible: boolean }

// ── Report ───────────────────────────────────────────────────────────────────

export interface AnalyticsReport {
  id: string
  name: string
  description: string | null
  reportType: string
  filterJson: string | null
  status: ReportStatus
  format: ReportFormat
  filePath: string | null
  errorMessage: string | null
  generatedAt: string | null
  createdAt: string
}

export interface AnalyticsScheduledReport {
  id: string
  name: string
  reportType: string
  frequency: ReportFrequency
  recipients: string
  format: ReportFormat
  filterJson: string | null
  isEnabled: boolean
  lastSentAt: string | null
  nextSendAt: string | null
  createdAt: string
}

export interface CreateReportDto { name: string; description?: string; reportType: string; filterJson?: string; format: ReportFormat }
export interface ExportRequestDto { reportType: string; preset: DateRangePreset; from?: string; to?: string; format: ReportFormat }
export interface SaveScheduledReportDto { name: string; reportType: string; frequency: ReportFrequency; recipients: string; format: ReportFormat; filterJson?: string; isEnabled: boolean }

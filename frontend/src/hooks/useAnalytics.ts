import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/api'
import type {
  DateRangePreset,
  ExecutiveDashboard,
  KpiSummary,
  TimeSeriesPoint,
  Funnel,
  EmailAnalytics,
  AutomationAnalytics,
  AirtableAnalytics,
  MonitoringStats,
  AnalyticsDashboard,
  AnalyticsWidget,
  AddWidgetDto,
  UpdateWidgetDto,
  AnalyticsReport,
  CreateReportDto,
  ExportRequestDto,
  AnalyticsScheduledReport,
  SaveScheduledReportDto,
  ForecastSummary,
  ForecastPoint,
} from '@/types/analytics'

// ── Helpers ──────────────────────────────────────────────────────────────────

function rangeParams(preset: DateRangePreset, from?: string, to?: string) {
  const params: Record<string, string> = { preset }
  if (from) params.from = from
  if (to) params.to = to
  return { params }
}

// ── Dashboard ────────────────────────────────────────────────────────────────

export function useExecutiveDashboard(preset: DateRangePreset, from?: string, to?: string) {
  return useQuery<ExecutiveDashboard>({
    queryKey: ['analytics-dashboard', preset, from, to],
    queryFn: async () => (await api.get<ExecutiveDashboard>('/analytics/dashboard', rangeParams(preset, from, to))).data,
  })
}

export function useKpiSummary(preset: DateRangePreset, from?: string, to?: string) {
  return useQuery<KpiSummary>({
    queryKey: ['analytics-kpi', preset, from, to],
    queryFn: async () => (await api.get<KpiSummary>('/analytics/kpi', rangeParams(preset, from, to))).data,
  })
}

export function useLeadTimeSeries(preset: DateRangePreset, from?: string, to?: string) {
  return useQuery<TimeSeriesPoint[]>({
    queryKey: ['analytics-lead-series', preset, from, to],
    queryFn: async () => (await api.get<TimeSeriesPoint[]>('/analytics/leads/series', rangeParams(preset, from, to))).data,
  })
}

export function useEmailTimeSeries(preset: DateRangePreset, from?: string, to?: string) {
  return useQuery<TimeSeriesPoint[]>({
    queryKey: ['analytics-email-series', preset, from, to],
    queryFn: async () => (await api.get<TimeSeriesPoint[]>('/analytics/emails/series', rangeParams(preset, from, to))).data,
  })
}

export function useSalesFunnel(preset: DateRangePreset, from?: string, to?: string) {
  return useQuery<Funnel>({
    queryKey: ['analytics-funnel', preset, from, to],
    queryFn: async () => (await api.get<Funnel>('/analytics/funnel', rangeParams(preset, from, to))).data,
  })
}

export function useEmailAnalytics(preset: DateRangePreset, from?: string, to?: string) {
  return useQuery<EmailAnalytics>({
    queryKey: ['analytics-emails', preset, from, to],
    queryFn: async () => (await api.get<EmailAnalytics>('/analytics/emails', rangeParams(preset, from, to))).data,
  })
}

export function useAutomationAnalytics(preset: DateRangePreset, from?: string, to?: string) {
  return useQuery<AutomationAnalytics>({
    queryKey: ['analytics-automation', preset, from, to],
    queryFn: async () => (await api.get<AutomationAnalytics>('/analytics/automation', rangeParams(preset, from, to))).data,
  })
}

export function useAirtableAnalytics(preset: DateRangePreset, from?: string, to?: string) {
  return useQuery<AirtableAnalytics>({
    queryKey: ['analytics-airtable', preset, from, to],
    queryFn: async () => (await api.get<AirtableAnalytics>('/analytics/airtable', rangeParams(preset, from, to))).data,
  })
}

export function useSystemMonitoring() {
  return useQuery<MonitoringStats>({
    queryKey: ['analytics-monitoring'],
    queryFn: async () => (await api.get<MonitoringStats>('/analytics/monitoring')).data,
    refetchInterval: 15_000,
  })
}

// ── Widgets / Dashboards ─────────────────────────────────────────────────────

export function useDashboards() {
  return useQuery<AnalyticsDashboard[]>({
    queryKey: ['analytics-dashboards'],
    queryFn: async () => (await api.get<AnalyticsDashboard[]>('/analytics/dashboards')).data,
  })
}

export function useWidgets(dashboardId: string | undefined) {
  return useQuery<AnalyticsWidget[]>({
    queryKey: ['analytics-widgets', dashboardId],
    queryFn: async () => (await api.get<AnalyticsWidget[]>(`/analytics/dashboards/${dashboardId}/widgets`)).data,
    enabled: !!dashboardId,
  })
}

export function useAddWidget() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (dto: AddWidgetDto) =>
      (await api.post<AnalyticsWidget>(`/analytics/dashboards/${dto.dashboardId}/widgets`, dto)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['analytics-widgets'] }),
  })
}

export function useUpdateWidget() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, dto }: { id: string; dto: UpdateWidgetDto }) =>
      (await api.put<AnalyticsWidget>(`/analytics/widgets/${id}`, dto)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['analytics-widgets'] }),
  })
}

export function useDeleteWidget() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => api.delete(`/analytics/widgets/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['analytics-widgets'] }),
  })
}

export function useSaveLayout() {
  return useMutation({
    mutationFn: async ({ dashboardId, layoutJson }: { dashboardId: string; layoutJson: string }) =>
      api.put(`/analytics/dashboards/${dashboardId}/layout`, { layoutJson }),
  })
}

// ── Reports ──────────────────────────────────────────────────────────────────

export function useReports() {
  return useQuery<AnalyticsReport[]>({
    queryKey: ['analytics-reports'],
    queryFn: async () => (await api.get<AnalyticsReport[]>('/analytics/reports')).data,
  })
}

export function useCreateReport() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (dto: CreateReportDto) =>
      (await api.post<AnalyticsReport>('/analytics/reports', dto)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['analytics-reports'] }),
  })
}

export function useExportReport() {
  return useMutation({
    mutationFn: async (dto: ExportRequestDto) => {
      const response = await api.post('/analytics/reports/export', dto, { responseType: 'blob' })
      const url = window.URL.createObjectURL(new Blob([response.data]))
      const a = document.createElement('a')
      a.href = url
      const ext = dto.format === 'Csv' ? 'csv' : dto.format === 'Excel' ? 'xlsx' : 'pdf'
      a.download = `report.${ext}`
      a.click()
      window.URL.revokeObjectURL(url)
    },
  })
}

export function useScheduledReports() {
  return useQuery<AnalyticsScheduledReport[]>({
    queryKey: ['analytics-scheduled-reports'],
    queryFn: async () => (await api.get<AnalyticsScheduledReport[]>('/analytics/reports/scheduled')).data,
  })
}

export function useSaveScheduledReport() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (dto: SaveScheduledReportDto) =>
      (await api.post<AnalyticsScheduledReport>('/analytics/reports/scheduled', dto)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['analytics-scheduled-reports'] }),
  })
}

export function useDeleteScheduledReport() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => api.delete(`/analytics/reports/scheduled/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['analytics-scheduled-reports'] }),
  })
}

// ── Forecast ─────────────────────────────────────────────────────────────────

export function useForecastSummary() {
  return useQuery<ForecastSummary>({
    queryKey: ['analytics-forecast-summary'],
    queryFn: async () => (await api.get<ForecastSummary>('/analytics/forecast')).data,
  })
}

export function useForecastLeads(daysAhead: number = 30) {
  return useQuery<ForecastPoint[]>({
    queryKey: ['analytics-forecast-leads', daysAhead],
    queryFn: async () => (await api.get<ForecastPoint[]>('/analytics/forecast/leads', { params: { daysAhead } })).data,
  })
}

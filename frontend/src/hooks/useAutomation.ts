import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/api'
import type {
  WorkflowSummary,
  AutomationWorkflow,
  CreateAutomationWorkflow,
  UpdateAutomationWorkflow,
  ExecutionSummary,
  ExecutionLog,
  AutomationTemplate,
  MonitoringStats,
  AutomationExecutionResult,
  AutomationVersion,
  AutomationQueueItem,
} from '@/types/automation'

// ── Workflows ────────────────────────────────────────────────────────────────

export function useWorkflows() {
  return useQuery<WorkflowSummary[]>({
    queryKey: ['automation-workflows'],
    queryFn: async () => (await api.get<WorkflowSummary[]>('/automation/workflows')).data,
  })
}

export function useWorkflow(id: string | undefined) {
  return useQuery<AutomationWorkflow>({
    queryKey: ['automation-workflow', id],
    queryFn: async () => (await api.get<AutomationWorkflow>(`/automation/workflows/${id}`)).data,
    enabled: !!id,
  })
}

export function useCreateWorkflow() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (dto: CreateAutomationWorkflow) =>
      (await api.post<AutomationWorkflow>('/automation/workflows', dto)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['automation-workflows'] }),
  })
}

export function useUpdateWorkflow() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, dto }: { id: string; dto: UpdateAutomationWorkflow }) =>
      (await api.put<AutomationWorkflow>(`/automation/workflows/${id}`, dto)).data,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['automation-workflows'] })
      qc.invalidateQueries({ queryKey: ['automation-workflow'] })
    },
  })
}

export function useDeleteWorkflow() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => api.delete(`/automation/workflows/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['automation-workflows'] }),
  })
}

export function useExecuteWorkflow() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) =>
      (await api.post<AutomationExecutionResult>(`/automation/workflows/${id}/execute`)).data,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['automation-workflows'] })
      qc.invalidateQueries({ queryKey: ['automation-executions'] })
    },
  })
}

export function useActivateWorkflow() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => api.post(`/automation/workflows/${id}/activate`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['automation-workflows'] }),
  })
}

export function useDeactivateWorkflow() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => api.post(`/automation/workflows/${id}/deactivate`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['automation-workflows'] }),
  })
}

export function useCloneWorkflow() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) =>
      (await api.post<AutomationWorkflow>(`/automation/workflows/${id}/clone`)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['automation-workflows'] }),
  })
}

export function useWorkflowVersions(workflowId: string | undefined) {
  return useQuery<AutomationVersion[]>({
    queryKey: ['automation-versions', workflowId],
    queryFn: async () =>
      (await api.get<AutomationVersion[]>(`/automation/workflows/${workflowId}/versions`)).data,
    enabled: !!workflowId,
  })
}

// ── Executions ───────────────────────────────────────────────────────────────

export function useWorkflowExecutions(workflowId?: string) {
  return useQuery<ExecutionSummary[]>({
    queryKey: ['automation-executions', workflowId],
    queryFn: async () => {
      const params = workflowId ? { workflowId } : {}
      return (await api.get<ExecutionSummary[]>('/automation/executions', { params })).data
    },
    refetchInterval: 10_000,
  })
}

export function useExecutionLogs(executionId: string | undefined) {
  return useQuery<ExecutionLog[]>({
    queryKey: ['automation-execution-logs', executionId],
    queryFn: async () =>
      (await api.get<ExecutionLog[]>(`/automation/executions/${executionId}/logs`)).data,
    enabled: !!executionId,
  })
}

export function useCancelExecution() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => api.post(`/automation/executions/${id}/cancel`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['automation-executions'] }),
  })
}

export function useRetryExecution() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => api.post(`/automation/executions/${id}/retry`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['automation-executions'] }),
  })
}

// ── Templates ────────────────────────────────────────────────────────────────

export function useAutomationTemplates() {
  return useQuery<AutomationTemplate[]>({
    queryKey: ['automation-templates'],
    queryFn: async () => (await api.get<AutomationTemplate[]>('/automation/templates')).data,
  })
}

export function useUseTemplate() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) =>
      (await api.post<AutomationWorkflow>(`/automation/templates/${id}/use`)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['automation-workflows'] }),
  })
}

// ── Monitoring ───────────────────────────────────────────────────────────────

export function useAutomationMonitoring() {
  return useQuery<MonitoringStats>({
    queryKey: ['automation-monitoring'],
    queryFn: async () => (await api.get<MonitoringStats>('/automation/monitoring/stats')).data,
    refetchInterval: 15_000,
  })
}

export function useAutomationQueue() {
  return useQuery<AutomationQueueItem[]>({
    queryKey: ['automation-queue'],
    queryFn: async () => (await api.get<AutomationQueueItem[]>('/automation/monitoring/queue')).data,
    refetchInterval: 10_000,
  })
}

export function useActiveJobs() {
  return useQuery<ExecutionSummary[]>({
    queryKey: ['automation-active-jobs'],
    queryFn: async () => (await api.get<ExecutionSummary[]>('/automation/monitoring/active')).data,
    refetchInterval: 10_000,
  })
}

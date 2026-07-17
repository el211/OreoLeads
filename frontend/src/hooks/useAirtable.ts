import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/api'
import type {
  AirtableConfiguration,
  AirtableConflict,
  AirtableField,
  AirtableFieldMapping,
  AirtableSyncJob,
  AirtableSyncLog,
  AirtableSyncStats,
  AirtableTable,
  AirtableTestResult,
  SaveAirtableFieldMapping,
  UpdateAirtableConfiguration,
} from '@/types/airtable'

// ── Configuration ─────────────────────────────────────────────────────────────

export function useAirtableConfig() {
  return useQuery<AirtableConfiguration>({
    queryKey: ['airtable-config'],
    queryFn: async () => (await api.get<AirtableConfiguration>('/airtable')).data,
  })
}

export function useUpdateAirtableConfig() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (dto: UpdateAirtableConfiguration) =>
      (await api.put<AirtableConfiguration>('/airtable', dto)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['airtable-config'] }),
  })
}

export function useTestAirtableConnection() {
  return useMutation({
    mutationFn: async () => (await api.post<AirtableTestResult>('/airtable/test')).data,
  })
}

export function useAirtableTables() {
  return useQuery<AirtableTable[]>({
    queryKey: ['airtable-tables'],
    queryFn: async () => (await api.get<AirtableTable[]>('/airtable/tables')).data,
    enabled: false,
  })
}

export function useAirtableFields(table: string) {
  return useQuery<AirtableField[]>({
    queryKey: ['airtable-fields', table],
    queryFn: async () =>
      (await api.get<AirtableField[]>('/airtable/fields', { params: { table } })).data,
    enabled: !!table,
  })
}

// ── Mappings ──────────────────────────────────────────────────────────────────

export function useAirtableMappings() {
  return useQuery<AirtableFieldMapping[]>({
    queryKey: ['airtable-mappings'],
    queryFn: async () => (await api.get<AirtableFieldMapping[]>('/airtable/mappings')).data,
  })
}

export function useSaveAirtableMappings() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (mappings: SaveAirtableFieldMapping[]) =>
      (await api.put('/airtable/mappings', mappings)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['airtable-mappings'] }),
  })
}

// ── Stats ─────────────────────────────────────────────────────────────────────

export function useAirtableSyncStats() {
  return useQuery<AirtableSyncStats>({
    queryKey: ['airtable-stats'],
    queryFn: async () => (await api.get<AirtableSyncStats>('/airtable/stats')).data,
    refetchInterval: 30_000,
  })
}

// ── Sync jobs ─────────────────────────────────────────────────────────────────

export function useAirtableSyncJobs() {
  return useQuery<AirtableSyncJob[]>({
    queryKey: ['airtable-sync-jobs'],
    queryFn: async () => (await api.get<AirtableSyncJob[]>('/airtable/sync/jobs')).data,
    refetchInterval: 10_000,
  })
}

export function useAirtableSyncJob(id: string) {
  return useQuery<AirtableSyncJob>({
    queryKey: ['airtable-sync-job', id],
    queryFn: async () => (await api.get<AirtableSyncJob>(`/airtable/sync/jobs/${id}`)).data,
    enabled: !!id,
    refetchInterval: 5_000,
  })
}

export function useAirtableSyncLogs(jobId: string) {
  return useQuery<AirtableSyncLog[]>({
    queryKey: ['airtable-sync-logs', jobId],
    queryFn: async () =>
      (await api.get<AirtableSyncLog[]>(`/airtable/sync/jobs/${jobId}/logs`)).data,
    enabled: !!jobId,
  })
}

export function useEnqueueSync() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async () => (await api.post<AirtableSyncJob>('/airtable/sync')).data,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['airtable-sync-jobs'] })
      qc.invalidateQueries({ queryKey: ['airtable-stats'] })
    },
  })
}

export function useEnqueueFullSync() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async () => (await api.post<AirtableSyncJob>('/airtable/sync/full')).data,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['airtable-sync-jobs'] })
      qc.invalidateQueries({ queryKey: ['airtable-stats'] })
    },
  })
}

export function useCancelSyncJob() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => api.delete(`/airtable/sync/jobs/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['airtable-sync-jobs'] }),
  })
}

// ── Conflicts ─────────────────────────────────────────────────────────────────

export function useAirtableConflicts() {
  return useQuery<AirtableConflict[]>({
    queryKey: ['airtable-conflicts'],
    queryFn: async () => (await api.get<AirtableConflict[]>('/airtable/sync/conflicts')).data,
    refetchInterval: 30_000,
  })
}

export function useResolveConflict() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, winnerSource }: { id: string; winnerSource: string }) =>
      api.post(`/airtable/sync/conflicts/${id}/resolve`, { winnerSource }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['airtable-conflicts'] })
      qc.invalidateQueries({ queryKey: ['airtable-stats'] })
    },
  })
}

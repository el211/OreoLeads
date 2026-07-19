import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/api'
import type {
  AiConfiguration, AiProviderInfo, AiStats, AiTestResult,
  EmailDraft, EmailDraftWithVersions, EmailDraftVersion,
  GenerateEmailRequest, PromptTemplate, UpdateAiConfiguration,
} from '@/types/emails'

// ── AI Config ─────────────────────────────────────────────────────────────────

export function useAiProviders() {
  return useQuery<AiProviderInfo[]>({
    queryKey: ['ai-providers'],
    queryFn: async () => (await api.get<AiProviderInfo[]>('/ai/providers')).data,
    staleTime: Infinity,
  })
}

export function useAiConfig() {
  return useQuery<AiConfiguration>({
    queryKey: ['ai-config'],
    queryFn: async () => (await api.get<AiConfiguration>('/ai/config')).data,
  })
}

export function useUpdateAiConfig() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (dto: UpdateAiConfiguration) =>
      (await api.put<AiConfiguration>('/ai/config', dto)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['ai-config'] }),
  })
}

export function useTestAiConnection() {
  return useMutation({
    mutationFn: async () => (await api.post<AiTestResult>('/ai/test')).data,
  })
}

export function useAiStats() {
  return useQuery<AiStats>({
    queryKey: ['ai-stats'],
    queryFn: async () => (await api.get<AiStats>('/ai/stats')).data,
  })
}

// ── Email Drafts ──────────────────────────────────────────────────────────────

export function useEmailDrafts(page = 1, pageSize = 20, status?: string) {
  return useQuery<{ totalCount: number; page: number; pageSize: number; totalPages: number; items: EmailDraft[] }>({
    queryKey: ['email-drafts', page, pageSize, status],
    queryFn: async () =>
      (await api.get('/emails', { params: { page, pageSize, status } })).data,
  })
}

export function useEmailDraft(id: string) {
  return useQuery<EmailDraftWithVersions>({
    queryKey: ['email-draft', id],
    queryFn: async () => (await api.get<EmailDraftWithVersions>(`/emails/${id}`)).data,
    enabled: !!id,
  })
}

export function useEmailVersions(id: string) {
  return useQuery<EmailDraftVersion[]>({
    queryKey: ['email-draft-versions', id],
    queryFn: async () => (await api.get<EmailDraftVersion[]>(`/emails/${id}/versions`)).data,
    enabled: !!id,
  })
}

export function useUpdateEmailDraft(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (dto: { subject?: string; body?: string }) =>
      (await api.put<EmailDraft>(`/emails/${id}`, dto)).data,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['email-draft', id] })
      qc.invalidateQueries({ queryKey: ['email-drafts'] })
    },
  })
}

export function useSendEmail(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async () => (await api.post(`/emails/${id}/send`, {})).data,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['email-draft', id] })
      qc.invalidateQueries({ queryKey: ['email-drafts'] })
    },
  })
}

export function useApproveEmail(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async () => (await api.post<EmailDraft>(`/emails/${id}/approve`)).data,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['email-draft', id] })
      qc.invalidateQueries({ queryKey: ['email-drafts'] })
    },
  })
}

export function useRejectEmail(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async () => (await api.post<EmailDraft>(`/emails/${id}/reject`)).data,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['email-draft', id] })
      qc.invalidateQueries({ queryKey: ['email-drafts'] })
    },
  })
}

export function useRegenerateEmail(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async () => (await api.post<EmailDraftWithVersions>(`/emails/${id}/regenerate`)).data,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['email-draft', id] })
      qc.invalidateQueries({ queryKey: ['email-draft-versions', id] })
      qc.invalidateQueries({ queryKey: ['email-drafts'] })
    },
  })
}

export function useRestoreEmailVersion(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (version: number) =>
      (await api.post<EmailDraft>(`/emails/${id}/restore/${version}`)).data,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['email-draft', id] })
      qc.invalidateQueries({ queryKey: ['email-drafts'] })
    },
  })
}

// ── Generate from Lead ────────────────────────────────────────────────────────

export function useGenerateEmail(leadId: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (request: GenerateEmailRequest) =>
      (await api.post(`/leads/${leadId}/generate-email`, request)).data,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['email-drafts'] })
    },
  })
}

// ── Prompt Templates ──────────────────────────────────────────────────────────

export function usePromptTemplates() {
  return useQuery<PromptTemplate[]>({
    queryKey: ['prompt-templates'],
    queryFn: async () => (await api.get<PromptTemplate[]>('/prompt-templates')).data,
  })
}

export function useUpdatePromptTemplate(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (dto: { content: string; name?: string; description?: string }) =>
      (await api.put(`/prompt-templates/${id}`, dto)).data,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['prompt-templates'] }),
  })
}

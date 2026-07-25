import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/api'
import type { CreateLeadDto, Lead, LeadFilter, LeadNote, LeadsMeta, LeadSummary, PagedResult } from '@/types/lead'

export function useLeads(filter: LeadFilter) {
  return useQuery<PagedResult<LeadSummary>>({
    queryKey: ['leads', filter],
    queryFn: async () => {
      const params = new URLSearchParams()
      if (filter.search) params.set('search', filter.search)
      if (filter.city) params.set('city', filter.city)
      if (filter.industry) params.set('industry', filter.industry)
      if (filter.status) params.set('status', filter.status)
      if (filter.priority) params.set('priority', filter.priority)
      if (filter.legalForm) params.set('legalForm', filter.legalForm)
      if (filter.hasWebsite !== undefined) params.set('hasWebsite', String(filter.hasWebsite))
      if (filter.hasEmail !== undefined) params.set('hasEmail', String(filter.hasEmail))
      if (filter.minDaysSinceEmail) params.set('minDaysSinceEmail', String(filter.minDaysSinceEmail))
      if (filter.sortBy) params.set('sortBy', filter.sortBy)
      if (filter.sortDesc) params.set('sortDesc', 'true')
      params.set('page', String(filter.page))
      params.set('pageSize', String(filter.pageSize))
      const { data } = await api.get<PagedResult<LeadSummary>>(`/leads?${params}`)
      return data
    },
    placeholderData: keepPreviousData,
  })
}

export function useLeadsMeta() {
  return useQuery<LeadsMeta>({
    queryKey: ['leads-meta'],
    queryFn: async () => (await api.get<LeadsMeta>('/leads/meta')).data,
    staleTime: 5 * 60 * 1000,
  })
}

export function useLead(id: string) {
  return useQuery<Lead>({
    queryKey: ['lead', id],
    queryFn: async () => {
      const { data } = await api.get<Lead>(`/leads/${id}`)
      return data
    },
    enabled: !!id,
  })
}

export function useLeadActivities(leadId: string) {
  return useQuery({
    queryKey: ['lead-activities', leadId],
    queryFn: async () => {
      const { data } = await api.get(`/leads/${leadId}/activities`)
      return data
    },
    enabled: !!leadId,
  })
}

export function useLeadNotes(leadId: string) {
  return useQuery<LeadNote[]>({
    queryKey: ['lead-notes', leadId],
    queryFn: async () => {
      const { data } = await api.get<LeadNote[]>(`/leads/${leadId}/notes`)
      return data
    },
    enabled: !!leadId,
  })
}

export function useCreateLead() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (dto: CreateLeadDto) => {
      const { data } = await api.post<Lead>('/leads', dto)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['leads'] })
    },
  })
}

export function useUpdateLead(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (dto: CreateLeadDto) => {
      await api.put(`/leads/${id}`, dto)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['leads'] })
      queryClient.invalidateQueries({ queryKey: ['lead', id] })
    },
  })
}

export function useDeleteLead() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/leads/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['leads'] })
    },
  })
}

/** Supprime tous les prospects sans e-mail envoyé. Renvoie le nombre supprimé. */
export function useDeleteUncontactedLeads() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async () => {
      const { data } = await api.delete<{ deleted: number }>('/leads/uncontacted')
      return data.deleted
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['leads'] })
      queryClient.invalidateQueries({ queryKey: ['leads-meta'] })
    },
  })
}

/** Compte les prospects sans e-mail envoyé (pour la confirmation). */
export async function fetchUncontactedCount(): Promise<number> {
  const { data } = await api.get<{ count: number }>('/leads/uncontacted/count')
  return data.count
}

/** Met à jour le secteur d'activité de plusieurs prospects en une fois. */
export function useBulkUpdateIndustry() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ leadIds, industry }: { leadIds: string[]; industry: string }) => {
      const { data } = await api.patch<{ updated: number }>('/leads/bulk/industry', { leadIds, industry })
      return data.updated
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['leads'] })
      queryClient.invalidateQueries({ queryKey: ['leads-meta'] })
    },
  })
}

/** Déduit et enregistre le secteur des prospects sélectionnés via l'IA. */
export function useAutofillIndustry() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (leadIds: string[]) => {
      const { data } = await api.post<{ updated: number }>('/leads/bulk/autofill-industry', { leadIds })
      return data.updated
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['leads'] })
      queryClient.invalidateQueries({ queryKey: ['leads-meta'] })
    },
  })
}

export function useCreateNote(leadId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (dto: { title: string; content: string; authorName: string }) => {
      const { data } = await api.post(`/leads/${leadId}/notes`, dto)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lead-notes', leadId] })
      queryClient.invalidateQueries({ queryKey: ['lead', leadId] })
    },
  })
}

export function useDeleteNote(leadId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (noteId: string) => {
      await api.delete(`/leads/${leadId}/notes/${noteId}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lead-notes', leadId] })
    },
  })
}

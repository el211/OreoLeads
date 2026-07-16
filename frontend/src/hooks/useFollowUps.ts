import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/api'
import type { FollowUp, FollowUpStatus, LeadPriority } from '@/types/lead'

export function useFollowUps() {
  return useQuery<FollowUp[]>({
    queryKey: ['followups'],
    queryFn: async () => {
      const { data } = await api.get<FollowUp[]>('/followups')
      return data
    },
  })
}

export function useOverdueFollowUps() {
  return useQuery<FollowUp[]>({
    queryKey: ['followups', 'overdue'],
    queryFn: async () => {
      const { data } = await api.get<FollowUp[]>('/followups/overdue')
      return data
    },
  })
}

export function useLeadFollowUps(leadId: string) {
  return useQuery<FollowUp[]>({
    queryKey: ['lead-followups', leadId],
    queryFn: async () => {
      const { data } = await api.get<FollowUp[]>(`/leads/${leadId}/followups`)
      return data
    },
    enabled: !!leadId,
  })
}

export function useCreateFollowUp(leadId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (dto: { scheduledAt: string; userName: string; comment?: string; priority: LeadPriority }) => {
      const { data } = await api.post<FollowUp>(`/leads/${leadId}/followups`, dto)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lead-followups', leadId] })
      queryClient.invalidateQueries({ queryKey: ['followups'] })
    },
  })
}

export function useUpdateFollowUp() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({
      id,
      dto,
    }: {
      id: string
      dto: { scheduledAt: string; comment?: string; status: FollowUpStatus; priority: LeadPriority }
    }) => {
      await api.put(`/followups/${id}`, dto)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['followups'] })
      queryClient.invalidateQueries({ queryKey: ['lead-followups'] })
    },
  })
}

export function useDeleteFollowUp() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/followups/${id}`)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['followups'] })
    },
  })
}

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/api'
import type { WebsiteAnalysisDto } from '@/types/analysis'

export function useLeadAnalysis(leadId: string) {
  return useQuery<WebsiteAnalysisDto | null>({
    queryKey: ['lead-analysis', leadId],
    queryFn: async () => {
      const { data } = await api.get<WebsiteAnalysisDto | null>(`/leads/${leadId}/analysis`)
      return data
    },
    enabled: !!leadId,
  })
}

export function useLeadAnalysisHistory(leadId: string) {
  return useQuery<WebsiteAnalysisDto[]>({
    queryKey: ['lead-analysis-history', leadId],
    queryFn: async () => {
      const { data } = await api.get<WebsiteAnalysisDto[]>(`/leads/${leadId}/analysis/history`)
      return data
    },
    enabled: !!leadId,
  })
}

export function useRunAnalysis(leadId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async () => {
      const { data } = await api.post<WebsiteAnalysisDto>(`/leads/${leadId}/analysis`)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lead-analysis', leadId] })
      queryClient.invalidateQueries({ queryKey: ['lead-analysis-history', leadId] })
      queryClient.invalidateQueries({ queryKey: ['lead', leadId] })
    },
  })
}

export function useRecalculateAnalysis(leadId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async () => {
      const { data } = await api.post<WebsiteAnalysisDto>(`/leads/${leadId}/analysis/recalculate`)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['lead-analysis', leadId] })
    },
  })
}

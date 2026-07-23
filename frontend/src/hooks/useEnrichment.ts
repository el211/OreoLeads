import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/api'
import type {
  LeadEnrichment,
  EnrichmentValidateRequest,
  EnrichmentQueueResult,
} from '@/types/enrichment'

export function useLeadEnrichments(leadId: string) {
  return useQuery<LeadEnrichment[]>({
    queryKey: ['enrichments', leadId],
    queryFn: async () => {
      const { data } = await api.get<LeadEnrichment[]>(`/enrichment/leads/${leadId}`)
      return data
    },
    // Rafraîchit tant qu'un enrichissement est en attente / en cours
    refetchInterval: query => {
      const latest = query.state.data?.[0]
      return latest && (latest.status === 'Pending' || latest.status === 'Running') ? 5000 : false
    },
  })
}

export function useTriggerEnrichment(leadId: string) {
  const queryClient = useQueryClient()
  return useMutation<EnrichmentQueueResult, Error, boolean>({
    mutationFn: async (force: boolean) => {
      const { data } = await api.post<EnrichmentQueueResult>(
        `/enrichment/leads/${leadId}?force=${force}`
      )
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['enrichments', leadId] })
    },
  })
}

export function useValidateEnrichment(leadId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: EnrichmentValidateRequest }) => {
      const { data } = await api.post<LeadEnrichment>(`/enrichment/${id}/validate`, request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['enrichments', leadId] })
      queryClient.invalidateQueries({ queryKey: ['lead', leadId] })
    },
  })
}

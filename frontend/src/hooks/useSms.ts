import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/api'
import type { SmsSendJob, SendSmsRequest, GenerateSmsRequest, GenerateSmsResponse } from '@/types/sms'

export function useSmsJobs(leadId: string) {
  return useQuery<SmsSendJob[]>({
    queryKey: ['sms-jobs', leadId],
    queryFn: async () => {
      const { data } = await api.get(`/leads/${leadId}/sms`)
      return data
    },
  })
}

export function useSendSms(leadId: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (req: SendSmsRequest) => {
      const { data } = await api.post<SmsSendJob>(`/leads/${leadId}/sms`, req)
      return data
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['sms-jobs', leadId] })
      qc.invalidateQueries({ queryKey: ['lead-activities', leadId] })
    },
  })
}

export function useGenerateSms(leadId: string) {
  return useMutation({
    mutationFn: async (req: GenerateSmsRequest) => {
      const { data } = await api.post<GenerateSmsResponse>(`/leads/${leadId}/generate-sms`, req)
      return data
    },
  })
}

export function useCancelSmsJob() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: async (jobId: string) => {
      const { data } = await api.delete<SmsSendJob>(`/sms-jobs/${jobId}`)
      return data
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['sms-jobs'] })
    },
  })
}

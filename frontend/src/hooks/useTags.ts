import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/api'
import type { Tag } from '@/types/lead'

export function useTags() {
  return useQuery<Tag[]>({
    queryKey: ['tags'],
    queryFn: async () => {
      const { data } = await api.get<Tag[]>('/tags')
      return data
    },
  })
}

export function useCreateTag() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (dto: { name: string; color: string }) => {
      const { data } = await api.post<Tag>('/tags', dto)
      return data
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['tags'] }),
  })
}

export function useDeleteTag() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/tags/${id}`)
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['tags'] }),
  })
}

/** Attache un tag à un prospect. */
export function useAttachTag(leadId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (tagId: string) => {
      await api.post(`/leads/${leadId}/tags/${tagId}`)
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['lead', leadId] }),
  })
}

/** Retire un tag d'un prospect. */
export function useDetachTag(leadId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (tagId: string) => {
      await api.delete(`/leads/${leadId}/tags/${tagId}`)
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['lead', leadId] }),
  })
}

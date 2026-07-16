import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import api from '@/lib/api'
import type {
  CompanySearchRequest,
  CompanySearchResponse,
  SearchImportRequest,
  SearchImportResult,
  SearchHistory,
} from '@/types/search'
import type { PagedResult } from '@/types/lead'

export function useCompanySearch() {
  return useMutation({
    mutationFn: async (request: CompanySearchRequest) => {
      const { data } = await api.post<CompanySearchResponse>('/search', request)
      return data
    },
  })
}

export function useSearchImport() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (request: SearchImportRequest) => {
      const { data } = await api.post<SearchImportResult>('/search/import', request)
      return data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['leads'] })
      queryClient.invalidateQueries({ queryKey: ['search-history'] })
    },
  })
}

export function useSearchHistory(page = 1, pageSize = 20) {
  return useQuery<PagedResult<SearchHistory>>({
    queryKey: ['search-history', page, pageSize],
    queryFn: async () => {
      const { data } = await api.get<PagedResult<SearchHistory>>(
        `/search/history?page=${page}&pageSize=${pageSize}`
      )
      return data
    },
  })
}

import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../api/apiClient'
import { useSession } from '../auth/useSession'
import type { Product } from '../products/types'

export const productSearchQueryKey = ['products', 'search'] as const

/** Owner-scoped product search for recipe ingredient pickers. */
export function useProductSearch(search: string, take = 8) {
  const { session } = useSession()
  const trimmed = search.trim()

  return useQuery({
    queryKey: [...productSearchQueryKey, trimmed, take],
    queryFn: () => {
      const params = new URLSearchParams({ take: String(take) })
      if (trimmed) {
        params.set('search', trimmed)
      }

      return apiClient.get<Product[]>(`/api/products?${params.toString()}`)
    },
    enabled: Boolean(session),
  })
}

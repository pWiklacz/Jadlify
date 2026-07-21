import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../api/apiClient'
import { useSession } from '../auth/useSession'
import type { Product } from './types'

/** Query key for a single product's detail; mutations invalidate the `products` prefix. */
export function productDetailQueryKey(id: string) {
  return ['products', 'detail', id] as const
}

/**
 * Fetches one product from `GET /api/products/{id}` for the detail route. Enabled
 * only when signed in and an id is present, so it never fires on a bare route.
 */
export function useProduct(id: string | undefined) {
  const { session } = useSession()

  return useQuery({
    queryKey: productDetailQueryKey(id ?? ''),
    queryFn: () => apiClient.get<Product>(`/api/products/${id}`),
    enabled: Boolean(session) && Boolean(id),
  })
}

import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../api/apiClient'
import { useSession } from '../auth/useSession'
import type { Product } from './types'

/** Query key for the signed-in user's product list; mutations invalidate it. */
export const productsQueryKey = ['products'] as const

/**
 * Fetches the signed-in user's products from `GET /api/products`. Enabled only
 * when a session exists (mirrors `useMe`), so it never fires while signed out.
 */
export function useProducts() {
  const { session } = useSession()

  return useQuery({
    queryKey: productsQueryKey,
    queryFn: () => apiClient.get<Product[]>('/api/products'),
    enabled: Boolean(session),
  })
}

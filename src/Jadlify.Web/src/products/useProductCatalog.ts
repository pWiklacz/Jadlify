import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { apiClient } from '../api/apiClient'
import { useSession } from '../auth/useSession'
import type { ProductCatalogResponse, ProductCatalogSort } from './types'

/** How many products a catalog page requests (the API caps a page at 100). */
export const CATALOG_PAGE_SIZE = 100

/**
 * The catalog category filter value: `'all'` (no filter), `'None'`
 * (uncategorized products only), or a specific category wire name.
 */
export type CatalogCategoryFilter = 'all' | 'None' | string

export interface ProductCatalogParams {
  search: string
  category: CatalogCategoryFilter
  sort: ProductCatalogSort
}

/** Query key for a catalog page; every filter/sort/page input is part of the key. */
export function productCatalogQueryKey(params: ProductCatalogParams, skip: number) {
  return ['products', 'catalog', { ...params, skip }] as const
}

/**
 * Fetches one owner-scoped page of `GET /api/products/catalog` for the given
 * search / category / sort. The previous page stays visible while a new query is
 * in flight (`keepPreviousData`) so typing in the search box does not flash the
 * grid back to a skeleton on every keystroke.
 */
export function useProductCatalog(params: ProductCatalogParams, skip = 0) {
  const { session } = useSession()

  return useQuery({
    queryKey: productCatalogQueryKey(params, skip),
    queryFn: () => {
      const query = new URLSearchParams()
      const search = params.search.trim()
      if (search) {
        query.set('search', search)
      }
      if (params.category !== 'all') {
        query.set('category', params.category)
      }
      query.set('sort', params.sort)
      query.set('skip', String(skip))
      query.set('take', String(CATALOG_PAGE_SIZE))
      return apiClient.get<ProductCatalogResponse>(`/api/products/catalog?${query.toString()}`)
    },
    enabled: Boolean(session),
    placeholderData: keepPreviousData,
  })
}

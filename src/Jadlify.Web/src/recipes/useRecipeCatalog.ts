import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { apiClient } from '../api/apiClient'
import { useSession } from '../auth/useSession'
import { recipesQueryKey } from './useRecipes'
import type { RecipeCatalogResponse, RecipeCatalogSort } from './types'

/** How many recipes a catalog page requests (the API caps a page at 100). */
export const RECIPE_CATALOG_PAGE_SIZE = 100

export interface RecipeCatalogParams {
  search: string
  sort: RecipeCatalogSort
}

/**
 * Query key for a catalog page; every filter/sort/page input is part of the key.
 * Nested under `recipesQueryKey` so a mutation invalidating the `['recipes']`
 * prefix refreshes the index, the detail and every cached page at once.
 */
export function recipeCatalogQueryKey(params: RecipeCatalogParams, skip: number) {
  return [...recipesQueryKey, 'catalog', { ...params, skip }] as const
}

/**
 * Fetches one owner-scoped page of `GET /api/recipes/catalog` for the given
 * search / sort. The summary shape means an index page never downloads the full
 * ingredient composition of every recipe. The previous page stays visible while
 * a new query is in flight (`keepPreviousData`) so typing in the search box does
 * not flash the grid back to a skeleton on every keystroke.
 */
export function useRecipeCatalog(params: RecipeCatalogParams, skip = 0) {
  const { session } = useSession()

  return useQuery({
    queryKey: recipeCatalogQueryKey(params, skip),
    queryFn: () => {
      const query = new URLSearchParams()
      const search = params.search.trim()
      if (search) {
        query.set('search', search)
      }
      query.set('sort', params.sort)
      query.set('skip', String(skip))
      query.set('take', String(RECIPE_CATALOG_PAGE_SIZE))
      return apiClient.get<RecipeCatalogResponse>(`/api/recipes/catalog?${query.toString()}`)
    },
    enabled: Boolean(session),
    placeholderData: keepPreviousData,
  })
}

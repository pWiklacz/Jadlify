import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../api/apiClient'
import { useSession } from '../auth/useSession'
import type { Recipe } from './types'

export const recipesQueryKey = ['recipes'] as const

export const recipeDetailQueryKey = (id: string) => [...recipesQueryKey, 'detail', id] as const

/** Fetches the signed-in user's saved recipes. */
export function useRecipes() {
  const { session } = useSession()

  return useQuery({
    queryKey: recipesQueryKey,
    queryFn: () => apiClient.get<Recipe[]>('/api/recipes'),
    enabled: Boolean(session),
  })
}

/** Fetches one recipe for editing. */
export function useRecipe(id: string | null) {
  const { session } = useSession()

  return useQuery({
    queryKey: id ? recipeDetailQueryKey(id) : [...recipesQueryKey, 'detail', 'none'],
    queryFn: () => apiClient.get<Recipe>(`/api/recipes/${id}`),
    enabled: Boolean(session && id),
  })
}

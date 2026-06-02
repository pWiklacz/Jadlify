import { useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../api/apiClient'
import { recipeDetailQueryKey, recipesQueryKey } from './useRecipes'
import type { CreatedRecipeResponse, CreateRecipeRequest, UpdateRecipeRequest } from './types'

/** Creates a complete recipe, then refreshes recipe queries. */
export function useCreateRecipe() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: CreateRecipeRequest) =>
      apiClient.post<CreatedRecipeResponse>('/api/recipes', body),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: recipesQueryKey })
    },
  })
}

/** Replaces recipe metadata and ingredient composition, then refreshes recipe queries. */
export function useUpdateRecipe() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: UpdateRecipeRequest }) =>
      apiClient.put<void>(`/api/recipes/${id}`, body),
    onSuccess: (_result, variables) => {
      void queryClient.invalidateQueries({ queryKey: recipesQueryKey })
      void queryClient.invalidateQueries({ queryKey: recipeDetailQueryKey(variables.id) })
    },
  })
}

/** Deletes a recipe, then refreshes recipe queries. */
export function useDeleteRecipe() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => apiClient.del(`/api/recipes/${id}`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: recipesQueryKey })
    },
  })
}

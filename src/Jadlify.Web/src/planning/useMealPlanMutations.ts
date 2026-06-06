import { useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../api/apiClient'
import { shoppingListQueryKey } from '../shopping/useShoppingList'
import { dailyMacroSummaryQueryKey } from './useDailyMacroSummary'
import { mealPlanQueryKey } from './useMealPlan'
import type {
  AddMealPlanEntryRequest,
  CreatedMealPlanEntryResponse,
  UpdateMealPlanEntryRequest,
} from './types'

/** Adds a recipe entry to the selected day and refreshes only that day's plan. */
export function useAddMealPlanEntry() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: AddMealPlanEntryRequest) =>
      apiClient.post<CreatedMealPlanEntryResponse>('/api/meal-plan', body),
    onSuccess: (_result, variables) => {
      void queryClient.invalidateQueries({ queryKey: mealPlanQueryKey(variables.date) })
      void queryClient.invalidateQueries({ queryKey: dailyMacroSummaryQueryKey(variables.date) })
      void queryClient.invalidateQueries({ queryKey: shoppingListQueryKey(variables.date) })
    },
  })
}

/** Updates only meal type and portions, then refreshes the entry's selected day. */
export function useUpdateMealPlanEntry() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (variables: {
      id: string
      date: string
      body: UpdateMealPlanEntryRequest
    }) => apiClient.put<void>(`/api/meal-plan/${variables.id}`, variables.body),
    onSuccess: (_result, variables) => {
      void queryClient.invalidateQueries({ queryKey: mealPlanQueryKey(variables.date) })
      void queryClient.invalidateQueries({ queryKey: dailyMacroSummaryQueryKey(variables.date) })
      void queryClient.invalidateQueries({ queryKey: shoppingListQueryKey(variables.date) })
    },
  })
}

/** Deletes one entry and refreshes only the selected day it came from. */
export function useDeleteMealPlanEntry() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id }: { id: string; date: string }) => apiClient.del(`/api/meal-plan/${id}`),
    onSuccess: (_result, variables) => {
      void queryClient.invalidateQueries({ queryKey: mealPlanQueryKey(variables.date) })
      void queryClient.invalidateQueries({ queryKey: dailyMacroSummaryQueryKey(variables.date) })
      void queryClient.invalidateQueries({ queryKey: shoppingListQueryKey(variables.date) })
    },
  })
}

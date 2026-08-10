import { useMutation, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { apiClient } from '../api/apiClient'
import { shoppingListsBaseKey } from '../shopping/useShoppingLists'
import { mealPlanBaseKey } from './useMealPlanRange'
import type {
  AddMealPlanEntryRequest,
  CopiedMealPlanEntriesResponse,
  CopyMealPlanDayRequest,
  CopyMealPlanEntryRequest,
  CreatedMealPlanEntryResponse,
  MoveMealPlanEntryRequest,
  UpdateMealPlanEntryRequest,
} from './types'

/**
 * Every planner mutation touches the same shared reads, so instead of each hook
 * hand-syncing a handful of keys they all invalidate three prefixes: the whole
 * `meal-plan` tree (every cached range plus the legacy single-day summary) and
 * every shopping read (a plan change makes an existing list potentially stale, so
 * its source-change state and any cached diff preview must be re-derived). One
 * invalidation call keeps day / week / month, the dashboard and the shopping
 * surface consistent.
 */
function invalidatePlannerReads(queryClient: QueryClient) {
  void queryClient.invalidateQueries({ queryKey: mealPlanBaseKey })
  void queryClient.invalidateQueries({ queryKey: ['planning', 'meal-plan-summary'] })
  void queryClient.invalidateQueries({ queryKey: shoppingListsBaseKey })
}

/** Adds one entry (recipe + portions, or product + grams) and refreshes the planner. */
export function useAddMealPlanEntry() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: AddMealPlanEntryRequest) =>
      apiClient.post<CreatedMealPlanEntryResponse>('/api/meal-plan', body),
    onSuccess: () => invalidatePlannerReads(queryClient),
  })
}

/** Updates an entry's meal type and quantity (in its own source's unit). */
export function useUpdateMealPlanEntry() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (variables: { id: string; body: UpdateMealPlanEntryRequest }) =>
      apiClient.put<void>(`/api/meal-plan/${variables.id}`, variables.body),
    onSuccess: () => invalidatePlannerReads(queryClient),
  })
}

/** Deletes one entry. */
export function useDeleteMealPlanEntry() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id }: { id: string }) => apiClient.del(`/api/meal-plan/${id}`),
    onSuccess: () => invalidatePlannerReads(queryClient),
  })
}

/** Moves one entry to a new day and meal type, keeping its id, source and quantity. */
export function useMoveMealPlanEntry() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (variables: { id: string; body: MoveMealPlanEntryRequest }) =>
      apiClient.post<void>(`/api/meal-plan/${variables.id}/move`, variables.body),
    onSuccess: () => invalidatePlannerReads(queryClient),
  })
}

/** Copies one entry onto every target day in a single transactional request. */
export function useCopyMealPlanEntry() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (variables: { id: string; body: CopyMealPlanEntryRequest }) =>
      apiClient.post<CopiedMealPlanEntriesResponse>(
        `/api/meal-plan/${variables.id}/copies`,
        variables.body,
      ),
    onSuccess: () => invalidatePlannerReads(queryClient),
  })
}

/** Copies a whole day onto every target day (`Add` keeps, `Replace` clears existing entries). */
export function useCopyMealPlanDay() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (variables: { date: string; body: CopyMealPlanDayRequest }) =>
      apiClient.post<CopiedMealPlanEntriesResponse>(
        `/api/meal-plan/days/${variables.date}/copies`,
        variables.body,
      ),
    onSuccess: () => invalidatePlannerReads(queryClient),
  })
}

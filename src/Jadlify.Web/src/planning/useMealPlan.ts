import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../api/apiClient'
import { useSession } from '../auth/useSession'
import type { MealPlanEntry } from './types'

export const mealPlanQueryKey = (date: string) => ['planning', 'meal-plan', date] as const

/** Fetches meal-plan entries for one selected ISO date. */
export function useMealPlan(date: string) {
  const { session } = useSession()

  return useQuery({
    queryKey: mealPlanQueryKey(date),
    queryFn: () =>
      apiClient.get<MealPlanEntry[]>(`/api/meal-plan?date=${encodeURIComponent(date)}`),
    enabled: Boolean(session && date),
  })
}

import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { apiClient } from '../api/apiClient'
import { useSession } from '../auth/useSession'
import type { MealPlanRange } from './types'

/** The shared prefix for every meal-plan read; a mutation invalidates it to refresh all ranges. */
export const mealPlanBaseKey = ['planning', 'meal-plan'] as const

/** Query key for one range window. Nested under {@link mealPlanBaseKey} so one prefix covers all. */
export function mealPlanRangeQueryKey(from: string, to: string) {
  return [...mealPlanBaseKey, 'range', from, to] as const
}

/**
 * Fetches one owner-scoped `GET /api/meal-plan/range?from=&to=` window. The day,
 * week and month views all read through this single hook — the range is the only
 * request that varies, so switching the selected day inside the same window hits
 * the cache rather than firing one request per cell. The previous window stays
 * visible while a new one loads (`keepPreviousData`) so paging a week or month
 * shows a background refresh instead of a skeleton flash.
 */
export function useMealPlanRange(from: string, to: string) {
  const { session } = useSession()

  return useQuery({
    queryKey: mealPlanRangeQueryKey(from, to),
    queryFn: () =>
      apiClient.get<MealPlanRange>(
        `/api/meal-plan/range?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`,
      ),
    enabled: Boolean(session && from && to),
    placeholderData: keepPreviousData,
  })
}

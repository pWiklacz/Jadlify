import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../api/apiClient'
import { useSession } from '../auth/useSession'
import type { DailyMacroSummary } from './types'

export const dailyMacroSummaryQueryKey = (date: string) =>
  ['planning', 'meal-plan-summary', date] as const

/** Fetches macro totals and per-entry contributions for one selected ISO date. */
export function useDailyMacroSummary(date: string) {
  const { session } = useSession()

  return useQuery({
    queryKey: dailyMacroSummaryQueryKey(date),
    queryFn: () =>
      apiClient.get<DailyMacroSummary>(
        `/api/meal-plan/summary?date=${encodeURIComponent(date)}`,
      ),
    enabled: Boolean(session && date),
  })
}

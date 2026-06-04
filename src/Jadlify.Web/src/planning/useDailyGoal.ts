import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../api/apiClient'
import { useSession } from '../auth/useSession'
import type { DailyGoal, UpsertDailyGoalRequest } from './types'

export const dailyGoalQueryKey = ['planning', 'daily-goal'] as const

/** Fetches the signed-in user's single current daily macro goal. */
export function useDailyGoal() {
  const { session } = useSession()

  return useQuery({
    queryKey: dailyGoalQueryKey,
    queryFn: () => apiClient.get<DailyGoal | null>('/api/daily-goal'),
    enabled: Boolean(session),
  })
}

/** Replaces the current daily goal and refreshes the singleton query. */
export function useUpsertDailyGoal() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: UpsertDailyGoalRequest) => apiClient.put<void>('/api/daily-goal', body),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: dailyGoalQueryKey })
    },
  })
}

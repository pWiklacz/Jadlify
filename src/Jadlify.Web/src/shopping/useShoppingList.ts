import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../api/apiClient'
import { useSession } from '../auth/useSession'
import type { ShoppingList } from './types'

export const shoppingListQueryKey = (date: string) => ['shopping-list', date] as const

export function useShoppingList(date: string) {
  const { session } = useSession()

  return useQuery({
    queryKey: shoppingListQueryKey(date),
    queryFn: () =>
      apiClient.get<ShoppingList>(`/api/shopping-list?date=${encodeURIComponent(date)}`),
    enabled: Boolean(session && date),
  })
}

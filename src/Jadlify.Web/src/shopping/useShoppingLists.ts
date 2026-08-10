import { useQuery } from '@tanstack/react-query'
import { apiClient } from '../api/apiClient'
import { useSession } from '../auth/useSession'
import type { ShoppingListDetail, ShoppingListDiff, ShoppingListIndex } from './types'

/**
 * The shared prefix for every shopping-list read. A planner mutation invalidates
 * this one key, because any plan change can make an existing list stale.
 */
export const shoppingListsBaseKey = ['shopping-lists'] as const

/** The index (active + history). Separate from detail so ticking an item need not refetch it. */
export const shoppingListIndexQueryKey = () => [...shoppingListsBaseKey, 'index'] as const

/** One list's full state, keyed by id. */
export const shoppingListDetailQueryKey = (id: string) =>
  [...shoppingListsBaseKey, 'detail', id] as const

/**
 * One list's refresh preview. Kept under its own key — never merged into detail —
 * so previewing a diff can never be mistaken for having applied it.
 */
export const shoppingListDiffQueryKey = (id: string) =>
  [...shoppingListsBaseKey, 'diff', id] as const

/** `GET /api/shopping-lists` — the single active list and the completed history. */
export function useShoppingListIndex() {
  const { session } = useSession()

  return useQuery({
    queryKey: shoppingListIndexQueryKey(),
    queryFn: () => apiClient.get<ShoppingListIndex>('/api/shopping-lists'),
    enabled: Boolean(session),
  })
}

/** `GET /api/shopping-lists/{id}` — one owner-scoped list, active or completed. */
export function useShoppingListDetail(id: string | undefined) {
  const { session } = useSession()

  return useQuery({
    queryKey: shoppingListDetailQueryKey(id ?? ''),
    queryFn: () => apiClient.get<ShoppingListDetail>(`/api/shopping-lists/${id}`),
    enabled: Boolean(session && id),
  })
}

/**
 * `GET /api/shopping-lists/{id}/diff` — a read-only preview of what a refresh would
 * change. This never writes; applying the diff is a separate, explicitly confirmed
 * refresh carrying the versions this preview returned.
 *
 * Enabled only for an active list (a completed one is a frozen snapshot that can
 * never drift), so it costs exactly one request while shopping. A planner mutation
 * invalidates it, which is what turns the "plan changed" banner on.
 */
export function useShoppingListDiff(id: string | undefined, enabled: boolean) {
  const { session } = useSession()

  return useQuery({
    queryKey: shoppingListDiffQueryKey(id ?? ''),
    queryFn: () => apiClient.get<ShoppingListDiff>(`/api/shopping-lists/${id}/diff`),
    enabled: Boolean(session && id && enabled),
    // A preview is only meaningful against the plan as it is right now.
    staleTime: 0,
  })
}

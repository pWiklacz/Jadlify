import { useMutation, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { apiClient } from '../api/apiClient'
import { ApiError } from '../api/client'
import {
  shoppingListDetailQueryKey,
  shoppingListDiffQueryKey,
  shoppingListIndexQueryKey,
  shoppingListsBaseKey,
} from './useShoppingLists'
import type {
  CompleteShoppingListRequest,
  CreateShoppingListRequest,
  RefreshShoppingListRequest,
  ShoppingListDetail,
  ToggleShoppingListItemRequest,
} from './types'

/** True when a rejection is the API's optimistic-concurrency 409 (stale version or fingerprint). */
export function isConflict(error: unknown): boolean {
  return error instanceof ApiError && error.status === 409
}

/**
 * Every write returns the list's new authoritative state, so the detail cache is
 * seeded from the response rather than refetched. The index is invalidated
 * alongside it because progress counts and status live there too.
 */
function adoptDetail(queryClient: QueryClient, detail: ShoppingListDetail) {
  queryClient.setQueryData(shoppingListDetailQueryKey(detail.id), detail)
  void queryClient.invalidateQueries({ queryKey: shoppingListIndexQueryKey() })
}

/** Creates a list from an arbitrary set of days. Fails with 409 when one is already active. */
export function useCreateShoppingList() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: CreateShoppingListRequest) =>
      apiClient.post<ShoppingListDetail>('/api/shopping-lists', body),
    onSuccess: (detail) => adoptDetail(queryClient, detail),
  })
}

/**
 * Ticks one item off, optimistically. The checkbox flips immediately — shopping
 * happens in a supermarket, not on a fast connection — and rolls back to the exact
 * previous snapshot if the write fails. `expectedVersion` still guards the server
 * side, so an optimistic tick can never silently overwrite a concurrent refresh.
 */
export function useToggleShoppingListItem(listId: string) {
  const queryClient = useQueryClient()
  const detailKey = shoppingListDetailQueryKey(listId)

  return useMutation({
    mutationFn: (variables: { itemId: string } & ToggleShoppingListItemRequest) =>
      apiClient.patch<ShoppingListDetail>(
        `/api/shopping-lists/${listId}/items/${variables.itemId}`,
        { isBought: variables.isBought, expectedVersion: variables.expectedVersion },
      ),
    onMutate: async (variables) => {
      await queryClient.cancelQueries({ queryKey: detailKey })
      const previous = queryClient.getQueryData<ShoppingListDetail>(detailKey)
      if (previous) {
        queryClient.setQueryData<ShoppingListDetail>(detailKey, {
          ...previous,
          items: previous.items.map((item) =>
            item.id === variables.itemId ? { ...item, isBought: variables.isBought } : item,
          ),
        })
      }
      return { previous }
    },
    onError: (_error, _variables, context) => {
      if (context?.previous) {
        queryClient.setQueryData(detailKey, context.previous)
      }
    },
    onSuccess: (detail) => adoptDetail(queryClient, detail),
  })
}

/**
 * Applies a previewed refresh. Both expected values come from the diff the user
 * just saw; if either moved on the API answers 409 and nothing is written, so the
 * user re-previews instead of applying changes they never read.
 */
export function useRefreshShoppingList(listId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: RefreshShoppingListRequest) =>
      apiClient.post<ShoppingListDetail>(`/api/shopping-lists/${listId}/refresh`, body),
    onSuccess: (detail) => {
      adoptDetail(queryClient, detail)
      // The applied preview is spent; the next "review changes" must re-read.
      void queryClient.invalidateQueries({ queryKey: shoppingListDiffQueryKey(listId) })
    },
  })
}

/** Completes the active list, freezing it into history. */
export function useCompleteShoppingList(listId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: CompleteShoppingListRequest) =>
      apiClient.post<ShoppingListDetail>(`/api/shopping-lists/${listId}/complete`, body),
    onSuccess: (detail) => adoptDetail(queryClient, detail),
  })
}

/** Invalidates every shopping read; used by planner mutations, whose writes can stale a list. */
export function invalidateShoppingLists(queryClient: QueryClient) {
  void queryClient.invalidateQueries({ queryKey: shoppingListsBaseKey })
}

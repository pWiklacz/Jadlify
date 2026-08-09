/**
 * Frontend mirrors of the persistent shopping-list contracts
 * (`src/Jadlify.API/Shopping/ShoppingListsContracts.cs`). A list is its own
 * aggregate now — not the old compute-on-read day projection — so the wire
 * carries identity, status, a concurrency `version`, the selected source days
 * and the per-item source contributions the "by meal" / "by day" views need.
 */
import type { MealType } from '../planning/types'
import type { ProductCategory } from '../products/types'

/** Wire status names; the UI maps its own Polish labels. A user has at most one `Active` list. */
export type ShoppingListStatus = 'Active' | 'Completed'

/**
 * A list as shown on the index. `boughtCount` / `itemCount` render the progress
 * line without loading the whole list.
 */
export interface ShoppingListSummary {
  id: string
  name: string
  status: ShoppingListStatus
  createdAt: string
  completedAt: string | null
  itemCount: number
  boughtCount: number
  /** Selected source days as `yyyy-MM-dd`, ascending. */
  sourceDays: string[]
}

/** The index split: the single active list (or null) and the completed history, newest first. */
export interface ShoppingListIndex {
  active: ShoppingListSummary | null
  history: ShoppingListSummary[]
}

/** One meal's contribution to a line — the basis of the "Wg posiłków" and "Wg dni" views. */
export interface ShoppingListItemSource {
  date: string
  mealType: MealType
  /** The recipe or product name the grams came from. */
  sourceLabel: string
  grams: number
}

/** One shopping line: a product snapshot, its aggregated grams, bought state and sources. */
export interface ShoppingListItem {
  id: string
  productId: string
  productName: string
  category: ProductCategory | null
  grams: number
  isBought: boolean
  sources: ShoppingListItemSource[]
}

/**
 * The full state of one list. `version` is the optimistic-concurrency token echoed
 * back on every mutation; a stale value is rejected with 409 rather than silently
 * overwriting someone else's change.
 */
export interface ShoppingListDetail {
  id: string
  name: string
  status: ShoppingListStatus
  version: number
  createdAt: string
  completedAt: string | null
  sourceDays: string[]
  items: ShoppingListItem[]
}

/** One diff line. `previousGrams` is null for an add, `newGrams` null for a removal. */
export interface ShoppingListDiffLine {
  productId: string
  productName: string
  category: ProductCategory | null
  previousGrams: number | null
  newGrams: number | null
}

/**
 * A refresh preview. Both `listVersion` and `sourceFingerprint` are echoed back on
 * confirmation: the version guards against the list moving on, the fingerprint
 * against the plan changing again between preview and apply.
 */
export interface ShoppingListDiff {
  listVersion: number
  sourceFingerprint: string
  hasChanges: boolean
  added: ShoppingListDiffLine[]
  removed: ShoppingListDiffLine[]
  changed: ShoppingListDiffLine[]
  /** Same totals, different contributing meals — confirmed like any change, but keeps `isBought`. */
  sourceOnly: ShoppingListDiffLine[]
}

/** Body for `POST /api/shopping-lists`. Days are distinct and within a 42-day span. */
export interface CreateShoppingListRequest {
  name: string
  days: string[]
}

/** Body for `PATCH /api/shopping-lists/{id}/items/{itemId}`. */
export interface ToggleShoppingListItemRequest {
  isBought: boolean
  expectedVersion: number
}

/** Body for `POST /api/shopping-lists/{id}/refresh`; both values come from the diff preview. */
export interface RefreshShoppingListRequest {
  expectedVersion: number
  expectedSourceFingerprint: string
}

/** Body for `POST /api/shopping-lists/{id}/complete`. */
export interface CompleteShoppingListRequest {
  expectedVersion: number
}

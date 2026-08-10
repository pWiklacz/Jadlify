/**
 * The recipe → planner handoff contract.
 *
 * A "Dodaj do planu" action navigates to
 * `/meal-plan?addRecipe=<id>&date=<yyyy-MM-dd>&returnTo=<encoded-path>`.
 * The URL — not `localStorage` — carries the intent, so the handoff survives a
 * reload, a shared link and a back/forward step. The planner validates the id
 * against its own owner-scoped data and strips the parameters once it has taken
 * them over, so a refresh does not re-trigger the flow.
 *
 * Phase 7 replaces the planner's add-meal modal without changing this contract.
 */

export interface AddToPlanParams {
  recipeId: string
  /** Target day as `yyyy-MM-dd`; omitted means "let the planner decide" (today). */
  date?: string
  /** Path to return to after the entry is added, e.g. `/recipes/abc`. */
  returnTo?: string
}

/** Query-parameter names of the handoff. Shared by the producer and the planner. */
export const ADD_TO_PLAN_PARAMS = {
  recipeId: 'addRecipe',
  date: 'date',
  returnTo: 'returnTo',
} as const

/** Builds the planner URL for a "Dodaj do planu" handoff. */
export function buildAddToPlanUrl({ recipeId, date, returnTo }: AddToPlanParams): string {
  const params = new URLSearchParams()
  params.set(ADD_TO_PLAN_PARAMS.recipeId, recipeId)
  if (date) {
    params.set(ADD_TO_PLAN_PARAMS.date, date)
  }
  if (returnTo) {
    params.set(ADD_TO_PLAN_PARAMS.returnTo, returnTo)
  }
  return `/meal-plan?${params.toString()}`
}

/** An `yyyy-MM-dd` day, the only date shape the handoff accepts. */
const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/

/**
 * Reads a handoff out of the planner's query string. Returns `null` when no
 * recipe is being handed off. `date` and `returnTo` are dropped unless they are
 * well-formed — a bad date is ignored rather than crashing the planner, and
 * `returnTo` must be a same-origin absolute path so the handoff can never be
 * used to bounce the user to another site.
 */
export function readAddToPlanParams(search: URLSearchParams): AddToPlanParams | null {
  const recipeId = search.get(ADD_TO_PLAN_PARAMS.recipeId)
  if (!recipeId) {
    return null
  }

  const date = search.get(ADD_TO_PLAN_PARAMS.date)
  const returnTo = search.get(ADD_TO_PLAN_PARAMS.returnTo)

  return {
    recipeId,
    date: date && ISO_DATE.test(date) ? date : undefined,
    returnTo: returnTo && isSafeInternalPath(returnTo) ? returnTo : undefined,
  }
}

/** Removes the handoff parameters, leaving any unrelated ones untouched. */
export function stripAddToPlanParams(search: URLSearchParams): URLSearchParams {
  const next = new URLSearchParams(search)
  for (const key of Object.values(ADD_TO_PLAN_PARAMS)) {
    next.delete(key)
  }
  return next
}

/** A single leading slash: an in-app path, never `//host` or `https://host`. */
function isSafeInternalPath(value: string): boolean {
  return value.startsWith('/') && !value.startsWith('//')
}

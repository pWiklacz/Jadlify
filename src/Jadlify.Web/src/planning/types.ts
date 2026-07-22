import type { ProductCategory } from '../products/types'

export interface DailyGoal {
  calories: number
  protein: number
  fat: number
  carbohydrates: number
}

export type UpsertDailyGoalRequest = DailyGoal

export interface MacroSummary {
  calories: number
  protein: number
  fat: number
  carbohydrates: number
}

export interface MealEntryMacro {
  entryId: string
  macros: MacroSummary
}

export interface DailyMacroSummary {
  date: string
  entries: MealEntryMacro[]
  total: MacroSummary
  goal: MacroSummary | null
  remaining: MacroSummary | null
}

export type MealType = 'Breakfast' | 'Lunch' | 'Dinner' | 'Snack'

export const mealTypes: MealType[] = ['Breakfast', 'Lunch', 'Dinner', 'Snack']

/**
 * Which source variant an entry carries. A `Recipe` entry references a live recipe and is
 * measured in portions; a `Product` entry carries its own snapshot and is measured in grams.
 */
export type MealPlanEntrySource = 'Recipe' | 'Product'

/**
 * A planned meal. The two field groups are mutually exclusive and keyed off `source`: a recipe
 * entry fills `recipeId` / `recipeName` / `portions`, a product entry fills `productId` /
 * `productName` / `category` / `grams`. Narrow on `source` before reading either group.
 *
 * A recipe entry's name follows the live recipe; a product entry's name and category come from
 * the snapshot taken when it was planned and do not follow later catalog edits or deletion.
 */
export interface MealPlanEntry {
  id: string
  date: string
  mealType: MealType
  source: MealPlanEntrySource
  recipeId: string | null
  recipeName: string | null
  portions: number | null
  productId: string | null
  productName: string | null
  category: ProductCategory | null
  grams: number | null
}

/**
 * Exactly one variant must be supplied: `recipeId` with `portions` (positive multiples of 0.5),
 * or `productId` with `grams`. The units are separate fields so neither is ever inferred.
 */
export type AddMealPlanEntryRequest = {
  date: string
  mealType: MealType
} & (
  | { recipeId: string; portions: number; productId?: never; grams?: never }
  | { productId: string; grams: number; recipeId?: never; portions?: never }
)

/**
 * The quantity must match the target entry's own source — `portions` for a recipe entry,
 * `grams` for a product entry. A mismatch is rejected by the API rather than coerced.
 */
export type UpdateMealPlanEntryRequest = { mealType: MealType } & (
  | { portions: number; grams?: never }
  | { grams: number; portions?: never }
)

export interface CreatedMealPlanEntryResponse {
  id: string
}

/**
 * One day of a planner range. `remaining` is signed — negative once the day is over its goal —
 * and is null together with `goal` when no goal is configured.
 */
export interface MealPlanDay {
  date: string
  entries: MealPlanEntry[]
  entryMacros: MealEntryMacro[]
  total: MacroSummary
  goal: MacroSummary | null
  remaining: MacroSummary | null
}

/**
 * An inclusive window of planned days, capped at 42 days. Every date from `from` to `to` is
 * present in `days` in ascending order, empty days included, so day, week, and month views all
 * render from one request rather than one per day.
 */
export interface MealPlanRange {
  from: string
  to: string
  days: MealPlanDay[]
}

/**
 * Reschedules an entry: it keeps its id, source and quantity — only the day and meal type
 * change. The date is `yyyy-MM-dd` (mirrors `MoveMealPlanEntryRequest`).
 */
export interface MoveMealPlanEntryRequest {
  date: string
  mealType: MealType
}

/**
 * Copies an entry onto every date in `targetDates`, leaving the original in place. Dates must
 * be distinct; the entry's own date is allowed (mirrors `CopyMealPlanEntryRequest`).
 */
export interface CopyMealPlanEntryRequest {
  targetDates: string[]
}

/** Whether a copied day keeps (`Add`) or clears (`Replace`) what each target day already holds. */
export type MealPlanDayCopyMode = 'Add' | 'Replace'

/**
 * Copies a whole day onto every date in `targetDates`. `Replace` first removes each target
 * day's existing entries (mirrors `CopyMealPlanDayRequest`).
 */
export interface CopyMealPlanDayRequest {
  targetDates: string[]
  mode: MealPlanDayCopyMode
}

/** One entry a copy created: the new id and the day it landed on (mirrors the API response). */
export interface CopiedMealPlanEntry {
  id: string
  date: string
  mealType: MealType
}

/** Everything a copy operation created, in target-date order (mirrors `CopiedMealPlanEntriesResponse`). */
export interface CopiedMealPlanEntriesResponse {
  entries: CopiedMealPlanEntry[]
}

import type { Product } from '../products/types'

/** The four macro fields used by recipe API responses and live UI preview. */
export interface RecipeMacroSummary {
  calories: number
  protein: number
  fat: number
  carbohydrates: number
}

/** Ingredient row returned by the API, including the product snapshot used for stable totals. */
export interface RecipeIngredient {
  productId: string
  productName: string
  wholeRecipeGrams: number
  per100Grams: RecipeMacroSummary
}

/** A saved recipe with whole-recipe and per-serving macro summaries. */
export interface Recipe {
  id: string
  name: string
  portions: number
  ingredients: RecipeIngredient[]
  totalMacros: RecipeMacroSummary
  perServingMacros: RecipeMacroSummary
  /** True when at least one of the user's meal-plan entries references this recipe. */
  isInPlan: boolean
}

/**
 * One recipe in the catalog index (mirrors `RecipeSummaryResponse`). Carries no
 * ingredient snapshots — the detail route's `Recipe` is where those live — plus
 * `isInPlan`, resolved by one batched owner-scoped usage read per page.
 */
export interface RecipeSummary {
  id: string
  name: string
  portions: number
  ingredientCount: number
  totalMacros: RecipeMacroSummary
  perServingMacros: RecipeMacroSummary
  isInPlan: boolean
}

/** One page of the paginated recipe catalog (mirrors `RecipeCatalogResponse`). */
export interface RecipeCatalogResponse {
  items: RecipeSummary[]
  total: number
  skip: number
  take: number
}

/**
 * The deterministic catalog sort orders (stable wire names mirroring the API
 * `RecipeCatalogSort` enum). A "recently updated" order is intentionally absent —
 * recipes carry no modification timestamp.
 */
export type RecipeCatalogSort = 'NameAsc' | 'CaloriesPerServingAsc'

/** Sort wire values with their Polish labels; the first is the default. */
export const RECIPE_SORTS: readonly { value: RecipeCatalogSort; label: string }[] = [
  { value: 'NameAsc', label: 'Alfabetycznie (A–Z)' },
  { value: 'CaloriesPerServingAsc', label: 'Kalorie na porcję (rosnąco)' },
] as const

export interface RecipeIngredientRequest {
  productId: string
  wholeRecipeGrams: number
}

export interface CreateRecipeRequest {
  name: string
  portions: number
  ingredients: RecipeIngredientRequest[]
}

export type UpdateRecipeRequest = CreateRecipeRequest

export interface CreatedRecipeResponse {
  id: string
}

/** Product shape needed by the recipe builder, compatible with Product and snapshots. */
export type RecipeProductSelection = Pick<
  Product,
  'id' | 'name' | 'calories' | 'protein' | 'fat' | 'carbohydrates'
>

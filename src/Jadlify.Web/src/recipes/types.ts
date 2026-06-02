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
}

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

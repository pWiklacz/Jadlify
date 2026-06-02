import type { RecipeMacroSummary } from './types'

export interface MacroPreviewIngredient {
  per100Grams: RecipeMacroSummary
  wholeRecipeGrams: number
}

export interface RecipeMacroPreviewTotals {
  total: RecipeMacroSummary
  perServing: RecipeMacroSummary
}

const ZERO_MACROS: RecipeMacroSummary = {
  calories: 0,
  protein: 0,
  fat: 0,
  carbohydrates: 0,
}

/** Calculates deterministic preview totals from per-100g values and whole-recipe grams. */
export function calculateRecipePreview(
  ingredients: MacroPreviewIngredient[],
  portions: number,
): RecipeMacroPreviewTotals {
  const total = ingredients.reduce<RecipeMacroSummary>(
    (sum, ingredient) => add(sum, scale(ingredient.per100Grams, ingredient.wholeRecipeGrams)),
    ZERO_MACROS,
  )

  return {
    total: roundSummary(total),
    perServing: portions > 0 ? roundSummary(scale(total, 1, portions)) : ZERO_MACROS,
  }
}

export function formatMacro(value: number): string {
  return String(Math.round(value * 10) / 10)
}

function add(left: RecipeMacroSummary, right: RecipeMacroSummary): RecipeMacroSummary {
  return {
    calories: left.calories + right.calories,
    protein: left.protein + right.protein,
    fat: left.fat + right.fat,
    carbohydrates: left.carbohydrates + right.carbohydrates,
  }
}

function scale(
  macros: RecipeMacroSummary,
  grams: number,
  baseGrams = 100,
): RecipeMacroSummary {
  const factor = grams / baseGrams
  return {
    calories: macros.calories * factor,
    protein: macros.protein * factor,
    fat: macros.fat * factor,
    carbohydrates: macros.carbohydrates * factor,
  }
}

function roundSummary(macros: RecipeMacroSummary): RecipeMacroSummary {
  return {
    calories: roundOne(macros.calories),
    protein: roundOne(macros.protein),
    fat: roundOne(macros.fat),
    carbohydrates: roundOne(macros.carbohydrates),
  }
}

function roundOne(value: number): number {
  return Math.round((value + Number.EPSILON) * 10) / 10
}

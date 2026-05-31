/**
 * Frontend mirrors of the API product contracts
 * (`src/Jadlify.API/Products/ProductContracts.cs`). Kept here so the products
 * feature owns its wire shapes independently of the rest of the app.
 */

/**
 * Optional net package size plus the per-100g extended nutrient profile shared by
 * every product contract (mirrors the nullable tail of the API records): the fat
 * breakdown, sugars/fiber, salt/sodium/potassium, and a compact micronutrient set.
 * Every field is nullable because Open Food Facts coverage is sparse and manual
 * entry stays optional (FR-006). Vitamins/minerals are grams, like OFF stores them.
 */
export interface ExtendedNutrition {
  packageSizeGrams: number | null
  saturatedFat: number | null
  monounsaturatedFat: number | null
  polyunsaturatedFat: number | null
  transFat: number | null
  sugars: number | null
  fiber: number | null
  salt: number | null
  sodium: number | null
  potassium: number | null
  calcium: number | null
  iron: number | null
  vitaminA: number | null
  vitaminC: number | null
  vitaminD: number | null
}

/** The 14 extended per-100g nutrient field keys, in form/display order (excludes packageSizeGrams). */
export const EXTENDED_NUTRITION_KEYS = [
  'saturatedFat',
  'monounsaturatedFat',
  'polyunsaturatedFat',
  'transFat',
  'sugars',
  'fiber',
  'salt',
  'sodium',
  'potassium',
  'calcium',
  'iron',
  'vitaminA',
  'vitaminC',
  'vitaminD',
] as const

export type ExtendedNutritionKey = (typeof EXTENDED_NUTRITION_KEYS)[number]

/** A product in the signed-in user's catalog (mirrors `ProductResponse`). */
export interface Product extends ExtendedNutrition {
  id: string
  name: string
  barcode: string | null
  calories: number
  protein: number
  fat: number
  carbohydrates: number
}

/** Body for creating a product (mirrors `CreateProductRequest`). */
export interface CreateProductRequest extends ExtendedNutrition {
  name: string
  barcode: string | null
  calories: number
  protein: number
  fat: number
  carbohydrates: number
}

/** Body for updating a product (mirrors `UpdateProductRequest`). */
export type UpdateProductRequest = CreateProductRequest

/** Outcome of a barcode lookup (mirrors `BarcodeLookupOutcome`). */
export type BarcodeLookupOutcome = 'Found' | 'NotFound' | 'AlreadyInCatalog'

/**
 * Result of a barcode lookup (mirrors `BarcodeLookupResponse`). Pre-fill fields
 * are nullable so partial external data maps to blank form inputs the user can
 * complete manually (FR-006).
 */
export interface BarcodeLookupResponse extends ExtendedNutrition {
  outcome: BarcodeLookupOutcome
  barcode: string
  existingProductId: string | null
  name: string | null
  brand: string | null
  calories: number | null
  protein: number | null
  fat: number | null
  carbohydrates: number | null
}

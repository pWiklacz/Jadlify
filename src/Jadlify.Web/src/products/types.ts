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

/**
 * The canonical product taxonomy — stable English wire names that mirror the API
 * `ProductCategory` enum (`src/Jadlify.Domain/Products/ProductCategory.cs`). A
 * `null` category is the always-valid "Bez kategorii" state. The UI maps each
 * name to a Polish label; the wire value is never translated.
 */
export type ProductCategory =
  | 'Vegetables'
  | 'Fruits'
  | 'MeatAndFish'
  | 'Dairy'
  | 'GrainsAndBread'
  | 'PantryAndDryGoods'
  | 'Frozen'
  | 'Beverages'
  | 'Other'

/** Category wire values with their Polish labels, in the curated shopping order (produce first, "Inne" last). */
export const PRODUCT_CATEGORIES: readonly { value: ProductCategory; label: string }[] = [
  { value: 'Vegetables', label: 'Warzywa' },
  { value: 'Fruits', label: 'Owoce' },
  { value: 'MeatAndFish', label: 'Mięso i ryby' },
  { value: 'Dairy', label: 'Nabiał' },
  { value: 'GrainsAndBread', label: 'Produkty zbożowe i pieczywo' },
  { value: 'PantryAndDryGoods', label: 'Produkty sypkie i spiżarnia' },
  { value: 'Frozen', label: 'Mrożonki' },
  { value: 'Beverages', label: 'Napoje' },
  { value: 'Other', label: 'Inne' },
] as const

const CATEGORY_LABELS = new Map(PRODUCT_CATEGORIES.map((c) => [c.value, c.label]))

/** Polish label for a category wire value; falls back to the raw value if unknown. */
export function categoryLabel(value: ProductCategory): string {
  return CATEGORY_LABELS.get(value) ?? value
}

/**
 * The deterministic catalog sort orders (stable wire names mirroring the API
 * `ProductCatalogSort` enum). A "recently updated" order is intentionally absent —
 * products carry no modification timestamp.
 */
export type ProductCatalogSort = 'NameAsc' | 'CaloriesAsc' | 'Category'

/** Sort wire values with their Polish labels; the first is the default. */
export const PRODUCT_SORTS: readonly { value: ProductCatalogSort; label: string }[] = [
  { value: 'NameAsc', label: 'Alfabetycznie (A–Z)' },
  { value: 'CaloriesAsc', label: 'Kalorie na 100 g (rosnąco)' },
  { value: 'Category', label: 'Według kategorii' },
] as const

/** A product in the signed-in user's catalog (mirrors `ProductResponse`). */
export interface Product extends ExtendedNutrition {
  id: string
  name: string
  barcode: string | null
  calories: number
  protein: number
  fat: number
  carbohydrates: number
  // Optional editable metadata added in the catalog phase; the wire always sends
  // them (nullable), but they stay optional here so lighter consumers (e.g. the
  // recipe ingredient picker) need not construct them.
  brand?: string | null
  category?: ProductCategory | null
}

/** Body for creating a product (mirrors `CreateProductRequest`). */
export interface CreateProductRequest extends ExtendedNutrition {
  name: string
  barcode: string | null
  calories: number
  protein: number
  fat: number
  carbohydrates: number
  brand?: string | null
  category?: ProductCategory | null
}

/** One page of the paginated product catalog (mirrors `ProductCatalogResponse`). */
export interface ProductCatalogResponse {
  items: Product[]
  total: number
  skip: number
  take: number
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
  /** Suggested category from the external source, as a stable wire name or null. */
  category: ProductCategory | null
  calories: number | null
  protein: number | null
  fat: number | null
  carbohydrates: number | null
}

/**
 * Frontend mirrors of the API product contracts
 * (`src/Jadlify.API/Products/ProductContracts.cs`). Kept here so the products
 * feature owns its wire shapes independently of the rest of the app.
 */

/** A product in the signed-in user's catalog (mirrors `ProductResponse`). */
export interface Product {
  id: string
  name: string
  barcode: string | null
  calories: number
  protein: number
  fat: number
  carbohydrates: number
}

/** Body for creating a product (mirrors `CreateProductRequest`). */
export interface CreateProductRequest {
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
export interface BarcodeLookupResponse {
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

import { useState } from 'react'
import { DeleteProductDialog } from './DeleteProductDialog'
import { ProductFormModal } from './ProductFormModal'
import { useProducts } from './useProducts'
import type { ExtendedNutritionKey, Product } from './types'

type ModalState = { mode: 'create' } | { mode: 'edit'; product: Product } | null

/** A few high-signal extended fields surfaced compactly on the card; full detail lives in the edit form. */
const CARD_EXTENDED_FIELDS: { key: ExtendedNutritionKey; label: string }[] = [
  { key: 'saturatedFat', label: 'Saturated fat' },
  { key: 'sugars', label: 'Sugars' },
  { key: 'fiber', label: 'Fiber' },
  { key: 'salt', label: 'Salt' },
]

/**
 * Product catalog page: lists the signed-in user's products and hosts the
 * add/edit modal (with barcode pre-fill) and the delete confirmation. Wired to
 * the API through react-query hooks; mutations refresh the list automatically.
 */
export function ProductsPage() {
  const { data: products, isLoading, isError } = useProducts()
  const [modal, setModal] = useState<ModalState>(null)
  const [deleteTarget, setDeleteTarget] = useState<Product | null>(null)

  // A barcode lookup that hit an existing product: switch the modal to edit it.
  function editExisting(productId: string) {
    const existing = products?.find((product) => product.id === productId)
    if (existing) {
      setModal({ mode: 'edit', product: existing })
    }
  }

  return (
    <section className="flex flex-col gap-4">
      <div className="flex items-center justify-between gap-4">
        <h1 className="text-2xl font-bold">Products</h1>
        <button
          type="button"
          onClick={() => setModal({ mode: 'create' })}
          className="rounded-md bg-slate-900 px-4 py-2 text-sm font-medium text-white"
        >
          Add product
        </button>
      </div>

      {isLoading && <p className="text-slate-600">Loading products…</p>}

      {isError && (
        <p role="alert" className="text-red-600">
          Could not load products. Please refresh to try again.
        </p>
      )}

      {products && products.length === 0 && (
        <p className="text-slate-600">No products yet. Add your first product to get started.</p>
      )}

      {products && products.length > 0 && (
        <ul className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {products.map((product) => (
            <li
              key={product.id}
              className="flex flex-col gap-2 rounded-lg border border-slate-200 bg-white p-4"
            >
              <h2 className="font-semibold">{product.name}</h2>
              {product.barcode && (
                <p className="text-sm text-slate-500">Barcode: {product.barcode}</p>
              )}
              <dl className="grid grid-cols-2 gap-x-3 gap-y-1 text-sm text-slate-700">
                <Macro label="kcal / 100 g" value={product.calories} />
                <Macro label="Protein" value={product.protein} />
                <Macro label="Fat" value={product.fat} />
                <Macro label="Carbs" value={product.carbohydrates} />
              </dl>

              {product.packageSizeGrams != null && (
                <div className="rounded-md bg-slate-50 px-3 py-2 text-sm">
                  <p className="text-slate-500">Package: {formatNumber(product.packageSizeGrams)} g</p>
                  <p className="font-medium text-slate-700">
                    Per package: {formatNumber(perPackage(product.calories, product.packageSizeGrams))} kcal
                    {` · P ${formatNumber(perPackage(product.protein, product.packageSizeGrams))} g`}
                    {` · F ${formatNumber(perPackage(product.fat, product.packageSizeGrams))} g`}
                    {` · C ${formatNumber(perPackage(product.carbohydrates, product.packageSizeGrams))} g`}
                  </p>
                </div>
              )}

              {CARD_EXTENDED_FIELDS.some((field) => product[field.key] != null) && (
                <dl className="grid grid-cols-2 gap-x-3 gap-y-1 text-sm text-slate-700">
                  {CARD_EXTENDED_FIELDS.map((field) => {
                    const value = product[field.key]
                    return value == null ? null : (
                      <div key={field.key} className="flex justify-between gap-2">
                        <dt className="text-slate-500">{field.label}</dt>
                        <dd className="font-medium">{formatMass(value)}</dd>
                      </div>
                    )
                  })}
                </dl>
              )}

              <div className="mt-1 flex gap-2">
                <button
                  type="button"
                  onClick={() => setModal({ mode: 'edit', product })}
                  className="rounded-md border border-slate-300 px-3 py-1 text-sm font-medium hover:bg-slate-100"
                >
                  Edit
                </button>
                <button
                  type="button"
                  onClick={() => setDeleteTarget(product)}
                  className="rounded-md border border-slate-300 px-3 py-1 text-sm font-medium text-red-600 hover:bg-red-50"
                >
                  Delete
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}

      {modal && (
        <ProductFormModal
          // Remount on mode/target change so the form re-seeds from the new
          // product (e.g. when an AlreadyInCatalog lookup switches us to edit).
          key={modal.mode === 'edit' ? `edit-${modal.product.id}` : 'create'}
          mode={modal.mode}
          product={modal.mode === 'edit' ? modal.product : undefined}
          onClose={() => setModal(null)}
          onEditExisting={editExisting}
        />
      )}

      {deleteTarget && (
        <DeleteProductDialog product={deleteTarget} onClose={() => setDeleteTarget(null)} />
      )}
    </section>
  )
}

function Macro({ label, value }: { label: string; value: number }) {
  return (
    <div className="flex justify-between gap-2">
      <dt className="text-slate-500">{label}</dt>
      <dd className="font-medium">{value}</dd>
    </div>
  )
}

/** Rounds to at most one decimal and drops a trailing `.0`. */
function formatNumber(value: number): string {
  return String(Math.round(value * 10) / 10)
}

/** Scales a per-100g value to the whole package. */
function perPackage(per100g: number, packageSizeGrams: number): number {
  return (per100g * packageSizeGrams) / 100
}

/** Formats a grams value, dropping to mg / µg so sub-gram micronutrients stay readable. */
function formatMass(grams: number): string {
  if (grams === 0 || grams >= 1) {
    return `${formatNumber(grams)} g`
  }
  if (grams >= 0.001) {
    return `${formatNumber(grams * 1000)} mg`
  }
  return `${formatNumber(grams * 1_000_000)} µg`
}

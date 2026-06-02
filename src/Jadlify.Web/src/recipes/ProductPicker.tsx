import { useState } from 'react'
import { useProductSearch } from './useProductSearch'
import type { RecipeProductSelection } from './types'

interface ProductPickerProps {
  selected: RecipeProductSelection | null
  excludedProductIds: string[]
  onSelect: (product: RecipeProductSelection) => void
  onAddMissing: () => void
}

/** Search/select control for one ingredient row. */
export function ProductPicker({
  selected,
  excludedProductIds,
  onSelect,
  onAddMissing,
}: ProductPickerProps) {
  const [search, setSearch] = useState(selected?.name ?? '')
  const { data: products, isLoading, isError } = useProductSearch(search)

  return (
    <div className="flex flex-col gap-2">
      <label className="text-sm font-medium">
        Product
        <input
          type="search"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Search by name or barcode"
          className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
        />
      </label>

      {selected && (
        <p className="rounded-md bg-emerald-50 px-3 py-2 text-sm text-emerald-800">
          Selected: <span className="font-medium">{selected.name}</span>
        </p>
      )}

      <div className="max-h-36 overflow-y-auto rounded-md border border-slate-200 bg-white">
        {isLoading && <p className="px-3 py-2 text-sm text-slate-600">Searching products.</p>}
        {isError && (
          <p role="alert" className="px-3 py-2 text-sm text-red-600">
            Could not search products.
          </p>
        )}
        {products?.length === 0 && (
          <p className="px-3 py-2 text-sm text-slate-600">No matching products.</p>
        )}
        {products?.map((product) => {
          const alreadyUsed = excludedProductIds.includes(product.id) && selected?.id !== product.id
          return (
            <button
              key={product.id}
              type="button"
              onClick={() => onSelect(product)}
              disabled={alreadyUsed}
              className="flex w-full items-center justify-between gap-3 border-b border-slate-100 px-3 py-2 text-left text-sm last:border-b-0 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
            >
              <span className="min-w-0">
                <span className="block truncate font-medium">{product.name}</span>
                <span className="text-slate-500">
                  {product.calories} kcal / 100 g
                </span>
              </span>
              <span className="shrink-0 text-xs font-medium text-slate-600">
                {alreadyUsed ? 'Used' : 'Select'}
              </span>
            </button>
          )
        })}
      </div>

      <button
        type="button"
        onClick={onAddMissing}
        className="self-start rounded-md border border-slate-300 px-3 py-2 text-sm font-medium hover:bg-slate-100"
      >
        Add missing product
      </button>
    </div>
  )
}

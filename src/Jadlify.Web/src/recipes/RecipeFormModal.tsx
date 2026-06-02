import { type FormEvent, useEffect, useId, useMemo, useRef, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { ProductFormModal } from '../products/ProductFormModal'
import { productSearchQueryKey } from './useProductSearch'
import { useCreateRecipe, useUpdateRecipe } from './useRecipeMutations'
import { RecipeMacroPreview } from './RecipeMacroPreview'
import { ProductPicker } from './ProductPicker'
import type {
  CreateRecipeRequest,
  Recipe,
  RecipeProductSelection,
  UpdateRecipeRequest,
} from './types'

type Mode = 'create' | 'edit'

interface RecipeFormModalProps {
  mode: Mode
  recipe?: Recipe
  onClose: () => void
}

interface DraftIngredient {
  rowId: string
  product: RecipeProductSelection | null
  grams: string
}

let nextRowId = 1

function createRow(product: RecipeProductSelection | null = null, grams = ''): DraftIngredient {
  nextRowId += 1
  return { rowId: `ingredient-${nextRowId}`, product, grams }
}

function toDraftIngredients(recipe?: Recipe): DraftIngredient[] {
  if (!recipe) {
    return [createRow()]
  }

  return recipe.ingredients.map((ingredient) =>
    createRow(
      {
        id: ingredient.productId,
        name: ingredient.productName,
        calories: ingredient.per100Grams.calories,
        protein: ingredient.per100Grams.protein,
        fat: ingredient.per100Grams.fat,
        carbohydrates: ingredient.per100Grams.carbohydrates,
      },
      String(ingredient.wholeRecipeGrams),
    ),
  )
}

/** Add/edit recipe builder. Keeps whole-recipe grams explicit in labels and request bodies. */
export function RecipeFormModal({ mode, recipe, onClose }: RecipeFormModalProps) {
  const [name, setName] = useState(recipe?.name ?? '')
  const [portions, setPortions] = useState(recipe ? String(recipe.portions) : '1')
  const [ingredients, setIngredients] = useState<DraftIngredient[]>(() => toDraftIngredients(recipe))
  const [productModalRowId, setProductModalRowId] = useState<string | null>(null)
  const [productNotice, setProductNotice] = useState<string | null>(null)

  const dialogRef = useRef<HTMLDivElement>(null)
  const nameRef = useRef<HTMLInputElement>(null)
  const productModalRowIdRef = useRef<string | null>(null)
  const baseId = useId()
  const titleId = `${baseId}-title`
  const queryClient = useQueryClient()
  const createRecipe = useCreateRecipe()
  const updateRecipe = useUpdateRecipe()

  const parsedPortions = Number(portions)
  const selectedProductIds = ingredients
    .map((ingredient) => ingredient.product?.id)
    .filter((id): id is string => Boolean(id))
  const completeIngredients = ingredients.filter(
    (ingredient) => ingredient.product && parsePositiveNumber(ingredient.grams) > 0,
  )
  const hasDuplicateProducts = new Set(selectedProductIds).size !== selectedProductIds.length
  const canSave =
    name.trim().length > 0 &&
    Number.isInteger(parsedPortions) &&
    parsedPortions > 0 &&
    completeIngredients.length > 0 &&
    completeIngredients.length === ingredients.length &&
    !hasDuplicateProducts

  const previewIngredients = useMemo(
    () =>
      ingredients
        .filter((ingredient) => ingredient.product && parsePositiveNumber(ingredient.grams) > 0)
        .map((ingredient) => ({
          wholeRecipeGrams: parsePositiveNumber(ingredient.grams),
          per100Grams: {
            calories: ingredient.product!.calories,
            protein: ingredient.product!.protein,
            fat: ingredient.product!.fat,
            carbohydrates: ingredient.product!.carbohydrates,
          },
        })),
    [ingredients],
  )

  const isSaving = createRecipe.isPending || updateRecipe.isPending
  const saveFailed = createRecipe.isError || updateRecipe.isError

  useEffect(() => {
    const dialog = dialogRef.current
    if (!dialog) {
      return
    }

    function focusable(): HTMLElement[] {
      return Array.from(
        dialog!.querySelectorAll<HTMLElement>(
          'a[href], button:not([disabled]), input:not([disabled]), [tabindex]:not([tabindex="-1"])',
        ),
      )
    }

    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape' && !productModalRowId) {
        event.preventDefault()
        onClose()
        return
      }
      if (event.key !== 'Tab') {
        return
      }
      const items = focusable()
      if (items.length === 0) {
        return
      }
      const first = items[0]
      const last = items[items.length - 1]
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault()
        last.focus()
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault()
        first.focus()
      }
    }

    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [onClose, productModalRowId])

  useEffect(() => {
    nameRef.current?.focus()
  }, [])

  function updateIngredient(rowId: string, patch: Partial<DraftIngredient>) {
    setIngredients((current) =>
      current.map((ingredient) =>
        ingredient.rowId === rowId ? { ...ingredient, ...patch } : ingredient,
      ),
    )
  }

  function removeIngredient(rowId: string) {
    setIngredients((current) => {
      const next = current.filter((ingredient) => ingredient.rowId !== rowId)
      return next.length > 0 ? next : [createRow()]
    })
  }

  function handleProductCreated(product: RecipeProductSelection) {
    const rowId = productModalRowIdRef.current
    if (rowId) {
      updateIngredient(rowId, { product })
    }
    productModalRowIdRef.current = null
    setProductModalRowId(null)
    setProductNotice(null)
    void queryClient.invalidateQueries({ queryKey: productSearchQueryKey })
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    if (!canSave) {
      return
    }

    const body: CreateRecipeRequest | UpdateRecipeRequest = {
      name: name.trim(),
      portions: parsedPortions,
      ingredients: ingredients.map((ingredient) => ({
        productId: ingredient.product!.id,
        wholeRecipeGrams: parsePositiveNumber(ingredient.grams),
      })),
    }

    if (mode === 'edit' && recipe) {
      updateRecipe.mutate({ id: recipe.id, body }, { onSuccess: onClose })
    } else {
      createRecipe.mutate(body, { onSuccess: onClose })
    }
  }

  return (
    <div
      className="fixed inset-0 z-40 flex items-center justify-center bg-slate-900/50 p-4"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget && !productModalRowId) {
          onClose()
        }
      }}
    >
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="flex max-h-full w-full max-w-3xl flex-col overflow-y-auto rounded-lg bg-white p-5 shadow-xl"
      >
        <div className="mb-4 flex items-start justify-between gap-4">
          <h2 id={titleId} className="text-xl font-bold">
            {mode === 'edit' ? 'Edit recipe' : 'Create recipe'}
          </h2>
          <button
            type="button"
            aria-label="Close"
            onClick={onClose}
            className="rounded-md px-2 text-2xl leading-none text-slate-500 hover:bg-slate-100"
          >
            x
          </button>
        </div>

        <form className="flex flex-col gap-5" onSubmit={handleSubmit}>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-[1fr_10rem]">
            <label className="text-sm font-medium">
              Name
              <input
                ref={nameRef}
                type="text"
                required
                value={name}
                onChange={(event) => setName(event.target.value)}
                className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
              />
            </label>
            <label className="text-sm font-medium">
              Portions
              <input
                type="number"
                min="1"
                step="1"
                required
                value={portions}
                onChange={(event) => setPortions(event.target.value)}
                className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
              />
            </label>
          </div>

          <div className="flex flex-col gap-3">
            <div className="flex items-center justify-between gap-3">
              <h3 className="text-base font-semibold">Ingredients</h3>
              <button
                type="button"
                onClick={() => setIngredients((current) => [...current, createRow()])}
                className="rounded-md border border-slate-300 px-3 py-2 text-sm font-medium hover:bg-slate-100"
              >
                Add row
              </button>
            </div>

            {ingredients.map((ingredient, index) => (
              <section
                key={ingredient.rowId}
                className="grid grid-cols-1 gap-3 rounded-lg border border-slate-200 p-3 md:grid-cols-[minmax(0,1fr)_10rem_auto]"
              >
                <ProductPicker
                  selected={ingredient.product}
                  excludedProductIds={selectedProductIds}
                  onSelect={(product) => updateIngredient(ingredient.rowId, { product })}
                  onAddMissing={() => {
                    setProductNotice(null)
                    productModalRowIdRef.current = ingredient.rowId
                    setProductModalRowId(ingredient.rowId)
                  }}
                />
                <label className="text-sm font-medium">
                  Whole recipe grams
                  <input
                    type="number"
                    min="0"
                    step="any"
                    value={ingredient.grams}
                    onChange={(event) =>
                      updateIngredient(ingredient.rowId, { grams: event.target.value })
                    }
                    className="mt-1 w-full rounded-md border border-slate-300 px-3 py-2"
                  />
                </label>
                <button
                  type="button"
                  onClick={() => removeIngredient(ingredient.rowId)}
                  className="h-10 self-start rounded-md border border-slate-300 px-3 py-2 text-sm font-medium text-red-600 hover:bg-red-50 md:mt-6"
                >
                  Remove
                  <span className="sr-only"> ingredient {index + 1}</span>
                </button>
              </section>
            ))}
          </div>

          {hasDuplicateProducts && (
            <p role="alert" className="text-sm text-red-600">
              Each product can only appear once in a recipe.
            </p>
          )}

          {productNotice && (
            <p role="status" className="text-sm text-slate-600">
              {productNotice}
            </p>
          )}

          <RecipeMacroPreview ingredients={previewIngredients} portions={parsedPortions} />

          {saveFailed && (
            <p role="alert" className="text-sm text-red-600">
              Could not save the recipe. Check the ingredients and try again.
            </p>
          )}

          <div className="flex justify-end gap-2">
            <button
              type="button"
              onClick={onClose}
              className="rounded-md px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={!canSave || isSaving}
              className="rounded-md bg-slate-900 px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
            >
              {isSaving ? 'Saving.' : 'Save recipe'}
            </button>
          </div>
        </form>
      </div>

      {productModalRowId && (
        <ProductFormModal
          mode="create"
          onClose={() => {
            productModalRowIdRef.current = null
            setProductModalRowId(null)
          }}
          onCreated={handleProductCreated}
          onEditExisting={() => {
            setProductNotice('This product already exists. Search for it above and select it.')
            productModalRowIdRef.current = null
            setProductModalRowId(null)
          }}
        />
      )}
    </div>
  )
}

function parsePositiveNumber(value: string): number {
  const parsed = Number(value)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : 0
}

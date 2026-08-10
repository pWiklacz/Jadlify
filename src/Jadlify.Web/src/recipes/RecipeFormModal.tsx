import { type FormEvent, useId, useMemo, useRef, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { ProductFormModal } from '../products/ProductFormModal'
import { Button } from '../ui/Button'
import { Dialog } from '../ui/Dialog'
import { Field, TextInput } from '../ui/Field'
import { LoadingState } from '../ui/QueryState'
import { formatCount, formatKcal, formatMacro, parseDecimal } from '../ui/formatters'
import { calculateRecipePreview } from './macroMath'
import { ProductPicker } from './ProductPicker'
import { productSearchQueryKey } from './useProductSearch'
import { useRecipe } from './useRecipes'
import { useCreateRecipe, useUpdateRecipe } from './useRecipeMutations'
import type {
  CreateRecipeRequest,
  Recipe,
  RecipeProductSelection,
  UpdateRecipeRequest,
} from './types'

type Mode = 'create' | 'edit'

interface RecipeFormModalProps {
  mode: Mode
  /** Id of the recipe being edited; required in edit mode, ignored in create mode. */
  recipeId?: string
  onClose: () => void
  /** Fires after a successful save so the parent can toast. */
  onSaved?: (mode: Mode) => void
  /** Create-mode callback with the new recipe id (used by the add-to-plan flow). */
  onCreated?: (recipeId: string) => void
}

interface DraftIngredient {
  rowId: string
  product: RecipeProductSelection | null
  grams: string
}

let nextRowId = 0

function createRow(
  product: RecipeProductSelection | null = null,
  grams = '',
): DraftIngredient {
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

/**
 * Add/edit recipe builder. In edit mode the full recipe (with its ingredient
 * snapshots) is fetched by id — the catalog summary deliberately omits them —
 * and the form only mounts once that data is in hand, so the draft initialises
 * exactly once.
 */
export function RecipeFormModal({
  mode,
  recipeId,
  onClose,
  onSaved,
  onCreated,
}: RecipeFormModalProps) {
  const detail = useRecipe(mode === 'edit' ? (recipeId ?? null) : null)

  if (mode === 'edit' && !detail.data) {
    return (
      <Dialog open onClose={onClose} title="Edytuj przepis" size="lg">
        {detail.isError ? (
          <p role="alert" className="text-sm text-danger">
            Nie udało się wczytać przepisu. Zamknij okno i spróbuj ponownie.
          </p>
        ) : (
          <LoadingState label="Wczytujemy przepis…" lines={4} />
        )}
      </Dialog>
    )
  }

  return (
    <RecipeForm
      mode={mode}
      recipe={detail.data}
      onClose={onClose}
      onSaved={onSaved}
      onCreated={onCreated}
    />
  )
}

function RecipeForm({
  mode,
  recipe,
  onClose,
  onSaved,
  onCreated,
}: {
  mode: Mode
  recipe?: Recipe
  onClose: () => void
  onSaved?: (mode: Mode) => void
  onCreated?: (recipeId: string) => void
}) {
  const [name, setName] = useState(recipe?.name ?? '')
  const [portions, setPortions] = useState(recipe ? String(recipe.portions) : '1')
  const [ingredients, setIngredients] = useState<DraftIngredient[]>(() =>
    toDraftIngredients(recipe),
  )
  const [showErrors, setShowErrors] = useState(false)
  const [dirty, setDirty] = useState(false)
  const [confirmDiscard, setConfirmDiscard] = useState(false)
  const [productModalRowId, setProductModalRowId] = useState<string | null>(null)

  const nameRef = useRef<HTMLInputElement>(null)
  const baseId = useId()
  const queryClient = useQueryClient()
  const createRecipe = useCreateRecipe()
  const updateRecipe = useUpdateRecipe()

  const parsedPortions = parseDecimal(portions)
  const selectedProductIds = ingredients
    .map((ingredient) => ingredient.product?.id)
    .filter((id): id is string => Boolean(id))
  const duplicateProductIds = new Set(
    selectedProductIds.filter(
      (id, index) => selectedProductIds.indexOf(id) !== index,
    ),
  )

  const nameError = name.trim() === '' ? 'Podaj nazwę przepisu.' : null
  const portionsError =
    parsedPortions === null || !Number.isInteger(parsedPortions) || parsedPortions < 1
      ? 'Liczba porcji musi być dodatnią liczbą całkowitą, np. 4.'
      : null

  function rowError(ingredient: DraftIngredient): string | null {
    if (!ingredient.product) {
      return 'Wybierz produkt dla tego składnika.'
    }
    if (duplicateProductIds.has(ingredient.product.id)) {
      return 'Ten produkt jest już w przepisie.'
    }
    const grams = parseDecimal(ingredient.grams)
    if (grams === null || grams <= 0) {
      return 'Podaj gramaturę większą od zera, np. 250.'
    }
    return null
  }

  const rowErrors = ingredients.map(rowError)
  const hasRowErrors = rowErrors.some((error) => error !== null)
  const canSave = !nameError && !portionsError && !hasRowErrors && ingredients.length > 0

  const previewIngredients = useMemo(
    () =>
      ingredients
        .filter((ingredient) => ingredient.product && (parseDecimal(ingredient.grams) ?? 0) > 0)
        .map((ingredient) => ({
          wholeRecipeGrams: parseDecimal(ingredient.grams) ?? 0,
          per100Grams: {
            calories: ingredient.product!.calories,
            protein: ingredient.product!.protein,
            fat: ingredient.product!.fat,
            carbohydrates: ingredient.product!.carbohydrates,
          },
        })),
    [ingredients],
  )
  const preview = calculateRecipePreview(
    previewIngredients,
    portionsError ? 0 : (parsedPortions ?? 0),
  )

  const isSaving = createRecipe.isPending || updateRecipe.isPending
  const saveFailed = createRecipe.isError || updateRecipe.isError

  function touch() {
    setDirty(true)
  }

  function updateIngredient(rowId: string, patch: Partial<DraftIngredient>) {
    touch()
    setIngredients((current) =>
      current.map((ingredient) =>
        ingredient.rowId === rowId ? { ...ingredient, ...patch } : ingredient,
      ),
    )
  }

  function removeIngredient(rowId: string) {
    touch()
    setIngredients((current) => {
      const next = current.filter((ingredient) => ingredient.rowId !== rowId)
      return next.length > 0 ? next : [createRow()]
    })
  }

  /** Moves a row one slot toward `direction`; a no-op at the ends. */
  function moveIngredient(index: number, direction: -1 | 1) {
    const target = index + direction
    if (target < 0 || target >= ingredients.length) {
      return
    }
    touch()
    setIngredients((current) => {
      const next = [...current]
      const [moved] = next.splice(index, 1)
      next.splice(target, 0, moved)
      return next
    })
  }

  function handleProductCreated(product: RecipeProductSelection) {
    if (productModalRowId) {
      updateIngredient(productModalRowId, { product })
    }
    setProductModalRowId(null)
    void queryClient.invalidateQueries({ queryKey: productSearchQueryKey })
  }

  function requestClose() {
    if (dirty && !isSaving) {
      setConfirmDiscard(true)
      return
    }
    onClose()
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    if (!canSave) {
      setShowErrors(true)
      if (nameError) {
        nameRef.current?.focus()
      }
      return
    }

    const body: CreateRecipeRequest | UpdateRecipeRequest = {
      name: name.trim(),
      portions: parsedPortions!,
      ingredients: ingredients.map((ingredient) => ({
        productId: ingredient.product!.id,
        wholeRecipeGrams: parseDecimal(ingredient.grams)!,
      })),
    }

    if (mode === 'edit' && recipe) {
      updateRecipe.mutate(
        { id: recipe.id, body },
        {
          onSuccess: () => {
            onSaved?.('edit')
            onClose()
          },
        },
      )
      return
    }

    createRecipe.mutate(body, {
      onSuccess: (created) => {
        onSaved?.('create')
        onCreated?.(created.id)
        onClose()
      },
    })
  }

  const portionsLabel =
    portionsError || parsedPortions === null
      ? null
      : formatCount(parsedPortions, 'porcja', 'porcje', 'porcji')

  return (
    <>
      <Dialog
        open
        onClose={requestClose}
        size="lg"
        title={mode === 'edit' ? 'Edytuj przepis' : 'Nowy przepis'}
        initialFocusRef={nameRef}
        footer={
          <>
            <Button variant="ghost" onClick={requestClose} disabled={isSaving}>
              Anuluj
            </Button>
            <Button type="submit" form={`${baseId}-form`} isLoading={isSaving}>
              {mode === 'edit' ? 'Zapisz zmiany' : 'Zapisz przepis'}
            </Button>
          </>
        }
      >
        <form id={`${baseId}-form`} className="flex flex-col gap-5" onSubmit={handleSubmit} noValidate>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-[1fr_9rem]">
            <Field id={`${baseId}-name`} label="Nazwa przepisu" error={showErrors ? nameError : null}>
              <TextInput
                ref={nameRef}
                id={`${baseId}-name`}
                value={name}
                onChange={(event) => {
                  touch()
                  setName(event.target.value)
                }}
                placeholder="Np. Kurczak z ryżem i warzywami"
                invalid={showErrors && Boolean(nameError)}
                aria-describedby={showErrors && nameError ? `${baseId}-name-error` : undefined}
              />
            </Field>

            <Field
              id={`${baseId}-portions`}
              label="Liczba porcji"
              error={showErrors ? portionsError : null}
              hint="Na tyle porcji podzielimy wartości całego przepisu."
            >
              <TextInput
                id={`${baseId}-portions`}
                inputMode="numeric"
                value={portions}
                onChange={(event) => {
                  touch()
                  setPortions(event.target.value)
                }}
                invalid={showErrors && Boolean(portionsError)}
                aria-describedby={
                  [
                    `${baseId}-portions-hint`,
                    showErrors && portionsError ? `${baseId}-portions-error` : null,
                  ]
                    .filter(Boolean)
                    .join(' ') || undefined
                }
              />
            </Field>
          </div>

          <section className="flex flex-col gap-3" aria-label="Składniki">
            <div className="flex flex-wrap items-baseline gap-2">
              <h3 className="text-[11px] font-bold uppercase tracking-eyebrow text-label">
                Składniki
              </h3>
              <span className="rounded-pill border border-terracotta/30 bg-terracotta/10 px-2 py-0.5 text-[10px] font-bold uppercase tracking-eyebrow text-terracotta">
                Cały przepis
              </span>
            </div>
            <p className="text-[13px] leading-relaxed text-mocha">
              Składniki i ich gramatury dotyczą <b>całego przygotowywanego przepisu</b> — nie jednej
              porcji.
            </p>

            <ul className="flex flex-col gap-3">
              {ingredients.map((ingredient, index) => {
                const error = rowErrors[index]
                const rowId = `${baseId}-row-${ingredient.rowId}`
                return (
                  <li
                    key={ingredient.rowId}
                    className="rounded-panel border border-cream-border bg-cream-panel p-3"
                  >
                    <div className="grid grid-cols-1 gap-3 design:grid-cols-[minmax(0,1fr)_9rem]">
                      <Field id={`${rowId}-product`} label={`Składnik ${index + 1}`}>
                        <ProductPicker
                          id={`${rowId}-product`}
                          selected={ingredient.product}
                          excludedProductIds={selectedProductIds}
                          onSelect={(product) =>
                            updateIngredient(ingredient.rowId, { product })
                          }
                          onAddMissing={() => setProductModalRowId(ingredient.rowId)}
                          invalid={showErrors && Boolean(error)}
                          describedBy={showErrors && error ? `${rowId}-error` : undefined}
                        />
                      </Field>

                      <Field id={`${rowId}-grams`} label="Gramatura (g)">
                        <TextInput
                          id={`${rowId}-grams`}
                          inputMode="decimal"
                          value={ingredient.grams}
                          onChange={(event) =>
                            updateIngredient(ingredient.rowId, { grams: event.target.value })
                          }
                          placeholder="np. 250"
                          aria-label={`Gramatura składnika ${index + 1} w gramach (dla całego przepisu)`}
                          invalid={showErrors && Boolean(error)}
                        />
                      </Field>
                    </div>

                    {showErrors && error && (
                      <p
                        id={`${rowId}-error`}
                        role="alert"
                        className="mt-2 flex items-center gap-1.5 text-[12.5px] text-danger"
                      >
                        <span aria-hidden="true">▲</span>
                        {error}
                      </p>
                    )}

                    <div className="mt-2 flex items-center justify-end gap-1">
                      <IconButton
                        label={`Przesuń składnik ${index + 1} wyżej`}
                        title="Wyżej"
                        disabled={index === 0}
                        onClick={() => moveIngredient(index, -1)}
                      >
                        <ArrowGlyph direction="up" />
                      </IconButton>
                      <IconButton
                        label={`Przesuń składnik ${index + 1} niżej`}
                        title="Niżej"
                        disabled={index === ingredients.length - 1}
                        onClick={() => moveIngredient(index, 1)}
                      >
                        <ArrowGlyph direction="down" />
                      </IconButton>
                      <IconButton
                        label={`Usuń składnik ${index + 1}`}
                        title="Usuń składnik"
                        danger
                        onClick={() => removeIngredient(ingredient.rowId)}
                      >
                        <TrashGlyph />
                      </IconButton>
                    </div>
                  </li>
                )
              })}
            </ul>

            <Button
              variant="ghost"
              size="sm"
              className="self-start"
              onClick={() => {
                touch()
                setIngredients((current) => [...current, createRow()])
              }}
            >
              + Dodaj składnik
            </Button>
          </section>

          <section
            aria-label="Podsumowanie wartości odżywczych"
            className="sticky bottom-0 rounded-panel border-[1.5px] border-terracotta/40 bg-terracotta/[0.07] p-4"
          >
            <div className="text-[11px] font-bold uppercase tracking-eyebrow text-terracotta">
              Podsumowanie na żywo
            </div>
            <div className="mt-2 grid grid-cols-1 gap-3 sm:grid-cols-2">
              <SummaryBlock title="Cały przepis" macros={preview.total} />
              {portionsError ? (
                <p className="self-center text-[12.5px] leading-relaxed text-mocha">
                  Podaj poprawną liczbę porcji, aby zobaczyć wartości na porcję.
                </p>
              ) : (
                <SummaryBlock
                  title={portionsLabel ? `Na porcję (${portionsLabel})` : 'Na porcję'}
                  macros={preview.perServing}
                />
              )}
            </div>
          </section>

          {showErrors && !canSave && (
            <p role="alert" className="text-[12.5px] font-semibold text-danger">
              Popraw zaznaczone pola, aby zapisać przepis.
            </p>
          )}

          {saveFailed && (
            <p role="alert" className="text-[12.5px] font-semibold text-danger">
              Nie udało się zapisać przepisu. Sprawdź składniki i spróbuj ponownie.
            </p>
          )}
        </form>
      </Dialog>

      {productModalRowId && (
        <ProductFormModal
          mode="create"
          onClose={() => setProductModalRowId(null)}
          onCreated={handleProductCreated}
          onEditExisting={() => setProductModalRowId(null)}
        />
      )}

      {confirmDiscard && (
        <Dialog
          open
          role="alertdialog"
          onClose={() => setConfirmDiscard(false)}
          title="Masz niezapisane zmiany"
          description="Jeśli zamkniesz formularz teraz, wprowadzone zmiany przepadną."
          footer={
            <>
              <Button variant="ghost" onClick={() => setConfirmDiscard(false)}>
                Zostań w formularzu
              </Button>
              <Button variant="danger" onClick={onClose}>
                Odrzuć zmiany
              </Button>
            </>
          }
        >
          <p className="text-sm text-mocha">
            Możesz wrócić do formularza i zapisać przepis, albo odrzucić zmiany i zamknąć okno.
          </p>
        </Dialog>
      )}
    </>
  )
}

function SummaryBlock({
  title,
  macros,
}: {
  title: string
  macros: { calories: number; protein: number; fat: number; carbohydrates: number }
}) {
  return (
    <div>
      <div className="text-[10.5px] font-bold uppercase tracking-eyebrow text-mocha">{title}</div>
      <div className="mt-1 flex items-baseline gap-2">
        <span className="font-serif text-[28px] leading-none tabular-nums text-espresso">
          {formatKcal(macros.calories)}
        </span>
        <span className="text-[12px] font-bold text-mocha">kcal</span>
      </div>
      <div className="mt-1 text-[13px] tabular-nums text-label">
        B {formatMacro(macros.protein)} · T {formatMacro(macros.fat)} · W{' '}
        {formatMacro(macros.carbohydrates)}
      </div>
    </div>
  )
}

function IconButton({
  label,
  title,
  onClick,
  disabled = false,
  danger = false,
  children,
}: {
  label: string
  title: string
  onClick: () => void
  disabled?: boolean
  danger?: boolean
  children: React.ReactNode
}) {
  return (
    <button
      type="button"
      aria-label={label}
      title={title}
      onClick={onClick}
      disabled={disabled}
      className={[
        'flex h-10 w-10 items-center justify-center rounded-full text-mocha transition-colors disabled:cursor-not-allowed disabled:opacity-40',
        danger ? 'hover:bg-danger/10 hover:text-danger' : 'hover:bg-cream-hover hover:text-espresso',
      ].join(' ')}
    >
      {children}
    </button>
  )
}

function ArrowGlyph({ direction }: { direction: 'up' | 'down' }) {
  return (
    <svg
      width="15"
      height="15"
      viewBox="0 0 20 20"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.9"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      className={direction === 'down' ? 'rotate-180' : undefined}
    >
      <path d="M10 15.5V5M5.5 9.5 10 5l4.5 4.5" />
    </svg>
  )
}

function TrashGlyph() {
  return (
    <svg width="15" height="15" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M4 6h12" />
      <path d="M8 6V4.5h4V6" />
      <path d="M6 6l1 10.5h6L14 6" />
      <path d="M9 9v4.5M11 9v4.5" />
    </svg>
  )
}

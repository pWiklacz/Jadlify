import { type FormEvent, useEffect, useId, useRef, useState } from 'react'
import { useBarcodeLookup } from './useBarcodeLookup'
import { useCreateProduct, useUpdateProduct } from './useProductMutations'
import {
  EXTENDED_NUTRITION_KEYS,
  type CreateProductRequest,
  type ExtendedNutritionKey,
  type Product,
} from './types'

type Mode = 'create' | 'edit'

interface ProductFormModalProps {
  mode: Mode
  /** The product being edited; required in edit mode, ignored in create mode. */
  product?: Product
  onClose: () => void
  /** Optional create-mode callback used by parent flows that need the new product immediately. */
  onCreated?: (product: Product) => void
  /** Invoked when a barcode lookup reveals the code is already in the catalog. */
  onEditExisting: (productId: string) => void
}

type FormState = {
  name: string
  barcode: string
  calories: string
  protein: string
  fat: string
  carbohydrates: string
  packageSizeGrams: string
} & Record<ExtendedNutritionKey, string>

type LookupNotice =
  | { kind: 'none' }
  | { kind: 'found' }
  | { kind: 'notFound' }
  | { kind: 'alreadyInCatalog'; productId: string }

const MACRO_FIELDS = [
  { key: 'calories', label: 'Calories (kcal / 100 g)' },
  { key: 'protein', label: 'Protein (g / 100 g)' },
  { key: 'fat', label: 'Fat (g / 100 g)' },
  { key: 'carbohydrates', label: 'Carbohydrates (g / 100 g)' },
] as const

const EXTENDED_FIELD_LABELS: Record<ExtendedNutritionKey, string> = {
  saturatedFat: 'Saturated fat (g / 100 g)',
  monounsaturatedFat: 'Monounsaturated fat (g / 100 g)',
  polyunsaturatedFat: 'Polyunsaturated fat (g / 100 g)',
  transFat: 'Trans fat (g / 100 g)',
  sugars: 'Sugars (g / 100 g)',
  fiber: 'Fiber (g / 100 g)',
  salt: 'Salt (g / 100 g)',
  sodium: 'Sodium (g / 100 g)',
  potassium: 'Potassium (g / 100 g)',
  calcium: 'Calcium (g / 100 g)',
  iron: 'Iron (g / 100 g)',
  vitaminA: 'Vitamin A (g / 100 g)',
  vitaminC: 'Vitamin C (g / 100 g)',
  vitaminD: 'Vitamin D (g / 100 g)',
}

const EXTENDED_FIELD_GROUPS: { legend: string; keys: ExtendedNutritionKey[] }[] = [
  { legend: 'Fats', keys: ['saturatedFat', 'monounsaturatedFat', 'polyunsaturatedFat', 'transFat'] },
  { legend: 'Carbohydrates', keys: ['sugars', 'fiber'] },
  { legend: 'Minerals', keys: ['salt', 'sodium', 'potassium', 'calcium', 'iron'] },
  { legend: 'Vitamins', keys: ['vitaminA', 'vitaminC', 'vitaminD'] },
]

function toFormState(product?: Product): FormState {
  return {
    name: product?.name ?? '',
    barcode: product?.barcode ?? '',
    calories: numField(product?.calories),
    protein: numField(product?.protein),
    fat: numField(product?.fat),
    carbohydrates: numField(product?.carbohydrates),
    packageSizeGrams: numField(product?.packageSizeGrams),
    ...extendedStrings(product),
  }
}

/** Maps the extended-nutrient values of any product/lookup shape to form-input strings. */
function extendedStrings(
  source: Partial<Record<ExtendedNutritionKey, number | null>> | undefined,
): Record<ExtendedNutritionKey, string> {
  return Object.fromEntries(
    EXTENDED_NUTRITION_KEYS.map((key) => [key, numField(source?.[key])]),
  ) as Record<ExtendedNutritionKey, string>
}

/** Parses the extended-field inputs back to numbers, blank → null. */
function extendedNumbers(form: FormState): Record<ExtendedNutritionKey, number | null> {
  return Object.fromEntries(
    EXTENDED_NUTRITION_KEYS.map((key) => [key, parseOptionalNum(form[key])]),
  ) as Record<ExtendedNutritionKey, number | null>
}

function hasExtendedValues(source: Partial<Record<ExtendedNutritionKey, number | null>> & { packageSizeGrams?: number | null }): boolean {
  return source.packageSizeGrams != null || EXTENDED_NUTRITION_KEYS.some((key) => source[key] != null)
}

function numField(value: number | null | undefined): string {
  return value == null ? '' : String(value)
}

function parseNum(value: string): number {
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : 0
}

/** Blank input → null; otherwise the parsed number (NaN guarded to null). */
function parseOptionalNum(value: string): number | null {
  const trimmed = value.trim()
  if (!trimmed) {
    return null
  }
  const parsed = Number(trimmed)
  return Number.isFinite(parsed) ? parsed : null
}

/**
 * Add/edit product form, presented as a modal dialog. Hosts the barcode lookup,
 * which pre-fills the form per the three US-02 outcomes: Found pre-fills present
 * fields (missing left blank); NotFound/error keeps the barcode and blanks the
 * macros for manual entry; AlreadyInCatalog offers to edit the existing product.
 * The lookup never blocks manual completion (FR-006).
 */
export function ProductFormModal({
  mode,
  product,
  onClose,
  onCreated,
  onEditExisting,
}: ProductFormModalProps) {
  const [form, setForm] = useState<FormState>(() => toFormState(product))
  const [notice, setNotice] = useState<LookupNotice>({ kind: 'none' })
  // Keep the core form compact; auto-expand when editing a product that already
  // carries extended data so it is visible without a click.
  const [showExtended, setShowExtended] = useState(() => (product ? hasExtendedValues(product) : false))

  const dialogRef = useRef<HTMLDivElement>(null)
  const nameRef = useRef<HTMLInputElement>(null)
  const baseId = useId()
  const titleId = `${baseId}-title`
  const extendedId = `${baseId}-extended`

  const lookup = useBarcodeLookup()
  const createProduct = useCreateProduct()
  const updateProduct = useUpdateProduct()

  const isSaving = createProduct.isPending || updateProduct.isPending
  const saveFailed = createProduct.isError || updateProduct.isError
  const trimmedName = form.name.trim()

  // ESC to close + a Tab focus trap so keyboard users stay inside the dialog.
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
      if (event.key === 'Escape') {
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
  }, [onClose])

  // Land focus on the first field when the dialog opens.
  useEffect(() => {
    nameRef.current?.focus()
  }, [])

  function setField(key: keyof FormState, value: string) {
    setForm((prev) => ({ ...prev, [key]: value }))
  }

  async function handleLookup() {
    const code = form.barcode.trim()
    if (!code) {
      return
    }
    try {
      const result = await lookup.mutateAsync(code)
      if (result.outcome === 'AlreadyInCatalog' && result.existingProductId) {
        setNotice({ kind: 'alreadyInCatalog', productId: result.existingProductId })
        return
      }
      if (result.outcome === 'Found') {
        setForm((prev) => ({
          ...prev,
          name: result.name ?? '',
          calories: numField(result.calories),
          protein: numField(result.protein),
          fat: numField(result.fat),
          carbohydrates: numField(result.carbohydrates),
          packageSizeGrams: numField(result.packageSizeGrams),
          ...extendedStrings(result),
        }))
        // Reveal the extended section if the lookup actually returned any of it.
        if (hasExtendedValues(result)) {
          setShowExtended(true)
        }
        setNotice({ kind: 'found' })
        return
      }
      // NotFound: keep the barcode, blank the macros, let the user fill manually.
      setForm((prev) => ({ ...prev, calories: '', protein: '', fat: '', carbohydrates: '' }))
      setNotice({ kind: 'notFound' })
    } catch {
      // The endpoint always answers 200, but a transport failure must not block
      // manual entry (FR-006) — fall through to the manual-fill path.
      setNotice({ kind: 'notFound' })
    }
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    if (!trimmedName) {
      return
    }
    const body: CreateProductRequest = {
      name: trimmedName,
      barcode: form.barcode.trim() || null,
      calories: parseNum(form.calories),
      protein: parseNum(form.protein),
      fat: parseNum(form.fat),
      carbohydrates: parseNum(form.carbohydrates),
      packageSizeGrams: parseOptionalNum(form.packageSizeGrams),
      ...extendedNumbers(form),
    }
    if (mode === 'edit' && product) {
      updateProduct.mutate({ id: product.id, body }, { onSuccess: onClose })
    } else {
      createProduct.mutate(body, {
        onSuccess: (createdProduct) => {
          onCreated?.(createdProduct)
          onClose()
        },
      })
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) {
          onClose()
        }
      }}
    >
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="flex max-h-full w-full max-w-md flex-col overflow-y-auto rounded-lg bg-white p-5 shadow-xl"
      >
        <div className="mb-4 flex items-start justify-between gap-4">
          <h2 id={titleId} className="text-xl font-bold">
            {mode === 'edit' ? 'Edit product' : 'Add product'}
          </h2>
          <button
            type="button"
            aria-label="Close"
            onClick={onClose}
            className="rounded-md px-2 text-2xl leading-none text-slate-500 hover:bg-slate-100"
          >
            ×
          </button>
        </div>

        <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
          <div className="flex flex-col gap-1">
            <label htmlFor={`${baseId}-name`} className="text-sm font-medium">
              Name
            </label>
            <input
              ref={nameRef}
              id={`${baseId}-name`}
              type="text"
              required
              value={form.name}
              onChange={(event) => setField('name', event.target.value)}
              className="rounded-md border border-slate-300 px-3 py-2"
            />
          </div>

          <div className="flex flex-col gap-1">
            <label htmlFor={`${baseId}-barcode`} className="text-sm font-medium">
              Barcode
            </label>
            <div className="flex gap-2">
              <input
                id={`${baseId}-barcode`}
                type="text"
                inputMode="numeric"
                value={form.barcode}
                onChange={(event) => setField('barcode', event.target.value)}
                className="flex-1 rounded-md border border-slate-300 px-3 py-2"
              />
              <button
                type="button"
                onClick={handleLookup}
                disabled={!form.barcode.trim() || lookup.isPending}
                className="shrink-0 rounded-md bg-slate-900 px-3 py-2 text-sm font-medium text-white disabled:opacity-50"
              >
                {lookup.isPending ? 'Looking up…' : 'Look up'}
              </button>
            </div>
            <LookupNoticeBanner notice={notice} onEditExisting={onEditExisting} />
          </div>

          <div className="grid grid-cols-2 gap-3">
            {MACRO_FIELDS.map((field) => (
              <div key={field.key} className="flex flex-col gap-1">
                <label htmlFor={`${baseId}-${field.key}`} className="text-sm font-medium">
                  {field.label}
                </label>
                <input
                  id={`${baseId}-${field.key}`}
                  type="number"
                  min="0"
                  step="any"
                  value={form[field.key]}
                  onChange={(event) => setField(field.key, event.target.value)}
                  className="rounded-md border border-slate-300 px-3 py-2"
                />
              </div>
            ))}
          </div>

          <div className="flex flex-col gap-3 border-t border-slate-200 pt-3">
            <button
              type="button"
              onClick={() => setShowExtended((value) => !value)}
              aria-expanded={showExtended}
              aria-controls={extendedId}
              className="self-start text-sm font-medium text-slate-700 underline"
            >
              {showExtended ? 'Hide additional nutrition' : 'Additional nutrition'}
            </button>

            {showExtended && (
              <div id={extendedId} className="flex flex-col gap-4">
                <div className="flex flex-col gap-1">
                  <label htmlFor={`${baseId}-packageSizeGrams`} className="text-sm font-medium">
                    Package size (g)
                  </label>
                  <input
                    id={`${baseId}-packageSizeGrams`}
                    type="number"
                    min="0"
                    step="any"
                    value={form.packageSizeGrams}
                    onChange={(event) => setField('packageSizeGrams', event.target.value)}
                    className="rounded-md border border-slate-300 px-3 py-2"
                  />
                </div>

                {EXTENDED_FIELD_GROUPS.map((group) => (
                  <fieldset key={group.legend} className="flex flex-col gap-2">
                    <legend className="text-sm font-semibold text-slate-600">{group.legend}</legend>
                    <div className="grid grid-cols-2 gap-3">
                      {group.keys.map((key) => (
                        <div key={key} className="flex flex-col gap-1">
                          <label htmlFor={`${baseId}-${key}`} className="text-sm font-medium">
                            {EXTENDED_FIELD_LABELS[key]}
                          </label>
                          <input
                            id={`${baseId}-${key}`}
                            type="number"
                            min="0"
                            step="any"
                            value={form[key]}
                            onChange={(event) => setField(key, event.target.value)}
                            className="rounded-md border border-slate-300 px-3 py-2"
                          />
                        </div>
                      ))}
                    </div>
                  </fieldset>
                ))}
              </div>
            )}
          </div>

          {saveFailed && (
            <p role="alert" className="text-sm text-red-600">
              Could not save the product. Please try again.
            </p>
          )}

          <div className="mt-1 flex justify-end gap-2">
            <button
              type="button"
              onClick={onClose}
              className="rounded-md px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={!trimmedName || isSaving}
              className="rounded-md bg-slate-900 px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
            >
              {isSaving ? 'Saving…' : 'Save'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

function LookupNoticeBanner({
  notice,
  onEditExisting,
}: {
  notice: LookupNotice
  onEditExisting: (productId: string) => void
}) {
  if (notice.kind === 'none') {
    return null
  }
  if (notice.kind === 'found') {
    return (
      <p role="status" className="text-sm text-emerald-700">
        Pre-filled from Open Food Facts. Complete any blank fields.
      </p>
    )
  }
  if (notice.kind === 'notFound') {
    return (
      <p role="status" className="text-sm text-slate-600">
        No data found — fill the details manually.
      </p>
    )
  }
  return (
    <div role="status" className="flex flex-wrap items-center gap-2 text-sm text-slate-700">
      <span>This barcode is already in your catalog.</span>
      <button
        type="button"
        onClick={() => onEditExisting(notice.productId)}
        className="font-medium text-slate-900 underline"
      >
        Edit existing product
      </button>
    </div>
  )
}

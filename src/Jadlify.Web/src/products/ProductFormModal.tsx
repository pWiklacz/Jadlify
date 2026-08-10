import { type FormEvent, useId, useMemo, useRef, useState } from 'react'
import { Button } from '../ui/Button'
import { Dialog } from '../ui/Dialog'
import { Field, TextInput } from '../ui/Field'
import { parseDecimal } from '../ui/formatters'
import { useBarcodeLookup } from './useBarcodeLookup'
import { useCreateProduct, useUpdateProduct } from './useProductMutations'
import {
  displayFieldToGrams,
  EXTENDED_FIELD_GROUPS,
  EXTENDED_FIELDS,
  gramsToDisplayField,
} from './nutrients'
import {
  categoryLabel,
  EXTENDED_NUTRITION_KEYS,
  PRODUCT_CATEGORIES,
  type CreateProductRequest,
  type ExtendedNutritionKey,
  type Product,
  type ProductCategory,
} from './types'

type Mode = 'create' | 'edit'

interface ProductFormModalProps {
  mode: Mode
  /** The product being edited; required in edit mode, ignored in create mode. */
  product?: Product
  onClose: () => void
  /** Create-mode callback: the freshly created product (used by the recipe flow). */
  onCreated?: (product: Product) => void
  /** Fires after any successful save (create or edit); the parent can toast. */
  onSaved?: (mode: Mode, product: Product) => void
  /** Invoked when a barcode lookup reveals the code is already in the catalog. */
  onEditExisting: (productId: string) => void
}

type CoreKey = 'calories' | 'protein' | 'fat' | 'carbohydrates'

const CORE_FIELDS: { key: CoreKey; label: string; unit: string }[] = [
  { key: 'calories', label: 'Kalorie', unit: 'kcal' },
  { key: 'protein', label: 'Białko', unit: 'g' },
  { key: 'fat', label: 'Tłuszcz', unit: 'g' },
  { key: 'carbohydrates', label: 'Węglowodany', unit: 'g' },
]

/** Labels shown when a Found lookup is missing a core value ("partial data"). */
const CORE_MISSING_LABELS: Record<'name' | CoreKey, string> = {
  name: 'nazwa',
  calories: 'kalorie',
  protein: 'białko',
  fat: 'tłuszcz',
  carbohydrates: 'węglowodany',
}

type FormState = {
  name: string
  brand: string
  barcode: string
  category: '' | ProductCategory
  packageSizeGrams: string
} & Record<CoreKey, string> &
  Record<`ext_${ExtendedNutritionKey}`, string>

type LookupNotice =
  | { kind: 'none' }
  | { kind: 'found' }
  | { kind: 'partial'; missing: string[] }
  | { kind: 'notFound' }
  | { kind: 'error' }
  | { kind: 'dup'; productId: string; name: string | null }

interface FormErrors {
  name: boolean
  core: CoreKey[]
  packageSize: boolean
  extended: ExtendedNutritionKey[]
}

const NO_ERRORS: FormErrors = { name: false, core: [], packageSize: false, extended: [] }

function toFormState(product?: Product): FormState {
  const base = {
    name: product?.name ?? '',
    brand: product?.brand ?? '',
    barcode: product?.barcode ?? '',
    category: (product?.category ?? '') as '' | ProductCategory,
    packageSizeGrams: numField(product?.packageSizeGrams),
    calories: numField(product?.calories),
    protein: numField(product?.protein),
    fat: numField(product?.fat),
    carbohydrates: numField(product?.carbohydrates),
  } as FormState

  for (const meta of EXTENDED_FIELDS) {
    base[`ext_${meta.key}`] = gramsToDisplayField(product?.[meta.key], meta.unit)
  }
  return base
}

function numField(value: number | null | undefined): string {
  return value == null ? '' : String(value)
}

/** True once any extended nutrient carries a value, so an edited product opens the section expanded. */
function hasExtendedValues(product?: Product): boolean {
  if (!product) {
    return false
  }
  return product.packageSizeGrams != null || EXTENDED_NUTRITION_KEYS.some((key) => product[key] != null)
}

/**
 * Add/edit product form, hosted in the shared {@link Dialog} (a centred card on
 * desktop, a bottom sheet on mobile). Carries the full catalog contract: brand,
 * category and package metadata, the extended per-100g profile in human units,
 * the five barcode-lookup outcomes, and an unsaved-changes guard on close.
 */
export function ProductFormModal({
  mode,
  product,
  onClose,
  onCreated,
  onSaved,
  onEditExisting,
}: ProductFormModalProps) {
  const initialRef = useRef<FormState>(toFormState(product))
  const [form, setForm] = useState<FormState>(initialRef.current)
  const [notice, setNotice] = useState<LookupNotice>({ kind: 'none' })
  const [categorySuggested, setCategorySuggested] = useState(false)
  const [errors, setErrors] = useState<FormErrors>(NO_ERRORS)
  const [showExtended, setShowExtended] = useState(() => hasExtendedValues(product))
  const [confirmLeave, setConfirmLeave] = useState(false)

  const nameRef = useRef<HTMLInputElement>(null)
  const baseId = useId()
  const formId = `${baseId}-form`

  const lookup = useBarcodeLookup()
  const createProduct = useCreateProduct()
  const updateProduct = useUpdateProduct()

  const isSaving = createProduct.isPending || updateProduct.isPending
  const saveFailed = createProduct.isError || updateProduct.isError
  const trimmedName = form.name.trim()

  const filledExtendedCount = useMemo(
    () => EXTENDED_FIELDS.filter((meta) => form[`ext_${meta.key}`].trim() !== '').length,
    [form],
  )

  const isDirty = useMemo(
    () => JSON.stringify(form) !== JSON.stringify(initialRef.current),
    [form],
  )

  function setField(key: keyof FormState, value: string) {
    setForm((prev) => ({ ...prev, [key]: value }))
  }

  function requestClose() {
    if (isDirty) {
      setConfirmLeave(true)
      return
    }
    onClose()
  }

  function applyLookupPrefill(result: Awaited<ReturnType<typeof lookup.mutateAsync>>) {
    setForm((prev) => {
      const next: FormState = {
        ...prev,
        name: result.name ?? prev.name,
        brand: result.brand ?? prev.brand,
        category: (result.category ?? prev.category) as '' | ProductCategory,
        calories: numField(result.calories),
        protein: numField(result.protein),
        fat: numField(result.fat),
        carbohydrates: numField(result.carbohydrates),
        packageSizeGrams: numField(result.packageSizeGrams),
      }
      for (const meta of EXTENDED_FIELDS) {
        next[`ext_${meta.key}`] = gramsToDisplayField(result[meta.key], meta.unit)
      }
      return next
    })
    setCategorySuggested(result.category != null)
    if (
      result.packageSizeGrams != null ||
      EXTENDED_NUTRITION_KEYS.some((key) => result[key] != null)
    ) {
      setShowExtended(true)
    }
  }

  async function handleLookup() {
    const code = form.barcode.trim()
    if (!code) {
      return
    }
    try {
      const result = await lookup.mutateAsync(code)
      if (result.outcome === 'AlreadyInCatalog' && result.existingProductId) {
        setNotice({ kind: 'dup', productId: result.existingProductId, name: result.name })
        return
      }
      if (result.outcome === 'Found') {
        applyLookupPrefill(result)
        const missing: string[] = []
        if (!result.name?.trim()) missing.push(CORE_MISSING_LABELS.name)
        for (const field of CORE_FIELDS) {
          if (result[field.key] == null) missing.push(CORE_MISSING_LABELS[field.key])
        }
        setNotice(missing.length > 0 ? { kind: 'partial', missing } : { kind: 'found' })
        return
      }
      // NotFound: keep the barcode, blank the macros for manual entry.
      setForm((prev) => ({ ...prev, calories: '', protein: '', fat: '', carbohydrates: '' }))
      setNotice({ kind: 'notFound' })
    } catch {
      // The endpoint answers 200 for a miss; a transport failure must not block
      // manual entry (FR-006) — keep the typed data and surface a soft error.
      setNotice({ kind: 'error' })
    }
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault()

    const nameInvalid = trimmedName === ''
    const coreInvalid = CORE_FIELDS.filter((field) => {
      const value = parseDecimal(form[field.key])
      return value == null || value < 0
    }).map((field) => field.key)

    const packageValue = form.packageSizeGrams.trim()
    const packageInvalid = packageValue !== '' && (() => {
      const parsed = parseDecimal(packageValue)
      return parsed == null || parsed <= 0
    })()

    const extendedInvalid: ExtendedNutritionKey[] = []
    const extendedGrams: Partial<Record<ExtendedNutritionKey, number | null>> = {}
    for (const meta of EXTENDED_FIELDS) {
      const grams = displayFieldToGrams(form[`ext_${meta.key}`], meta.unit)
      if (grams === undefined || (grams != null && grams < 0)) {
        extendedInvalid.push(meta.key)
      } else {
        extendedGrams[meta.key] = grams
      }
    }

    const nextErrors: FormErrors = {
      name: nameInvalid,
      core: coreInvalid,
      packageSize: packageInvalid,
      extended: extendedInvalid,
    }
    setErrors(nextErrors)

    if (nameInvalid || coreInvalid.length > 0 || packageInvalid || extendedInvalid.length > 0) {
      if (extendedInvalid.length > 0) {
        setShowExtended(true)
      }
      focusFirstError(nextErrors, baseId)
      return
    }

    const body: CreateProductRequest = {
      name: trimmedName,
      barcode: form.barcode.trim() || null,
      brand: form.brand.trim() || null,
      category: form.category || null,
      calories: parseDecimal(form.calories) ?? 0,
      protein: parseDecimal(form.protein) ?? 0,
      fat: parseDecimal(form.fat) ?? 0,
      carbohydrates: parseDecimal(form.carbohydrates) ?? 0,
      packageSizeGrams: packageValue ? parseDecimal(packageValue) : null,
      saturatedFat: extendedGrams.saturatedFat ?? null,
      monounsaturatedFat: extendedGrams.monounsaturatedFat ?? null,
      polyunsaturatedFat: extendedGrams.polyunsaturatedFat ?? null,
      transFat: extendedGrams.transFat ?? null,
      sugars: extendedGrams.sugars ?? null,
      fiber: extendedGrams.fiber ?? null,
      salt: extendedGrams.salt ?? null,
      sodium: extendedGrams.sodium ?? null,
      potassium: extendedGrams.potassium ?? null,
      calcium: extendedGrams.calcium ?? null,
      iron: extendedGrams.iron ?? null,
      vitaminA: extendedGrams.vitaminA ?? null,
      vitaminC: extendedGrams.vitaminC ?? null,
      vitaminD: extendedGrams.vitaminD ?? null,
    }

    if (mode === 'edit' && product) {
      updateProduct.mutate(
        { id: product.id, body },
        {
          onSuccess: () => {
            onSaved?.('edit', { ...product, ...body })
            onClose()
          },
        },
      )
    } else {
      createProduct.mutate(body, {
        onSuccess: (created) => {
          onCreated?.(created)
          onSaved?.('create', created)
          onClose()
        },
      })
    }
  }

  const title = mode === 'edit' ? 'Edytuj produkt' : 'Dodaj produkt'
  const saveLabel = mode === 'edit' ? 'Zapisz zmiany' : 'Zapisz produkt'

  return (
    <>
      <Dialog
        open
        onClose={requestClose}
        title={title}
        size="lg"
        initialFocusRef={nameRef}
        footer={
          <>
            <Button variant="ghost" onClick={requestClose} disabled={isSaving}>
              Anuluj
            </Button>
            <Button type="submit" form={formId} isLoading={isSaving}>
              {saveLabel}
            </Button>
          </>
        }
      >
        <form id={formId} className="flex flex-col gap-5" onSubmit={handleSubmit} noValidate>
          {/* 1. Identity */}
          <Field
            id={`${baseId}-name`}
            label="Nazwa produktu"
            error={errors.name ? 'Podaj nazwę produktu.' : null}
          >
            <TextInput
              ref={nameRef}
              id={`${baseId}-name`}
              type="text"
              value={form.name}
              invalid={errors.name}
              onChange={(event) => setField('name', event.target.value)}
              placeholder="Np. Serek wiejski"
            />
          </Field>

          <Field
            id={`${baseId}-brand`}
            label="Marka"
            optional
            hint="Marka pomaga rozróżniać podobne produkty i bywa uzupełniana z danych kodu kreskowego."
          >
            <TextInput
              id={`${baseId}-brand`}
              type="text"
              value={form.brand}
              onChange={(event) => setField('brand', event.target.value)}
              placeholder="Np. Piątnica"
            />
          </Field>

          {/* 2. Barcode lookup */}
          <div className="rounded-panel border border-cream-border bg-cream-panel p-4">
            <Field
              id={`${baseId}-barcode`}
              label="Kod kreskowy"
              optional
              hint="Wpisz lub wklej kod z opakowania — spróbujemy pobrać dane i wypełnić puste pola. Wszystko możesz uzupełnić ręcznie."
            >
              <div className="flex flex-wrap gap-2">
                <TextInput
                  id={`${baseId}-barcode`}
                  type="text"
                  inputMode="numeric"
                  value={form.barcode}
                  onChange={(event) => setField('barcode', event.target.value)}
                  placeholder="Np. 5900531000019"
                  className="min-w-0 flex-1 tabular-nums"
                />
                <Button
                  variant="secondary"
                  onClick={handleLookup}
                  disabled={!form.barcode.trim() || lookup.isPending}
                  isLoading={lookup.isPending}
                >
                  {lookup.isPending ? 'Szukamy…' : 'Wyszukaj'}
                </Button>
              </div>
            </Field>
            <LookupNoticeBanner
              notice={notice}
              onOpenExisting={onEditExisting}
              onDismiss={() => setNotice({ kind: 'none' })}
            />
          </div>

          {/* 3. Required macros */}
          <fieldset
            className={[
              'rounded-panel border bg-terracotta/[0.05] p-4',
              errors.core.length > 0 ? 'border-danger' : 'border-terracotta/45',
            ].join(' ')}
          >
            <legend className="px-1 text-[11px] font-bold uppercase tracking-eyebrow text-terracotta">
              Wartości odżywcze na 100 g — wymagane
            </legend>
            <p className="mb-3 mt-1 text-[12.5px] leading-relaxed text-mocha">
              Wpisz wartości z etykiety w przeliczeniu na 100 g produktu. Mogą wynosić 0, dopuszczalne
              są ułamki (np. 4,5).
            </p>
            <div className="grid grid-cols-2 gap-2.5 design:grid-cols-4">
              {CORE_FIELDS.map((field) => (
                <Field
                  key={field.key}
                  id={`${baseId}-${field.key}`}
                  label={`${field.label} (${field.unit})`}
                  labelVariant="plain"
                >
                  <TextInput
                    id={`${baseId}-${field.key}`}
                    type="text"
                    inputMode="decimal"
                    value={form[field.key]}
                    invalid={errors.core.includes(field.key)}
                    onChange={(event) => setField(field.key, event.target.value)}
                    placeholder="0"
                    className="text-right tabular-nums"
                  />
                </Field>
              ))}
            </div>
            {errors.core.length > 0 && (
              <p role="alert" className="mt-2 text-[12.5px] font-semibold text-danger">
                ▲ Uzupełnij wszystkie cztery wartości na 100 g — liczby 0 lub większe.
              </p>
            )}
          </fieldset>

          {/* 4. Category + package */}
          <div className="grid grid-cols-1 gap-4 design:grid-cols-2">
            <Field
              id={`${baseId}-category`}
              label="Kategoria"
              optional
              hint="Kategoria pomaga grupować produkty na liście zakupów. Możesz ją pominąć."
            >
              <select
                id={`${baseId}-category`}
                value={form.category}
                onChange={(event) => {
                  setField('category', event.target.value)
                  setCategorySuggested(false)
                }}
                className="w-full rounded-field border-[1.5px] border-cream-border bg-cream-input px-3.5 py-3 text-espresso outline-none focus:border-terracotta"
              >
                <option value="">Bez kategorii</option>
                {PRODUCT_CATEGORIES.map((c) => (
                  <option key={c.value} value={c.value}>
                    {c.label}
                  </option>
                ))}
              </select>
              {categorySuggested && form.category && (
                <p role="status" className="text-[12px] leading-snug text-warning-ink">
                  Kategoria „{categoryLabel(form.category)}” to propozycja na podstawie kodu
                  kreskowego — możesz ją zmienić lub usunąć.
                </p>
              )}
            </Field>

            <Field
              id={`${baseId}-packageSizeGrams`}
              label="Opakowanie (g)"
              optional
              hint="Rozmiar całego opakowania — pokażemy dodatkowo wyliczone wartości dla opakowania."
              error={errors.packageSize ? 'Rozmiar opakowania musi być liczbą większą od zera.' : null}
            >
              <TextInput
                id={`${baseId}-packageSizeGrams`}
                type="text"
                inputMode="decimal"
                value={form.packageSizeGrams}
                invalid={errors.packageSize}
                onChange={(event) => setField('packageSizeGrams', event.target.value)}
                placeholder="Np. 250"
                className="text-right tabular-nums"
              />
            </Field>
          </div>

          {/* 5. Extended nutrition */}
          <div className="border-t border-dotted border-cream-line pt-1">
            <button
              type="button"
              onClick={() => setShowExtended((value) => !value)}
              aria-expanded={showExtended}
              aria-controls={`${baseId}-extended`}
              className="flex w-full items-center gap-2 rounded-field px-1 py-2 text-left text-[13.5px] font-semibold text-label transition-colors hover:text-terracotta"
            >
              <span className="flex-1">
                Dodatkowe wartości odżywcze{' '}
                <span className="font-medium text-mocha">(opcjonalne, na 100 g)</span>
              </span>
              {filledExtendedCount > 0 && (
                <span className="tabular-nums text-mocha">{filledExtendedCount}/14</span>
              )}
              <span aria-hidden="true" className="text-mocha">
                {showExtended ? '▾' : '▸'}
              </span>
            </button>

            {showExtended && (
              <div id={`${baseId}-extended`} className="mt-2 flex flex-col gap-4">
                {EXTENDED_FIELD_GROUPS.map((group) => (
                  <fieldset key={group.legend} className="flex flex-col gap-2">
                    <legend className="text-[10.5px] font-bold uppercase tracking-eyebrow text-mocha">
                      {group.legend}
                    </legend>
                    <div className="grid grid-cols-2 gap-2.5 design:grid-cols-3">
                      {group.fields.map((meta) => (
                        <Field
                          key={meta.key}
                          id={`${baseId}-ext-${meta.key}`}
                          label={`${meta.label} (${meta.unit})`}
                          labelVariant="plain"
                        >
                          <TextInput
                            id={`${baseId}-ext-${meta.key}`}
                            type="text"
                            inputMode="decimal"
                            value={form[`ext_${meta.key}`]}
                            invalid={errors.extended.includes(meta.key)}
                            onChange={(event) => setField(`ext_${meta.key}`, event.target.value)}
                            placeholder="—"
                            className="text-right tabular-nums"
                          />
                        </Field>
                      ))}
                    </div>
                  </fieldset>
                ))}
                {errors.extended.length > 0 && (
                  <p role="alert" className="text-[12.5px] font-semibold text-danger">
                    ▲ Dodatkowe wartości muszą być liczbami 0 lub większymi — popraw zaznaczone pola
                    albo zostaw je puste.
                  </p>
                )}
              </div>
            )}
          </div>

          {saveFailed && (
            <p role="alert" className="text-sm font-semibold text-danger">
              Nie udało się zapisać produktu. Wpisane dane zostały zachowane — spróbuj ponownie.
            </p>
          )}
        </form>
      </Dialog>

      {confirmLeave && (
        <Dialog
          open
          onClose={() => setConfirmLeave(false)}
          role="alertdialog"
          title="Masz niezapisane zmiany"
          description="Jeśli opuścisz formularz, zmiany w produkcie zostaną utracone."
          footer={
            <>
              <Button variant="danger" onClick={onClose}>
                Odrzuć zmiany
              </Button>
              <Button variant="secondary" onClick={() => setConfirmLeave(false)}>
                Zostań w formularzu
              </Button>
            </>
          }
        >
          <p className="text-sm text-mocha">
            Możesz zostać i dokończyć edycję albo odrzucić zmiany i zamknąć formularz.
          </p>
        </Dialog>
      )}
    </>
  )
}

/** Focuses the first invalid control so keyboard users land on what needs fixing. */
function focusFirstError(errors: FormErrors, baseId: string) {
  const targetId = errors.name
    ? `${baseId}-name`
    : errors.core.length > 0
      ? `${baseId}-${errors.core[0]}`
      : errors.packageSize
        ? `${baseId}-packageSizeGrams`
        : errors.extended.length > 0
          ? `${baseId}-ext-${errors.extended[0]}`
          : null
  if (targetId) {
    requestAnimationFrame(() => document.getElementById(targetId)?.focus())
  }
}

function LookupNoticeBanner({
  notice,
  onOpenExisting,
  onDismiss,
}: {
  notice: LookupNotice
  onOpenExisting: (productId: string) => void
  onDismiss: () => void
}) {
  if (notice.kind === 'none') {
    return null
  }
  if (notice.kind === 'found') {
    return (
      <p
        role="status"
        className="mt-2.5 rounded-field border border-success/35 bg-success/10 px-3 py-2.5 text-[13px] leading-snug text-success-ink"
      >
        <b>Znaleziono dane produktu.</b> Puste pola zostały wypełnione — sprawdź wartości i zapisz.
      </p>
    )
  }
  if (notice.kind === 'partial') {
    return (
      <p
        role="status"
        className="mt-2.5 rounded-field border border-warning/40 bg-warning/10 px-3 py-2.5 text-[13px] leading-snug text-warning-ink"
      >
        <b>Znaleziono częściowe dane.</b> Uzupełnij ręcznie: {notice.missing.join(', ')}.
      </p>
    )
  }
  if (notice.kind === 'notFound') {
    return (
      <p
        role="status"
        className="mt-2.5 rounded-field border border-dashed border-cream-line bg-cream px-3 py-2.5 text-[13px] leading-snug text-mocha"
      >
        Nie znaleziono danych dla tego kodu. Uzupełnij produkt ręcznie — kod zostanie zapisany razem z
        produktem.
      </p>
    )
  }
  if (notice.kind === 'error') {
    return (
      <p
        role="status"
        className="mt-2.5 rounded-field border border-warning/40 bg-warning/10 px-3 py-2.5 text-[13px] leading-snug text-warning-ink"
      >
        Automatyczne wyszukiwanie jest chwilowo niedostępne. Kod został zachowany — możesz uzupełnić
        dane ręcznie albo spróbować ponownie.
      </p>
    )
  }
  return (
    <div
      role="status"
      className="mt-2.5 rounded-field border border-terracotta/35 bg-terracotta/[0.08] px-3 py-2.5 text-[13px] leading-snug text-espresso"
    >
      <p>
        Ten kod jest już przypisany do produktu
        {notice.name ? ` „${notice.name}”` : ''} w Twoim katalogu. Nie utworzymy duplikatu.
      </p>
      <div className="mt-2 flex flex-wrap gap-2">
        <Button variant="secondary" size="sm" onClick={() => onOpenExisting(notice.productId)}>
          Otwórz istniejący produkt
        </Button>
        <Button variant="ghost" size="sm" onClick={onDismiss}>
          Zostań w formularzu
        </Button>
      </div>
    </div>
  )
}

import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button } from '../ui/Button'
import { Dialog } from '../ui/Dialog'
import { Stepper } from '../ui/Stepper'
import { formatKcal, mealTypeLabel } from '../ui/formatters'
import { useProductSearch } from '../recipes/useProductSearch'
import { useRecipeCatalog } from '../recipes/useRecipeCatalog'
import { PRODUCT_GRAMS_STEP, RECIPE_PORTION_STEP } from './MealEntryCard'
import { formatDayLong } from './plannerFormat'
import { mealTypes, type AddMealPlanEntryRequest, type MealType } from './types'

type EntryMode = 'recipe' | 'product'

interface AddMealEntryDialogProps {
  /** The day the entry lands on; editable inside the dialog. */
  date: string
  /** Pre-selected meal type (e.g. the group whose "add" was clicked). */
  initialMealType?: MealType
  /** Recipe pre-selected by an add-to-plan handoff. */
  initialRecipeId?: string
  /** True while the create mutation is in flight. */
  isSubmitting: boolean
  /** True when the last submit failed; keeps the dialog open with the choices intact. */
  isError: boolean
  onClose: () => void
  onSubmit: (body: AddMealPlanEntryRequest, targetDate: string) => void
}

/**
 * The add-meal dialog. One entry is either a recipe (measured in half-portion
 * steps) or a single product (measured in 10 g steps) — the mode tabs and the
 * matching stepper keep the unit explicit so neither is ever inferred. The target
 * day is editable, and empty catalogs route out to create a recipe or product.
 */
export function AddMealEntryDialog({
  date,
  initialMealType,
  initialRecipeId,
  isSubmitting,
  isError,
  onClose,
  onSubmit,
}: AddMealEntryDialogProps) {
  const navigate = useNavigate()
  const [mode, setMode] = useState<EntryMode>('recipe')
  const [targetDate, setTargetDate] = useState(date)
  const [mealType, setMealType] = useState<MealType>(initialMealType ?? 'Breakfast')
  const [recipeSearch, setRecipeSearch] = useState('')
  const [productSearch, setProductSearch] = useState('')
  const [recipeId, setRecipeId] = useState(initialRecipeId ?? '')
  const [productId, setProductId] = useState('')
  const [portions, setPortions] = useState(1)
  const [grams, setGrams] = useState(100)
  const [showSourceError, setShowSourceError] = useState(false)

  const recipeCatalog = useRecipeCatalog({ search: recipeSearch, sort: 'NameAsc' })
  const productResults = useProductSearch(productSearch)

  const recipes = recipeCatalog.data?.items ?? []
  const products = productResults.data ?? []

  const sourceSelected = mode === 'recipe' ? Boolean(recipeId) : Boolean(productId)

  function submit() {
    if (!sourceSelected) {
      setShowSourceError(true)
      return
    }

    const body: AddMealPlanEntryRequest =
      mode === 'recipe'
        ? { date: targetDate, mealType, recipeId, portions }
        : { date: targetDate, mealType, productId, grams }
    onSubmit(body, targetDate)
  }

  return (
    <Dialog
      open
      onClose={onClose}
      size="lg"
      title="Dodaj posiłek"
      description={`Do planu: ${formatDayLong(targetDate)}`}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={isSubmitting}>
            Anuluj
          </Button>
          <Button onClick={submit} isLoading={isSubmitting} disabled={!sourceSelected}>
            Dodaj do planu
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-4">
        <label className="flex flex-col gap-1.5 text-[11px] font-bold uppercase tracking-eyebrow text-label">
          Dzień docelowy
          <input
            type="date"
            aria-label="Dzień docelowy"
            value={targetDate}
            onChange={(event) => setTargetDate(event.currentTarget.value || date)}
            className="w-full rounded-field border-[1.5px] border-cream-border bg-cream-input px-3.5 py-3 text-base font-normal text-espresso outline-none focus:border-terracotta"
          />
        </label>

        <div role="group" aria-label="Rodzaj wpisu" className="flex gap-1 rounded-pill bg-cream-hover p-1">
          {(['recipe', 'product'] as EntryMode[]).map((option) => {
            const active = option === mode
            return (
              <button
                key={option}
                type="button"
                aria-pressed={active}
                onClick={() => {
                  setMode(option)
                  setShowSourceError(false)
                }}
                className={[
                  'flex-1 rounded-pill px-3 py-2.5 text-[13.5px] font-bold transition-colors',
                  active ? 'bg-cream text-espresso shadow-sm' : 'text-mocha hover:text-espresso',
                ].join(' ')}
              >
                {option === 'recipe' ? 'Przepis' : 'Produkt'}
              </button>
            )
          })}
        </div>

        {mode === 'recipe' ? (
          <SourceSearch
            kind="recipe"
            search={recipeSearch}
            onSearch={setRecipeSearch}
            isPending={recipeCatalog.isPending}
            isError={recipeCatalog.isError}
            options={recipes.map((recipe) => ({
              id: recipe.id,
              name: recipe.name,
              kcal: recipe.perServingMacros.calories,
              suffix: '/ porcja',
            }))}
            selectedId={recipeId}
            onSelect={(id) => {
              setRecipeId(id)
              setShowSourceError(false)
            }}
            onCreate={() => {
              onClose()
              navigate('/recipes')
            }}
          />
        ) : (
          <SourceSearch
            kind="product"
            search={productSearch}
            onSearch={setProductSearch}
            isPending={productResults.isPending}
            isError={productResults.isError}
            options={products.map((product) => ({
              id: product.id,
              name: product.name,
              kcal: product.calories,
              suffix: '/ 100 g',
            }))}
            selectedId={productId}
            onSelect={(id) => {
              setProductId(id)
              setShowSourceError(false)
            }}
            onCreate={() => {
              onClose()
              navigate('/products')
            }}
          />
        )}

        {showSourceError && !sourceSelected && (
          <p role="alert" className="text-[12.5px] font-semibold text-danger">
            ▲ {mode === 'recipe' ? 'Wybierz przepis z listy.' : 'Wybierz produkt z listy.'}
          </p>
        )}

        <div>
          <p className="mb-2 text-[11px] font-bold uppercase tracking-eyebrow text-label">
            Typ posiłku
          </p>
          <div className="flex flex-wrap gap-1.5">
            {mealTypes.map((type) => {
              const active = type === mealType
              return (
                <button
                  key={type}
                  type="button"
                  aria-pressed={active}
                  onClick={() => setMealType(type)}
                  className={[
                    'min-h-[40px] rounded-pill border px-4 text-[13px] font-semibold transition-colors',
                    active
                      ? 'border-terracotta bg-terracotta/10 text-terracotta'
                      : 'border-cream-border bg-cream-input text-espresso hover:border-terracotta/50',
                  ].join(' ')}
                >
                  {mealTypeLabel(type)}
                </button>
              )
            })}
          </div>
        </div>

        <div>
          <p className="mb-2 text-[11px] font-bold uppercase tracking-eyebrow text-label">
            {mode === 'recipe' ? 'Porcje' : 'Gramatura'}
          </p>
          {mode === 'recipe' ? (
            <Stepper
              label="Liczba porcji"
              value={portions}
              onChange={setPortions}
              step={RECIPE_PORTION_STEP}
              min={RECIPE_PORTION_STEP}
              max={40}
            />
          ) : (
            <Stepper
              label="Gramatura w gramach"
              value={grams}
              onChange={setGrams}
              step={PRODUCT_GRAMS_STEP}
              min={PRODUCT_GRAMS_STEP}
              max={5000}
              unit="g"
            />
          )}
        </div>

        {isError && (
          <p role="alert" className="text-[13px] font-semibold text-danger">
            Nie udało się zapisać posiłku. Twoje wybory zostały zachowane — spróbuj ponownie.
          </p>
        )}
      </div>
    </Dialog>
  )
}

interface SourceOption {
  id: string
  name: string
  kcal: number
  suffix: string
}

interface SourceSearchProps {
  kind: EntryMode
  search: string
  onSearch: (value: string) => void
  isPending: boolean
  isError: boolean
  options: SourceOption[]
  selectedId: string
  onSelect: (id: string) => void
  onCreate: () => void
}

/** The recipe/product search box and its selectable result list inside the add dialog. */
function SourceSearch({
  kind,
  search,
  onSearch,
  isPending,
  isError,
  options,
  selectedId,
  onSelect,
  onCreate,
}: SourceSearchProps) {
  const noun = kind === 'recipe' ? 'przepisu' : 'produktu'
  const createLabel = kind === 'recipe' ? 'Utwórz nowy przepis' : 'Utwórz nowy produkt'

  return (
    <div>
      <input
        type="search"
        value={search}
        onChange={(event) => onSearch(event.currentTarget.value)}
        placeholder={`Szukaj ${noun} po nazwie…`}
        aria-label={`Szukaj ${noun} po nazwie`}
        className="mb-2.5 w-full rounded-pill border-[1.5px] border-cream-border bg-cream-input px-4 py-3 text-sm text-espresso outline-none placeholder:text-mocha/70 focus:border-terracotta"
      />

      <ul aria-label={kind === 'recipe' ? 'Wyniki przepisów' : 'Wyniki produktów'} className="flex max-h-56 flex-col gap-1.5 overflow-y-auto">
        {isPending && <li className="px-3 py-2.5 text-sm text-mocha">Szukamy…</li>}
        {isError && (
          <li role="alert" className="px-3 py-2.5 text-sm text-danger">
            Nie udało się wyszukać.
          </li>
        )}
        {!isPending && !isError && options.length === 0 && (
          <li className="px-3 py-2.5 text-sm text-mocha">Brak pasujących wyników.</li>
        )}
        {options.map((option) => {
          const selected = option.id === selectedId
          return (
            <li key={option.id}>
              <button
                type="button"
                aria-pressed={selected}
                onClick={() => onSelect(option.id)}
                className={[
                  'flex min-h-[56px] w-full items-center gap-3 rounded-field border px-3.5 py-2.5 text-left transition-colors',
                  selected
                    ? 'border-terracotta bg-terracotta/10'
                    : 'border-cream-border bg-cream-input hover:border-terracotta/50',
                ].join(' ')}
              >
                <span className="min-w-0 flex-1">
                  <span className="block break-words text-sm font-semibold text-espresso">
                    {option.name}
                  </span>
                  <span className="mt-0.5 block text-[12.5px] tabular-nums text-mocha">
                    {formatKcal(option.kcal)} kcal {option.suffix}
                  </span>
                </span>
                {selected && (
                  <span aria-hidden="true" className="flex-none font-bold text-terracotta">
                    ✓
                  </span>
                )}
              </button>
            </li>
          )
        })}
      </ul>

      <button
        type="button"
        onClick={onCreate}
        className="mt-2 text-[13px] font-bold text-terracotta underline underline-offset-[3px] hover:text-terracotta-hover"
      >
        {createLabel}
      </button>
    </div>
  )
}

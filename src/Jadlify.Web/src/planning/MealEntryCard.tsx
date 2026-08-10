import { Link } from 'react-router-dom'
import { Stepper } from '../ui/Stepper'
import { formatDecimal, formatGrams, formatKcal, mealTypeLabel } from '../ui/formatters'
import { mealTypes, type MacroSummary, type MealPlanEntry, type MealType } from './types'

/** Recipe portions step in half-portions; product grams step in 10 g — never inferred from one field. */
export const RECIPE_PORTION_STEP = 0.5
export const PRODUCT_GRAMS_STEP = 10

interface MealEntryCardProps {
  entry: MealPlanEntry
  macros: MacroSummary
  /** Disables the inline controls while a mutation for this day is in flight. */
  busy: boolean
  /** New quantity chosen with the stepper — portions for a recipe entry, grams for a product entry. */
  onQuantityChange: (value: number) => void
  onMealTypeChange: (mealType: MealType) => void
  /** Omitted where rescheduling is out of scope (the dashboard), which hides the action. */
  onMove?: () => void
  /** Omitted where copying is out of scope (the dashboard), which hides the action. */
  onCopy?: () => void
  onDelete: () => void
}

/**
 * One planned meal inside its meal-type group: the source name, its quantity, the
 * per-entry macro line (words and numbers, never colour alone), a stepper for the
 * quantity, an inline meal-type switch, and the move / copy / delete actions. A
 * recipe entry links to its live recipe; a product entry shows its snapshot name.
 *
 * Move and copy are optional so the same card serves the planner (full batch
 * operations) and the day dashboard (edit the day in front of you), rather than
 * the dashboard growing a parallel near-copy that could drift.
 */
export function MealEntryCard({
  entry,
  macros,
  busy,
  onQuantityChange,
  onMealTypeChange,
  onMove,
  onCopy,
  onDelete,
}: MealEntryCardProps) {
  const isRecipe = entry.source === 'Recipe'
  const name = (isRecipe ? entry.recipeName : entry.productName) ?? 'Bez nazwy'
  const quantity = (isRecipe ? entry.portions : entry.grams) ?? 0
  const quantityLabel = isRecipe
    ? `${formatDecimal(quantity)} × porcja`
    : formatGrams(quantity)

  return (
    <div className="border-t border-dotted border-cream-line pt-3">
      <div className="flex flex-wrap items-start gap-x-3 gap-y-2">
        <div className="min-w-0 flex-1">
          {isRecipe && entry.recipeId ? (
            <Link
              to={`/recipes/${entry.recipeId}`}
              className="break-words font-semibold text-espresso hover:text-terracotta hover:underline hover:underline-offset-[3px]"
            >
              {name}
            </Link>
          ) : (
            <span className="break-words font-semibold text-espresso">{name}</span>
          )}
          <p className="mt-0.5 text-[12.5px] tabular-nums text-mocha">{quantityLabel}</p>
        </div>

        <div className="flex flex-none items-baseline gap-2 text-[12px] font-bold tabular-nums">
          <span className="text-macro-protein">{formatDecimal(macros.protein)} B</span>
          <span className="text-macro-carbs">{formatDecimal(macros.carbohydrates)} W</span>
          <span className="text-macro-fat">{formatDecimal(macros.fat)} T</span>
        </div>

        <span className="flex-none min-w-[52px] text-right font-serif text-lg tabular-nums text-espresso">
          {formatKcal(macros.calories)}
        </span>
      </div>

      <div className="mt-2.5 flex flex-wrap items-center gap-2">
        {isRecipe ? (
          <Stepper
            label={`Liczba porcji: ${name}`}
            value={quantity}
            onChange={onQuantityChange}
            step={RECIPE_PORTION_STEP}
            min={RECIPE_PORTION_STEP}
            max={40}
            disabled={busy}
          />
        ) : (
          <Stepper
            label={`Gramatura: ${name}`}
            value={quantity}
            onChange={onQuantityChange}
            step={PRODUCT_GRAMS_STEP}
            min={PRODUCT_GRAMS_STEP}
            max={5000}
            unit="g"
            formatValue={(value) => formatDecimal(value)}
            disabled={busy}
          />
        )}

        <label className="sr-only" htmlFor={`meal-type-${entry.id}`}>
          Typ posiłku: {name}
        </label>
        <select
          id={`meal-type-${entry.id}`}
          value={entry.mealType}
          disabled={busy}
          onChange={(event) => onMealTypeChange(event.currentTarget.value as MealType)}
          className="min-h-[36px] rounded-pill border border-cream-border bg-cream-panel px-3 text-[12.5px] font-semibold text-espresso disabled:opacity-60"
        >
          {mealTypes.map((type) => (
            <option key={type} value={type}>
              {mealTypeLabel(type)}
            </option>
          ))}
        </select>
      </div>

      <div className="mt-1.5 flex flex-wrap gap-1">
        {onMove && (
          <button
            type="button"
            onClick={onMove}
            disabled={busy}
            className="rounded-panel px-2 py-1.5 text-[12.5px] font-semibold text-mocha transition-colors hover:bg-cream-hover hover:text-terracotta disabled:opacity-60"
          >
            Przenieś
          </button>
        )}
        {onCopy && (
          <button
            type="button"
            onClick={onCopy}
            disabled={busy}
            className="rounded-panel px-2 py-1.5 text-[12.5px] font-semibold text-mocha transition-colors hover:bg-cream-hover hover:text-terracotta disabled:opacity-60"
          >
            Kopiuj do innych dni
          </button>
        )}
        <button
          type="button"
          onClick={onDelete}
          disabled={busy}
          className="rounded-panel px-2 py-1.5 text-[12.5px] font-semibold text-mocha transition-colors hover:bg-danger/10 hover:text-danger disabled:opacity-60"
        >
          Usuń posiłek
        </button>
      </div>
    </div>
  )
}

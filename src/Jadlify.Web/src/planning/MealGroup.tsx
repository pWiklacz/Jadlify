import { formatKcal, mealTypeLabel } from '../ui/formatters'
import { MealEntryCard } from './MealEntryCard'
import type { MacroSummary, MealPlanEntry, MealType } from './types'

/** One entry paired with its resolved per-entry macro. */
export interface EntryWithMacros {
  entry: MealPlanEntry
  macros: MacroSummary
}

interface MealGroupProps {
  mealType: MealType
  items: EntryWithMacros[]
  /** True while any planner mutation is in flight; disables the group's controls. */
  busy: boolean
  onQuantityChange: (entry: MealPlanEntry, value: number) => void
  onMealTypeChange: (entry: MealPlanEntry, mealType: MealType) => void
  /** Omitted on surfaces without batch rescheduling (the dashboard); hides the action. */
  onMove?: (entry: MealPlanEntry) => void
  /** Omitted on surfaces without batch copying (the dashboard); hides the action. */
  onCopy?: (entry: MealPlanEntry) => void
  onDelete: (entry: MealPlanEntry) => void
  onAdd: (mealType: MealType) => void
}

/**
 * One meal-type section of the day panel (Śniadanie / Obiad / …): a dotted-leader
 * header with the group's kcal total, the entries it holds, and a dashed
 * "add another to this meal" affordance that pre-selects the meal type.
 */
export function MealGroup({
  mealType,
  items,
  busy,
  onQuantityChange,
  onMealTypeChange,
  onMove,
  onCopy,
  onDelete,
  onAdd,
}: MealGroupProps) {
  const kcal = items.reduce((sum, item) => sum + item.macros.calories, 0)
  const label = mealTypeLabel(mealType)

  return (
    <section
      aria-label={label}
      className="rounded-panel border border-cream-border bg-cream-panel px-4 py-3.5 design:px-[18px]"
    >
      <div className="flex items-baseline gap-2.5">
        <h3 className="font-serif text-xl font-normal text-espresso">{label}</h3>
        <span aria-hidden="true" className="flex-1 border-b-2 border-dotted border-cream-line" />
        <span className="font-serif text-lg tabular-nums text-espresso">{formatKcal(kcal)}</span>
        <span className="text-[10.5px] font-semibold uppercase tracking-eyebrow text-mocha">
          kcal
        </span>
      </div>

      <div className="flex flex-col">
        {items.map((item) => (
          <MealEntryCard
            key={item.entry.id}
            entry={item.entry}
            macros={item.macros}
            busy={busy}
            onQuantityChange={(value) => onQuantityChange(item.entry, value)}
            onMealTypeChange={(type) => onMealTypeChange(item.entry, type)}
            onMove={onMove ? () => onMove(item.entry) : undefined}
            onCopy={onCopy ? () => onCopy(item.entry) : undefined}
            onDelete={() => onDelete(item.entry)}
          />
        ))}
      </div>

      <button
        type="button"
        onClick={() => onAdd(mealType)}
        disabled={busy}
        className="mt-2.5 w-full rounded-field border-[1.5px] border-dashed border-cream-border px-4 py-2.5 text-center text-[12.5px] font-semibold text-mocha transition-colors hover:border-terracotta hover:bg-terracotta/5 hover:text-terracotta disabled:opacity-60"
      >
        + Dodaj do „{label}”
      </button>
    </section>
  )
}

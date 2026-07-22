import { Button } from '../ui/Button'
import { StatusPill } from '../ui/StatusPill'
import { DayBalancePanel } from './DayBalancePanel'
import { MealGroup, type EntryWithMacros } from './MealGroup'
import { formatDayLong } from './plannerFormat'
import { ZERO_MACRO } from './plannerMacros'
import { mealTypes, type MacroSummary, type MealPlanDay, type MealPlanEntry, type MealType } from './types'

interface DayPanelProps {
  day: MealPlanDay
  isToday: boolean
  busy: boolean
  onQuantityChange: (entry: MealPlanEntry, value: number) => void
  onMealTypeChange: (entry: MealPlanEntry, mealType: MealType) => void
  onMove: (entry: MealPlanEntry) => void
  onCopy: (entry: MealPlanEntry) => void
  onDelete: (entry: MealPlanEntry) => void
  /** Opens the add dialog; a meal type pre-selects that group. */
  onAdd: (mealType?: MealType) => void
  onCopyDay: () => void
}

function capitalize(value: string): string {
  return value.charAt(0).toUpperCase() + value.slice(1)
}

/**
 * The selected day's detail card: a header with the date, a "Kopiuj dzień" action
 * and the add button, then the meal-type groups beside the balance panel. An empty
 * day shows a dashed prompt to add a first meal or copy a whole day (meal prep).
 */
export function DayPanel({
  day,
  isToday,
  busy,
  onQuantityChange,
  onMealTypeChange,
  onMove,
  onCopy,
  onDelete,
  onAdd,
  onCopyDay,
}: DayPanelProps) {
  const macrosByEntry = new Map<string, MacroSummary>(
    day.entryMacros.map((item) => [item.entryId, item.macros]),
  )
  const groups = mealTypes
    .map((mealType) => ({
      mealType,
      items: day.entries
        .filter((entry) => entry.mealType === mealType)
        .map<EntryWithMacros>((entry) => ({
          entry,
          macros: macrosByEntry.get(entry.id) ?? ZERO_MACRO,
        })),
    }))
    .filter((group) => group.items.length > 0)

  const hasMeals = day.entries.length > 0

  return (
    <section
      aria-label="Szczegóły wybranego dnia"
      className="animate-rise rounded-card bg-cream p-5 text-espresso shadow-card design:p-7"
    >
      <div className="mb-4 flex flex-wrap items-center gap-x-3.5 gap-y-2.5">
        <h2 className="font-serif text-2xl font-normal text-espresso design:text-[28px]">
          {capitalize(formatDayLong(day.date))}
        </h2>
        {isToday && <StatusPill label="DZIŚ" tone="info" />}
        <span className="flex-1" />
        {hasMeals && (
          <Button variant="secondary" size="sm" onClick={onCopyDay} disabled={busy}>
            Kopiuj dzień
          </Button>
        )}
        <Button size="sm" onClick={() => onAdd()} disabled={busy}>
          + Dodaj posiłek
        </Button>
      </div>

      {hasMeals ? (
        <div className="flex flex-wrap items-start gap-6">
          <div className="flex min-w-0 flex-[2_1_440px] flex-col gap-3.5">
            {groups.map((group) => (
              <MealGroup
                key={group.mealType}
                mealType={group.mealType}
                items={group.items}
                busy={busy}
                onQuantityChange={onQuantityChange}
                onMealTypeChange={onMealTypeChange}
                onMove={onMove}
                onCopy={onCopy}
                onDelete={onDelete}
                onAdd={onAdd}
              />
            ))}
          </div>
          <aside
            aria-label="Bilans wybranego dnia"
            className="flex min-w-0 flex-[1_1_290px] flex-col gap-3.5"
          >
            <DayBalancePanel total={day.total} goal={day.goal} />
          </aside>
        </div>
      ) : (
        <div className="rounded-panel border-[1.5px] border-dashed border-cream-line px-6 py-8 text-center">
          <p className="font-serif text-xl text-espresso">Ten dzień jest jeszcze pusty</p>
          <p className="mx-auto mb-4 mt-1.5 max-w-sm text-[13.5px] leading-relaxed text-mocha">
            Dodaj pierwszy posiłek albo skopiuj cały dzień z innej daty — to najszybszy sposób na
            meal prep.
          </p>
          <Button onClick={() => onAdd()} disabled={busy}>
            Dodaj pierwszy posiłek
          </Button>
        </div>
      )}
    </section>
  )
}

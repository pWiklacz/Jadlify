import { useState } from 'react'
import { Button } from '../ui/Button'
import { Dialog } from '../ui/Dialog'
import { mealTypeLabel } from '../ui/formatters'
import { formatDayLong } from './plannerFormat'
import { mealTypes, type MealPlanEntry, type MealType, type MoveMealPlanEntryRequest } from './types'

interface MoveMealEntryDialogProps {
  entry: MealPlanEntry
  isSubmitting: boolean
  isError: boolean
  onClose: () => void
  onSubmit: (body: MoveMealPlanEntryRequest) => void
}

/**
 * Reschedules one entry: pick a new day and meal type. The entry keeps its id,
 * source and quantity, so anything referring to it still does — it just moves, and
 * only leaves the original day once the save succeeds.
 */
export function MoveMealEntryDialog({
  entry,
  isSubmitting,
  isError,
  onClose,
  onSubmit,
}: MoveMealEntryDialogProps) {
  const [targetDate, setTargetDate] = useState(entry.date)
  const [mealType, setMealType] = useState<MealType>(entry.mealType)
  const name = (entry.source === 'Recipe' ? entry.recipeName : entry.productName) ?? 'Posiłek'

  return (
    <Dialog
      open
      onClose={onClose}
      title="Przenieś posiłek"
      description={name}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={isSubmitting}>
            Anuluj
          </Button>
          <Button onClick={() => onSubmit({ date: targetDate, mealType })} isLoading={isSubmitting}>
            Przenieś posiłek
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
            onChange={(event) => setTargetDate(event.currentTarget.value || entry.date)}
            className="w-full rounded-field border-[1.5px] border-cream-border bg-cream-input px-3.5 py-3 text-base font-normal text-espresso outline-none focus:border-terracotta"
          />
        </label>

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

        <p className="rounded-panel border border-cream-border bg-cream-panel px-4 py-3 text-[13px] leading-relaxed text-mocha">
          Posiłek trafi na: <b className="text-espresso">{formatDayLong(targetDate)}</b>. Zniknie z
          pierwotnego dnia dopiero po udanym zapisie.
        </p>

        {isError && (
          <p role="alert" className="text-[13px] font-semibold text-danger">
            Nie udało się przenieść posiłku. Wpis pozostał w pierwotnym dniu — spróbuj ponownie.
          </p>
        )}
      </div>
    </Dialog>
  )
}

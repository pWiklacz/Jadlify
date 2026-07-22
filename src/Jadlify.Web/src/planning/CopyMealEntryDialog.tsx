import { useState } from 'react'
import { Button } from '../ui/Button'
import { Dialog } from '../ui/Dialog'
import { TargetDaysPicker } from './TargetDaysPicker'
import type { CopyMealPlanEntryRequest, MealPlanEntry } from './types'

interface CopyMealEntryDialogProps {
  entry: MealPlanEntry
  isSubmitting: boolean
  isError: boolean
  onClose: () => void
  onSubmit: (body: CopyMealPlanEntryRequest) => void
}

/**
 * Copies one entry onto other days of its week — the meal-prep shortcut. The copy
 * clones the source's meal type and quantity as-is; existing meals on a target day
 * are kept and the copy is added alongside them.
 */
export function CopyMealEntryDialog({
  entry,
  isSubmitting,
  isError,
  onClose,
  onSubmit,
}: CopyMealEntryDialogProps) {
  const [selected, setSelected] = useState<ReadonlySet<string>>(new Set())
  const name = (entry.source === 'Recipe' ? entry.recipeName : entry.productName) ?? 'Posiłek'

  function toggle(date: string) {
    setSelected((current) => {
      const next = new Set(current)
      if (next.has(date)) {
        next.delete(date)
      } else {
        next.add(date)
      }
      return next
    })
  }

  const targetDates = [...selected].sort()

  return (
    <Dialog
      open
      onClose={onClose}
      title="Kopiuj do innych dni"
      description={`${name} — idealne do meal prepu.`}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={isSubmitting}>
            Anuluj
          </Button>
          <Button
            onClick={() => onSubmit({ targetDates })}
            isLoading={isSubmitting}
            disabled={targetDates.length === 0}
          >
            {targetDates.length > 0 ? `Kopiuj do ${targetDates.length}` : 'Kopiuj'}
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-3">
        <p className="text-[11px] font-bold uppercase tracking-eyebrow text-label">Dni docelowe</p>
        <TargetDaysPicker
          sourceDate={entry.date}
          includeSource
          selected={selected}
          onToggle={toggle}
          label="Dni docelowe"
        />
        <p className="text-[12.5px] leading-relaxed text-mocha">
          Kopia zostanie dodana jako kolejny wpis — istniejące posiłki w wybranych dniach
          pozostaną bez zmian.
        </p>

        {isError && (
          <p role="alert" className="text-[13px] font-semibold text-danger">
            Nie udało się skopiować posiłku. Żaden dzień nie został zmieniony — spróbuj ponownie.
          </p>
        )}
      </div>
    </Dialog>
  )
}

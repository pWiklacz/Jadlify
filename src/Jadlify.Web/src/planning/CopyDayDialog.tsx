import { useState } from 'react'
import { Button } from '../ui/Button'
import { Dialog } from '../ui/Dialog'
import { formatCount } from '../ui/formatters'
import { TargetDaysPicker } from './TargetDaysPicker'
import { formatDayShort } from './plannerFormat'
import type { CopyMealPlanDayRequest, MealPlanDayCopyMode } from './types'

interface CopyDayDialogProps {
  /** The source day being copied. */
  date: string
  /** How many entries the source day holds (shown in the summary line). */
  entryCount: number
  isSubmitting: boolean
  isError: boolean
  onClose: () => void
  onSubmit: (body: CopyMealPlanDayRequest) => void
}

/**
 * Copies a whole day onto other days of the week. `Add` keeps whatever each target
 * day already holds and appends the copies; `Replace` clears each target day first
 * (irreversible), so it requires an explicit acknowledgement before it can run.
 */
export function CopyDayDialog({
  date,
  entryCount,
  isSubmitting,
  isError,
  onClose,
  onSubmit,
}: CopyDayDialogProps) {
  const [selected, setSelected] = useState<ReadonlySet<string>>(new Set())
  const [mode, setMode] = useState<MealPlanDayCopyMode>('Add')
  const [acknowledged, setAcknowledged] = useState(false)

  function toggle(target: string) {
    setSelected((current) => {
      const next = new Set(current)
      if (next.has(target)) {
        next.delete(target)
      } else {
        next.add(target)
      }
      return next
    })
  }

  const targetDates = [...selected].sort()
  const replaceBlocked = mode === 'Replace' && !acknowledged
  const canSubmit = targetDates.length > 0 && !replaceBlocked

  return (
    <Dialog
      open
      onClose={onClose}
      title="Kopiuj dzień"
      description={`Źródło: ${formatDayShort(date)} · ${formatCount(
        entryCount,
        'posiłek',
        'posiłki',
        'posiłków',
      )}`}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={isSubmitting}>
            Anuluj
          </Button>
          <Button
            onClick={() => onSubmit({ targetDates, mode })}
            isLoading={isSubmitting}
            disabled={!canSubmit}
          >
            {targetDates.length > 0 ? `Kopiuj do ${targetDates.length}` : 'Kopiuj dzień'}
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-4">
        <div className="flex flex-col gap-3">
          <p className="text-[11px] font-bold uppercase tracking-eyebrow text-label">Dni docelowe</p>
          <TargetDaysPicker
            sourceDate={date}
            includeSource={false}
            selected={selected}
            onToggle={toggle}
            label="Dni docelowe"
          />
        </div>

        <div>
          <p className="mb-2 text-[11px] font-bold uppercase tracking-eyebrow text-label">
            Istniejące posiłki w dniach docelowych
          </p>
          <div role="radiogroup" aria-label="Tryb kopiowania dnia" className="flex flex-col gap-1.5">
            <ModeOption
              active={mode === 'Add'}
              title="Zachowaj istniejące posiłki"
              description="Skopiowane wpisy zostaną dodane obok już zaplanowanych."
              tone="neutral"
              onSelect={() => setMode('Add')}
            />
            <ModeOption
              active={mode === 'Replace'}
              title="Zastąp posiłki w dniach docelowych"
              description="Istniejące wpisy w zaznaczonych dniach zostaną usunięte. Tej operacji nie można cofnąć."
              tone="danger"
              onSelect={() => setMode('Replace')}
            />
          </div>
        </div>

        {mode === 'Replace' && (
          <button
            type="button"
            role="checkbox"
            aria-checked={acknowledged}
            onClick={() => setAcknowledged((value) => !value)}
            className="flex min-h-[48px] w-full items-center gap-3 rounded-field border border-danger/40 bg-danger/5 px-3.5 py-2.5 text-left"
          >
            <span
              aria-hidden="true"
              className={[
                'flex h-5 w-5 flex-none items-center justify-center rounded-[6px] text-[12px] font-extrabold text-paper',
                acknowledged ? 'bg-terracotta-strong' : 'border border-cream-line',
              ].join(' ')}
            >
              {acknowledged ? '✓' : ''}
            </span>
            <span className="text-[13px] font-semibold text-danger-ink">
              Rozumiem — istniejące posiłki w dniach docelowych zostaną usunięte.
            </span>
          </button>
        )}

        {isError && (
          <p role="alert" className="text-[13px] font-semibold text-danger">
            Nie udało się skopiować dnia. Żaden dzień nie został zmieniony — spróbuj ponownie.
          </p>
        )}
      </div>
    </Dialog>
  )
}

interface ModeOptionProps {
  active: boolean
  title: string
  description: string
  tone: 'neutral' | 'danger'
  onSelect: () => void
}

function ModeOption({ active, title, description, tone, onSelect }: ModeOptionProps) {
  return (
    <button
      type="button"
      role="radio"
      aria-checked={active}
      onClick={onSelect}
      className={[
        'flex items-start gap-2.5 rounded-field border px-3.5 py-3 text-left transition-colors',
        active ? 'border-terracotta bg-terracotta/10' : 'border-cream-border bg-cream-input',
      ].join(' ')}
    >
      <span
        aria-hidden="true"
        className={[
          'mt-0.5 h-4 w-4 flex-none rounded-full border',
          active ? 'border-terracotta-strong bg-terracotta-strong ring-2 ring-inset ring-cream' : 'border-cream-line',
        ].join(' ')}
      />
      <span>
        <span className={`block text-[13.5px] font-bold ${tone === 'danger' ? 'text-danger' : 'text-espresso'}`}>
          {title}
        </span>
        <span className="mt-0.5 block text-[12.5px] leading-relaxed text-mocha">{description}</span>
      </span>
    </button>
  )
}

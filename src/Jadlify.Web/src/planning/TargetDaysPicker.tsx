import { weekdayIndex } from './dateRange'
import { WEEKDAY_SHORT, formatDayShort, weekDays } from './plannerFormat'

interface TargetDaysPickerProps {
  /** The day whose Monday–Sunday week supplies the candidate target dates. */
  sourceDate: string
  /** Whether the source day itself is offered as a target. */
  includeSource: boolean
  /** Currently selected target dates. */
  selected: ReadonlySet<string>
  onToggle: (date: string) => void
  /** Accessible group label, e.g. "Dni docelowe". */
  label: string
}

/**
 * A week of toggle buttons for choosing copy targets. The candidates are the
 * Monday–Sunday week of the source day, so copy and copy-day always offer sensible
 * targets regardless of the active view. Each button is an `aria-pressed` toggle,
 * fully keyboard-operable.
 */
export function TargetDaysPicker({
  sourceDate,
  includeSource,
  selected,
  onToggle,
  label,
}: TargetDaysPickerProps) {
  const candidates = weekDays(sourceDate).filter(
    (date) => includeSource || date !== sourceDate,
  )

  return (
    <div role="group" aria-label={label} className="flex flex-col gap-1.5">
      {candidates.map((date) => {
        const checked = selected.has(date)
        return (
          <button
            key={date}
            type="button"
            aria-pressed={checked}
            onClick={() => onToggle(date)}
            className={[
              'flex min-h-[48px] w-full items-center gap-3 rounded-field border px-3.5 py-2.5 text-left transition-colors',
              checked
                ? 'border-terracotta bg-terracotta/10'
                : 'border-cream-border bg-cream-input hover:border-terracotta/50',
            ].join(' ')}
          >
            <span
              aria-hidden="true"
              className={[
                'flex h-5 w-5 flex-none items-center justify-center rounded-[6px] text-[12px] font-extrabold text-paper',
                checked ? 'bg-terracotta-strong' : 'border border-cream-line bg-transparent',
              ].join(' ')}
            >
              {checked ? '✓' : ''}
            </span>
            <span className="flex-1 text-[13.5px] font-semibold text-espresso">
              {WEEKDAY_SHORT[weekdayIndex(date)]} · {formatDayShort(date)}
            </span>
          </button>
        )
      })}
    </div>
  )
}

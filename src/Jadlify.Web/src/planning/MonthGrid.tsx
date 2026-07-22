import type { KeyboardEvent } from 'react'
import { formatKcal } from '../ui/formatters'
import { WEEKDAY_SHORT, dayNumber, formatDayLong, inFocusedMonth } from './plannerFormat'
import { kcalPercent, kcalStatus, statusBarClass, statusTextClass } from './plannerMacros'
import type { MealPlanDay } from './types'

interface MonthGridProps {
  /** The 42 days of the month range, in ascending order. */
  days: MealPlanDay[]
  selectedDate: string
  todayDate: string
  /** The month currently in focus; days outside it render dimmed. */
  focusMonth: string
  onSelect: (date: string) => void
}

/**
 * The month view: a fixed 6×7 calendar built from the single range read — no
 * request per cell. Every cell carries a compact readout (kcal, goal progress,
 * textual status) and selecting one just changes the highlighted day within the
 * already-loaded range.
 */
export function MonthGrid({ days, selectedDate, todayDate, focusMonth, onSelect }: MonthGridProps) {
  return (
    <section aria-label="Kalendarz miesiąca" className="mb-5">
      <div className="mb-2 grid grid-cols-7 gap-1.5">
        {WEEKDAY_SHORT.map((label) => (
          <div
            key={label}
            className="text-center text-[10.5px] font-bold uppercase tracking-eyebrow text-parchment/45"
          >
            {label}
          </div>
        ))}
      </div>
      <div className="grid grid-cols-7 gap-1.5">
        {days.map((day) => (
          <MonthCell
            key={day.date}
            day={day}
            isSelected={day.date === selectedDate}
            isToday={day.date === todayDate}
            inFocus={inFocusedMonth(day.date, focusMonth)}
            onSelect={() => onSelect(day.date)}
          />
        ))}
      </div>
    </section>
  )
}

interface MonthCellProps {
  day: MealPlanDay
  isSelected: boolean
  isToday: boolean
  inFocus: boolean
  onSelect: () => void
}

function MonthCell({ day, isSelected, isToday, inFocus, onSelect }: MonthCellProps) {
  const hasMeals = day.entries.length > 0
  const status = kcalStatus(day.total.calories, day.goal?.calories)
  const percent = kcalPercent(day.total.calories, day.goal?.calories)

  function onKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault()
      onSelect()
    }
  }

  return (
    <div
      role="button"
      tabIndex={0}
      aria-pressed={isSelected}
      aria-label={`${formatDayLong(day.date)}${isToday ? ' (dziś)' : ''}`}
      onClick={onSelect}
      onKeyDown={onKeyDown}
      className={[
        'flex min-h-[58px] cursor-pointer flex-col gap-0.5 rounded-field px-2 py-1.5 outline-none transition-transform hover:-translate-y-px focus-visible:ring-2 focus-visible:ring-terracotta design:min-h-[132px]',
        isSelected
          ? 'bg-cream text-espresso shadow-card'
          : inFocus
            ? 'border border-parchment/12 bg-parchment/[0.06] text-parchment'
            : 'border border-parchment/[0.06] bg-parchment/[0.02] text-parchment/45',
      ].join(' ')}
    >
      <div className="flex items-center gap-1">
        <span className="font-serif text-[15px] tabular-nums design:text-lg">
          {dayNumber(day.date)}
        </span>
        <span className="flex-1" />
        {hasMeals && (
          <span
            aria-hidden="true"
            className={`h-1.5 w-1.5 rounded-full ${
              status ? statusBarClass(status.tone) : 'bg-terracotta'
            }`}
          />
        )}
      </div>
      {hasMeals && (
        <div className="hidden flex-col gap-0.5 design:flex">
          <p className={`text-[10.5px] tabular-nums ${isSelected ? 'text-mocha' : 'text-parchment/50'}`}>
            {day.entries.length}× posiłek
          </p>
          <p className="tabular-nums leading-tight">
            <span className="text-[15px] font-bold">{formatKcal(day.total.calories)}</span>
            <span className={`text-[9.5px] ${isSelected ? 'text-mocha' : 'text-parchment/50'}`}>
              {' '}
              kcal
            </span>
          </p>
          {status && (
            <>
              <div
                className={`h-1 overflow-hidden rounded-pill ${
                  isSelected ? 'bg-cream-track' : 'bg-parchment/12'
                }`}
              >
                <div
                  className={`h-full rounded-pill ${statusBarClass(status.tone)}`}
                  style={{ width: `${percent}%` }}
                />
              </div>
              <p className={`text-[9.5px] font-semibold leading-tight ${statusTextClass(status.tone)}`}>
                {status.label}
              </p>
            </>
          )}
        </div>
      )}
    </div>
  )
}

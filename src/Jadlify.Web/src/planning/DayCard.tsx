import type { KeyboardEvent } from 'react'
import { formatCount, formatKcal } from '../ui/formatters'
import { weekdayIndex } from './dateRange'
import { WEEKDAY_SHORT, dayNumber, formatDayLong } from './plannerFormat'
import {
  kcalPercent,
  kcalStatus,
  macroTripletLabel,
  statusBarClass,
  statusTextClass,
} from './plannerMacros'
import type { MealPlanDay } from './types'

interface DayCardProps {
  day: MealPlanDay
  isSelected: boolean
  isToday: boolean
  onSelect: () => void
  /** Opens the add dialog for this empty day. */
  onAddFirst: () => void
}

/**
 * A single day tile in the week overview. Shows the weekday, date, and — when the
 * day has meals — a compact readout (meal count, kcal, goal progress, textual
 * status, macro triplet). An empty day offers an inline "add first meal" button.
 * The whole tile is a keyboard-operable button that selects the day.
 */
export function DayCard({ day, isSelected, isToday, onSelect, onAddFirst }: DayCardProps) {
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
        'animate-rise flex min-h-[150px] cursor-pointer flex-col gap-2 rounded-panel p-3 outline-none transition-transform hover:-translate-y-0.5 focus-visible:ring-2 focus-visible:ring-terracotta',
        isSelected
          ? 'bg-cream text-espresso shadow-card'
          : 'border border-parchment/12 bg-parchment/[0.06] text-parchment',
      ].join(' ')}
    >
      <div className="flex items-baseline gap-1.5">
        <span
          className={`text-[10.5px] font-bold uppercase tracking-eyebrow ${
            isSelected ? 'text-mocha' : 'text-parchment/55'
          }`}
        >
          {WEEKDAY_SHORT[weekdayIndex(day.date)]}
        </span>
        <span className="font-serif text-xl tabular-nums">{dayNumber(day.date)}</span>
        <span className="flex-1" />
        {isToday && (
          <span className="rounded-pill border border-terracotta/45 bg-terracotta/15 px-2 py-0.5 text-[9px] font-bold tracking-eyebrow text-terracotta">
            DZIŚ
          </span>
        )}
      </div>

      {hasMeals ? (
        <>
          <p
            className={`text-xs tabular-nums ${isSelected ? 'text-mocha' : 'text-parchment/55'}`}
          >
            {formatCount(day.entries.length, 'posiłek', 'posiłki', 'posiłków')}
          </p>
          <p className="tabular-nums leading-tight">
            <span className="font-serif text-[22px]">{formatKcal(day.total.calories)}</span>
            <span className={`text-[11.5px] ${isSelected ? 'text-mocha' : 'text-parchment/55'}`}>
              {' '}
              kcal
            </span>
          </p>
          {status && (
            <>
              <div
                className={`h-[5px] overflow-hidden rounded-pill ${
                  isSelected ? 'bg-cream-track' : 'bg-parchment/12'
                }`}
              >
                <div
                  className={`h-full rounded-pill ${statusBarClass(status.tone)}`}
                  style={{ width: `${percent}%` }}
                />
              </div>
              <p className={`text-[11.5px] font-semibold ${statusTextClass(status.tone)}`}>
                {status.label}
              </p>
            </>
          )}
          <p
            className={`mt-auto text-[10.5px] font-bold tabular-nums ${
              isSelected ? '' : 'text-parchment/70'
            }`}
          >
            {macroTripletLabel(day.total)}
          </p>
        </>
      ) : (
        <div className="flex flex-1 flex-col justify-center gap-2">
          <p className={`text-xs ${isSelected ? 'text-mocha' : 'text-parchment/55'}`}>
            Brak posiłków
          </p>
          <button
            type="button"
            onClick={(event) => {
              event.stopPropagation()
              onAddFirst()
            }}
            className={[
              'rounded-field border-[1.5px] border-dashed px-2 py-2 text-[11.5px] font-semibold leading-tight transition-colors hover:border-terracotta hover:text-terracotta',
              isSelected ? 'border-cream-line text-mocha' : 'border-parchment/25 text-parchment/70',
            ].join(' ')}
          >
            + Dodaj pierwszy posiłek
          </button>
        </div>
      )}
    </div>
  )
}

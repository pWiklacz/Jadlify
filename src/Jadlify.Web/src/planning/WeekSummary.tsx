import { Link } from 'react-router-dom'
import { formatCount, formatKcal } from '../ui/formatters'
import type { MealPlanDay } from './types'

interface WeekSummaryProps {
  /** The seven days of the visible week. */
  days: MealPlanDay[]
}

/**
 * A one-line week roll-up above the day cards: how many meals are planned, how
 * many days are filled, and the week's total calories — plus a shortcut to build a
 * shopping list once there is anything to shop for.
 */
export function WeekSummary({ days }: WeekSummaryProps) {
  const plannedMeals = days.reduce((sum, day) => sum + day.entries.length, 0)
  const filledDays = days.filter((day) => day.entries.length > 0).length
  const totalKcal = days.reduce((sum, day) => sum + day.total.calories, 0)
  const hasMeals = plannedMeals > 0

  const chips = [
    formatCount(plannedMeals, 'zaplanowany posiłek', 'zaplanowane posiłki', 'zaplanowanych posiłków'),
    `${formatCount(filledDays, 'dzień', 'dni', 'dni')} z posiłkami`,
    `${formatKcal(totalKcal)} kcal w tygodniu`,
  ]

  return (
    <section
      aria-label="Podsumowanie tygodnia"
      className="mb-4 flex flex-wrap items-center gap-x-[18px] gap-y-2 text-[13px] text-parchment/60"
    >
      {chips.map((chip) => (
        <span key={chip} className="inline-flex items-center gap-1.5 tabular-nums">
          <span aria-hidden="true" className="h-1.5 w-1.5 rounded-full bg-terracotta" />
          {chip}
        </span>
      ))}
      <span className="flex-1" />
      {hasMeals && (
        <Link
          to="/shopping-list"
          className="inline-flex items-center gap-2 rounded-pill bg-ink-800 px-[18px] py-2.5 text-[13px] font-semibold text-cream ring-1 ring-parchment/14 transition-colors hover:bg-ink-700"
        >
          Utwórz listę zakupów
        </Link>
      )}
    </section>
  )
}

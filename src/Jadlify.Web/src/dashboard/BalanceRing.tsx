import { formatKcal } from '../ui/formatters'
import { kcalPercent, kcalStatus, statusTextClass } from '../planning/plannerMacros'

interface BalanceRingProps {
  calories: number
  goalCalories: number | null
}

const RADIUS = 52
const CIRCUMFERENCE = 2 * Math.PI * RADIUS

/**
 * The day's calorie ring. The arc is decorative (`aria-hidden`): the same figures
 * and the same ±5% verdict are rendered as text beside it, so the status never
 * depends on reading a coloured arc — the rule the whole macro surface follows.
 *
 * Without a goal the ring shows planned calories only; planning works without one.
 */
export function BalanceRing({ calories, goalCalories }: BalanceRingProps) {
  const percent = kcalPercent(calories, goalCalories)
  const status = kcalStatus(calories, goalCalories)
  const offset = CIRCUMFERENCE - (percent / 100) * CIRCUMFERENCE

  return (
    <div className="flex items-center gap-5">
      <svg
        aria-hidden="true"
        viewBox="0 0 120 120"
        className="h-[108px] w-[108px] flex-none -rotate-90"
      >
        <circle
          cx="60"
          cy="60"
          r={RADIUS}
          fill="none"
          strokeWidth="10"
          className="stroke-cream-track"
        />
        {goalCalories !== null && goalCalories > 0 && (
          <circle
            cx="60"
            cy="60"
            r={RADIUS}
            fill="none"
            strokeWidth="10"
            strokeLinecap="round"
            strokeDasharray={CIRCUMFERENCE}
            strokeDashoffset={offset}
            className="stroke-terracotta-strong transition-[stroke-dashoffset] duration-500"
          />
        )}
      </svg>

      <div className="min-w-0">
        <p className="text-[11px] font-bold uppercase tracking-eyebrow text-mocha">
          {goalCalories !== null ? 'Cel dzienny · kcal' : 'Zaplanowano · kcal'}
        </p>
        <p className="font-serif text-4xl leading-tight tabular-nums text-espresso">
          {formatKcal(calories)}
          {goalCalories !== null && (
            <span className="text-2xl text-mocha"> / {formatKcal(goalCalories)}</span>
          )}
        </p>
        {status ? (
          <p className={`text-[13px] font-semibold ${statusTextClass(status.tone)}`}>
            {status.label}
          </p>
        ) : (
          <p className="text-[13px] text-mocha">Nie ustawiono dziennych celów</p>
        )}
      </div>
    </div>
  )
}

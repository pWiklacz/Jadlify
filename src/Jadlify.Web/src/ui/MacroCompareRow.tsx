import { formatKcal, formatMacro } from './formatters'

export type MacroKind = 'protein' | 'carbs' | 'fat' | 'kcal'

interface MacroCompareRowProps {
  label: string
  value: number
  /** Target value; when null the row shows the value without a comparison. */
  goal?: number | null
  kind?: MacroKind
  /** Unit shown after the numbers; defaults to "g" ("kcal" for calories). */
  unit?: string
  /** Fraction of the goal treated as "on target" (default ±5%). */
  tolerance?: number
}

const BAR_CLASSES: Record<MacroKind, string> = {
  protein: 'bg-macro-protein',
  carbs: 'bg-macro-carbs',
  fat: 'bg-macro-fat',
  kcal: 'bg-terracotta-strong',
}

/**
 * A single macro/target comparison row: label, dotted leader, `value / goal`,
 * a progress bar, and a **textual** status ("zostało …", "cel osiągnięty",
 * "przekroczono o …"). The status is always words, never color-only, so the
 * information survives for color-blind users and screen readers (FR-013).
 */
export function MacroCompareRow({
  label,
  value,
  goal = null,
  kind = 'kcal',
  unit,
  tolerance = 0.05,
}: MacroCompareRowProps) {
  const resolvedUnit = unit ?? (kind === 'kcal' ? 'kcal' : 'g')
  const fmt = kind === 'kcal' ? formatKcal : formatMacro

  const hasGoal = goal !== null && goal > 0
  const pct = hasGoal ? Math.min((value / goal) * 100, 100) : 0

  let status: string | null = null
  if (hasGoal) {
    const delta = value - goal
    if (Math.abs(delta) <= tolerance * goal) {
      status = 'cel osiągnięty'
    } else if (delta < 0) {
      status = `zostało ${fmt(-delta)} ${resolvedUnit}`
    } else {
      status = `przekroczono o ${fmt(delta)} ${resolvedUnit}`
    }
  }

  return (
    <div className="flex flex-col gap-1.5">
      <div className="flex items-baseline gap-2">
        <span className="text-[13px] font-semibold text-espresso">{label}</span>
        <span aria-hidden="true" className="flex-1 border-b border-dotted border-cream-line" />
        <span className="text-[13px] font-semibold tabular-nums text-espresso">
          {fmt(value)}
          {hasGoal && <span className="text-mocha"> / {fmt(goal)}</span>} {resolvedUnit}
        </span>
      </div>
      <div className="h-[6px] overflow-hidden rounded-pill bg-cream-track">
        <div
          className={`h-full rounded-pill ${BAR_CLASSES[kind]}`}
          style={{ width: `${pct}%` }}
        />
      </div>
      {status && <p className="text-[12.5px] text-mocha">{status}</p>}
    </div>
  )
}

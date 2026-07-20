import { formatDecimal } from './formatters'

interface StepperProps {
  /** Accessible name for the control group (e.g. "Liczba porcji"). */
  label: string
  value: number
  onChange: (next: number) => void
  /** Increment size — the redesign uses 0.5 (portions), 1, or 10 (grams). */
  step: number
  min: number
  max: number
  /** Overrides how the current value is rendered (defaults to a PL decimal). */
  formatValue?: (value: number) => string
  /** Unit suffix rendered next to the value (e.g. "g"). */
  unit?: string
  disabled?: boolean
}

// Round away binary float drift from fractional steps (e.g. repeated +0.5).
const round = (value: number) => Math.round(value * 100) / 100

/**
 * Numeric stepper: a `−` / value / `+` pill. Steps are explicit (`step`, `min`,
 * `max`) so no unit is ever inferred, and the buttons disable at the bounds.
 * The value is exposed as an ARIA `spinbutton` so its current/allowed range is
 * announced; the buttons remain the primary keyboard/pointer affordance.
 */
export function Stepper({
  label,
  value,
  onChange,
  step,
  min,
  max,
  formatValue,
  unit,
  disabled = false,
}: StepperProps) {
  const display = formatValue ? formatValue(value) : formatDecimal(value)
  const valueText = unit ? `${display} ${unit}` : display

  const atMin = value <= min
  const atMax = value >= max

  const decrement = () => {
    if (!atMin) {
      onChange(round(Math.max(min, value - step)))
    }
  }
  const increment = () => {
    if (!atMax) {
      onChange(round(Math.min(max, value + step)))
    }
  }

  return (
    <div
      role="group"
      aria-label={label}
      className="inline-flex items-center gap-1 rounded-pill border border-cream-border bg-cream-panel p-1"
    >
      <button
        type="button"
        aria-label={`Zmniejsz: ${label}`}
        onClick={decrement}
        disabled={disabled || atMin}
        className="flex h-9 w-9 items-center justify-center rounded-full text-lg text-espresso transition-colors hover:bg-cream-hover disabled:cursor-not-allowed disabled:opacity-40"
      >
        <span aria-hidden="true">−</span>
      </button>
      <span
        role="spinbutton"
        aria-label={label}
        aria-valuenow={value}
        aria-valuemin={min}
        aria-valuemax={max}
        aria-valuetext={valueText}
        className="min-w-[3.5rem] px-1 text-center font-semibold text-espresso"
      >
        {valueText}
      </span>
      <button
        type="button"
        aria-label={`Zwiększ: ${label}`}
        onClick={increment}
        disabled={disabled || atMax}
        className="flex h-9 w-9 items-center justify-center rounded-full text-lg text-espresso transition-colors hover:bg-cream-hover disabled:cursor-not-allowed disabled:opacity-40"
      >
        <span aria-hidden="true">+</span>
      </button>
    </div>
  )
}

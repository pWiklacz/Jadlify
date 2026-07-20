import type { MealType } from '../planning/types'

/**
 * Shared Polish formatting + parsing helpers for the redesigned UI.
 *
 * Kept framework-free (pure functions) so they are trivially unit-testable and
 * reusable across every feature. Numbers render with a comma decimal separator
 * and are parsed leniently (comma or dot) to match how Polish users type.
 */

/**
 * Polish plural selection. Returns the correct grammatical form for `count`:
 * `one` for exactly 1, `few` for 2–4 (excluding the teens 12–14), and `many`
 * otherwise (including 0 and decimals).
 *
 * @example pol(1, 'produkt', 'produkty', 'produktów') // 'produkt'
 * @example pol(3, 'produkt', 'produkty', 'produktów') // 'produkty'
 * @example pol(5, 'produkt', 'produkty', 'produktów') // 'produktów'
 */
export function pol(count: number, one: string, few: string, many: string): string {
  if (!Number.isInteger(count)) {
    return many
  }
  const abs = Math.abs(count)
  if (abs === 1) {
    return one
  }
  const lastTwo = abs % 100
  const last = abs % 10
  if (last >= 2 && last <= 4 && (lastTwo < 12 || lastTwo > 14)) {
    return few
  }
  return many
}

/** `count` followed by its Polish plural form, e.g. `formatCount(2, ...) → '2 produkty'`. */
export function formatCount(
  count: number,
  one: string,
  few: string,
  many: string,
): string {
  return `${count} ${pol(count, one, few, many)}`
}

/**
 * Formats a number with the Polish locale (comma decimal, no grouping for the
 * small magnitudes we display). Trailing zeros are trimmed up to
 * `maxFractionDigits`.
 */
export function formatDecimal(value: number, maxFractionDigits = 1): string {
  return new Intl.NumberFormat('pl-PL', {
    maximumFractionDigits: maxFractionDigits,
    useGrouping: false,
  }).format(value)
}

/** Grams with a unit, e.g. `formatGrams(12.5) → '12,5 g'`. */
export function formatGrams(value: number): string {
  return `${formatDecimal(value, 1)} g`
}

/** A single macro value in grams, one decimal place, comma separator. */
export function formatMacro(value: number): string {
  return formatDecimal(value, 1)
}

/** Calories as a whole number with Polish grouping, e.g. `1 850`. */
export function formatKcal(value: number): string {
  return new Intl.NumberFormat('pl-PL', { maximumFractionDigits: 0 }).format(
    Math.round(value),
  )
}

/**
 * Parses a user-typed decimal, accepting either a comma or dot separator and
 * surrounding whitespace. Returns `null` for empty or non-numeric input so
 * callers can distinguish "not a number" from a valid `0`.
 */
export function parseDecimal(input: string): number | null {
  const trimmed = input.trim()
  if (trimmed === '') {
    return null
  }
  const normalized = trimmed.replace(/\s/g, '').replace(',', '.')
  if (!/^-?\d*\.?\d+$/.test(normalized)) {
    return null
  }
  const value = Number(normalized)
  return Number.isFinite(value) ? value : null
}

/** Polish labels for the wire meal-type enum. Wire values are never translated. */
const MEAL_TYPE_LABELS: Record<MealType, string> = {
  Breakfast: 'Śniadanie',
  Lunch: 'Obiad',
  Dinner: 'Kolacja',
  Snack: 'Przekąska',
}

/** Maps a wire meal type to its Polish display label. */
export function mealTypeLabel(type: MealType): string {
  return MEAL_TYPE_LABELS[type]
}

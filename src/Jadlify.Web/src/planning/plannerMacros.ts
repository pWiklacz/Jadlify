/**
 * Shared macro presentation for the planner: the single goal-status threshold and
 * the compact per-entry / per-day macro readouts. Keeping the ±5% rule and the
 * "zostało / cel osiągnięty / przekroczono" copy in one place means the day cards,
 * the month cells and the balance panel all judge a day the same way.
 */
import { formatKcal, formatMacro } from '../ui/formatters'
import type { MacroSummary } from './types'

/** Fraction of the goal treated as "on target"; a day within ±5% of its kcal goal is met. */
export const MACRO_STATUS_TOLERANCE = 0.05

export type MacroStatusTone = 'under' | 'on-target' | 'over'

export interface MacroStatus {
  tone: MacroStatusTone
  /** Words, never colour alone — survives for colour-blind and screen-reader users. */
  label: string
}

const TONE_TEXT: Record<MacroStatusTone, string> = {
  under: 'text-terracotta',
  'on-target': 'text-success',
  over: 'text-danger',
}

const TONE_BAR: Record<MacroStatusTone, string> = {
  under: 'bg-terracotta-strong',
  'on-target': 'bg-success',
  over: 'bg-danger',
}

export function statusTextClass(tone: MacroStatusTone): string {
  return TONE_TEXT[tone]
}

export function statusBarClass(tone: MacroStatusTone): string {
  return TONE_BAR[tone]
}

/**
 * The kcal goal status for a day, or `null` when no goal is configured. Uses the
 * single ±5% tolerance so a day is "cel osiągnięty" iff its calories land within
 * that band of the goal.
 */
export function kcalStatus(total: number, goal: number | null | undefined): MacroStatus | null {
  if (goal == null || goal <= 0) {
    return null
  }
  const delta = total - goal
  if (Math.abs(delta) <= MACRO_STATUS_TOLERANCE * goal) {
    return { tone: 'on-target', label: 'cel osiągnięty' }
  }
  if (delta < 0) {
    return { tone: 'under', label: `zostało ${formatKcal(-delta)} kcal` }
  }
  return { tone: 'over', label: `przekroczono o ${formatKcal(delta)} kcal` }
}

/** Progress toward a kcal goal as a 0–100 percentage, clamped (0 when no goal). */
export function kcalPercent(total: number, goal: number | null | undefined): number {
  if (goal == null || goal <= 0) {
    return 0
  }
  return Math.min((total / goal) * 100, 100)
}

/** The three macro figures as `12 B · 8 T · 30 W`, always numeric and textual. */
export function macroTripletLabel(macros: MacroSummary): string {
  return `${formatMacro(macros.protein)} B · ${formatMacro(macros.fat)} T · ${formatMacro(
    macros.carbohydrates,
  )} W`
}

/** A full one-line macro readout for an entry: `250 kcal · 12 B · 8 T · 30 W`. */
export function entryMacroLine(macros: MacroSummary): string {
  return `${formatKcal(macros.calories)} kcal · ${macroTripletLabel(macros)}`
}

/** Zero-macro placeholder for entries whose per-entry macro has not resolved yet. */
export const ZERO_MACRO: MacroSummary = {
  calories: 0,
  protein: 0,
  fat: 0,
  carbohydrates: 0,
}

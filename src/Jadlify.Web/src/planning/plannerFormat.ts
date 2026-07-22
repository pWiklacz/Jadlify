/**
 * Polish date labels for the planner surface. Every formatter builds its `Date`
 * from the ISO day at UTC midnight and formats in the UTC zone, so the rendered
 * day matches the stored calendar day regardless of the viewer's offset.
 */
import type { PlannerView } from './dateRange'
import { addDays, isSameMonth, rangeFor, startOfWeek } from './dateRange'

/** Monday-based short weekday labels, aligned with {@link weekdayIndex}. */
export const WEEKDAY_SHORT = ['Pon', 'Wt', 'Śr', 'Czw', 'Pt', 'Sob', 'Nd'] as const

function utcDate(iso: string): Date {
  const [y, m, d] = iso.split('-').map(Number)
  return new Date(Date.UTC(y, m - 1, d))
}

function capitalize(value: string): string {
  return value.charAt(0).toUpperCase() + value.slice(1)
}

/** The day-of-month as a bare number string (e.g. `3`). */
export function dayNumber(iso: string): string {
  return String(utcDate(iso).getUTCDate())
}

/** e.g. `poniedziałek, 3 czerwca 2026`. */
export function formatDayLong(iso: string): string {
  return new Intl.DateTimeFormat('pl-PL', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(utcDate(iso))
}

/** e.g. `3 czerwca` — the day and month without the year. */
export function formatDayShort(iso: string): string {
  return new Intl.DateTimeFormat('pl-PL', {
    day: 'numeric',
    month: 'long',
    timeZone: 'UTC',
  }).format(utcDate(iso))
}

/** e.g. `Czerwiec 2026` (capitalised). */
export function formatMonthYear(iso: string): string {
  return capitalize(
    new Intl.DateTimeFormat('pl-PL', {
      month: 'long',
      year: 'numeric',
      timeZone: 'UTC',
    }).format(utcDate(iso)),
  )
}

/** The subtitle under the page title: the day, the week span, or the month name. */
export function formatRangeLabel(view: PlannerView, date: string): string {
  if (view === 'day') {
    return capitalize(formatDayLong(date))
  }
  if (view === 'week') {
    const { from, to } = rangeFor('week', date)
    return `${formatDayShort(from)} – ${formatDayShort(to)} ${utcDate(to).getUTCFullYear()}`
  }
  return formatMonthYear(date)
}

/** The seven Monday–Sunday days of the week containing `date`, as ISO strings. */
export function weekDays(date: string): string[] {
  const from = startOfWeek(date)
  return Array.from({ length: 7 }, (_, index) => addDays(from, index))
}

/** True when `iso` belongs to the month currently in focus (for greying out spill-over cells). */
export function inFocusedMonth(iso: string, focus: string): boolean {
  return isSameMonth(iso, focus)
}

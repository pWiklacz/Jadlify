/**
 * Pure `yyyy-MM-dd` date helpers for the planner's day / week / month ranges.
 *
 * All arithmetic goes through UTC epoch milliseconds so a day never shifts under
 * daylight-saving or the viewer's local offset — a planner date is a calendar
 * day, not an instant. The Polish week starts on Monday, and the month view is
 * always a fixed 6×7 grid (42 cells) beginning on the Monday on or before the
 * first of the month, matching the mockup and the range API's 42-day cap.
 */

export type PlannerView = 'day' | 'week' | 'month'

export const PLANNER_VIEWS: readonly PlannerView[] = ['day', 'week', 'month'] as const

const MS_PER_DAY = 86_400_000
const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/

function pad(value: number): string {
  return String(value).padStart(2, '0')
}

/** Parses `yyyy-MM-dd` into its numeric year / month / day parts. */
function parts(iso: string): [number, number, number] {
  const [y, m, d] = iso.split('-').map(Number)
  return [y, m, d]
}

/** UTC epoch ms at midnight of the given ISO day. */
function toUtc(iso: string): number {
  const [y, m, d] = parts(iso)
  return Date.UTC(y, m - 1, d)
}

/** Formats a UTC epoch ms back to a `yyyy-MM-dd` day. */
function fromUtc(ms: number): string {
  const date = new Date(ms)
  return `${date.getUTCFullYear()}-${pad(date.getUTCMonth() + 1)}-${pad(date.getUTCDate())}`
}

/** True when the string is a well-formed `yyyy-MM-dd` calendar day. */
export function isIsoDate(value: string): boolean {
  if (!ISO_DATE.test(value)) {
    return false
  }
  const [y, m, d] = parts(value)
  const date = new Date(Date.UTC(y, m - 1, d))
  return (
    date.getUTCFullYear() === y && date.getUTCMonth() === m - 1 && date.getUTCDate() === d
  )
}

/** The viewer's local calendar day as `yyyy-MM-dd`. */
export function todayIso(): string {
  const now = new Date()
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`
}

/** Adds `amount` calendar days (may be negative). */
export function addDays(iso: string, amount: number): string {
  return fromUtc(toUtc(iso) + amount * MS_PER_DAY)
}

/** Whole days from `from` to `to` (negative when `to` precedes `from`). */
export function diffDays(from: string, to: string): number {
  return Math.round((toUtc(to) - toUtc(from)) / MS_PER_DAY)
}

/** Monday-based weekday index: 0 = Monday … 6 = Sunday. */
export function weekdayIndex(iso: string): number {
  const sundayBased = new Date(toUtc(iso)).getUTCDay()
  return (sundayBased + 6) % 7
}

/** The Monday of the week containing `iso`. */
export function startOfWeek(iso: string): string {
  return addDays(iso, -weekdayIndex(iso))
}

/** The first day of `iso`'s month. */
export function startOfMonth(iso: string): string {
  const [y, m] = parts(iso)
  return `${y}-${pad(m)}-01`
}

/** The Monday on or before the first of `iso`'s month (top-left cell of the month grid). */
export function startOfMonthGrid(iso: string): string {
  return startOfWeek(startOfMonth(iso))
}

/** Adds `amount` months, clamping the day to the target month's length. */
export function addMonths(iso: string, amount: number): string {
  const [y, m, d] = parts(iso)
  const base = new Date(Date.UTC(y, m - 1 + amount, 1))
  const targetYear = base.getUTCFullYear()
  const targetMonth = base.getUTCMonth()
  const lastDay = new Date(Date.UTC(targetYear, targetMonth + 1, 0)).getUTCDate()
  return `${targetYear}-${pad(targetMonth + 1)}-${pad(Math.min(d, lastDay))}`
}

/** True when both dates fall in the same calendar month. */
export function isSameMonth(a: string, b: string): boolean {
  const [ay, am] = parts(a)
  const [by, bm] = parts(b)
  return ay === by && am === bm
}

export interface PlannerRange {
  from: string
  to: string
}

/**
 * The inclusive `[from, to]` window a view needs for the selected day: the day
 * itself, its Monday–Sunday week, or its 42-cell month grid. Changing the
 * selected day *within* the same window returns the same range, so the day / week
 * / month reads never fan out into one request per cell.
 */
export function rangeFor(view: PlannerView, date: string): PlannerRange {
  if (view === 'day') {
    return { from: date, to: date }
  }
  if (view === 'week') {
    const from = startOfWeek(date)
    return { from, to: addDays(from, 6) }
  }
  const from = startOfMonthGrid(date)
  return { from, to: addDays(from, 41) }
}

/** The selected day after a prev/next step: ±1 day, ±1 week, or ±1 month per view. */
export function shiftDate(view: PlannerView, date: string, direction: -1 | 1): string {
  if (view === 'day') {
    return addDays(date, direction)
  }
  if (view === 'week') {
    return addDays(date, direction * 7)
  }
  return addMonths(date, direction)
}

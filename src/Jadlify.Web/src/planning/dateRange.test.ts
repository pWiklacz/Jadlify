import { describe, expect, it } from 'vitest'
import {
  addDays,
  addMonths,
  diffDays,
  isIsoDate,
  rangeFor,
  shiftDate,
  startOfMonthGrid,
  startOfWeek,
  weekdayIndex,
} from './dateRange'

describe('dateRange', () => {
  it('adds and diffs days across a month boundary', () => {
    expect(addDays('2026-06-30', 1)).toBe('2026-07-01')
    expect(addDays('2026-03-01', -1)).toBe('2026-02-28')
    expect(diffDays('2026-06-01', '2026-06-08')).toBe(7)
  })

  it('treats Monday as weekday 0', () => {
    // 2026-06-01 is a Monday.
    expect(weekdayIndex('2026-06-01')).toBe(0)
    expect(weekdayIndex('2026-06-07')).toBe(6)
    expect(startOfWeek('2026-06-03')).toBe('2026-06-01')
  })

  it('builds a 42-day month grid beginning on a Monday', () => {
    const start = startOfMonthGrid('2026-06-15')
    expect(weekdayIndex(start)).toBe(0)
    const { from, to } = rangeFor('month', '2026-06-15')
    expect(from).toBe(start)
    expect(diffDays(from, to)).toBe(41)
  })

  it('computes the inclusive window per view', () => {
    expect(rangeFor('day', '2026-06-03')).toEqual({ from: '2026-06-03', to: '2026-06-03' })
    expect(rangeFor('week', '2026-06-03')).toEqual({ from: '2026-06-01', to: '2026-06-07' })
  })

  it('shifts by the view period and clamps month day length', () => {
    expect(shiftDate('day', '2026-06-03', 1)).toBe('2026-06-04')
    expect(shiftDate('week', '2026-06-03', -1)).toBe('2026-05-27')
    expect(shiftDate('month', '2026-06-03', 1)).toBe('2026-07-03')
    // 31 January + 1 month clamps to the last day of February.
    expect(addMonths('2026-01-31', 1)).toBe('2026-02-28')
  })

  it('validates ISO calendar days', () => {
    expect(isIsoDate('2026-06-03')).toBe(true)
    expect(isIsoDate('2026-13-01')).toBe(false)
    expect(isIsoDate('2026-02-30')).toBe(false)
    expect(isIsoDate('not-a-date')).toBe(false)
  })
})

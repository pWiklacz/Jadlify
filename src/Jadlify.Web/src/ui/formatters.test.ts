import { describe, expect, it } from 'vitest'
import {
  formatCount,
  formatDecimal,
  formatGrams,
  formatKcal,
  formatMacro,
  mealTypeLabel,
  parseDecimal,
  pol,
} from './formatters'

// pl-PL groups thousands with a (narrow) no-break space; normalize to a plain
// space so assertions don't depend on the exact ICU separator codepoint.
const normalizeSpaces = (value: string) => value.replace(/\s/g, ' ')

describe('pol', () => {
  it('returns the singular form for exactly 1', () => {
    expect(pol(1, 'produkt', 'produkty', 'produktów')).toBe('produkt')
  })

  it('returns the "few" form for 2–4', () => {
    expect(pol(2, 'produkt', 'produkty', 'produktów')).toBe('produkty')
    expect(pol(3, 'produkt', 'produkty', 'produktów')).toBe('produkty')
    expect(pol(4, 'produkt', 'produkty', 'produktów')).toBe('produkty')
  })

  it('returns the "many" form for 0, 5+ and the teens', () => {
    expect(pol(0, 'produkt', 'produkty', 'produktów')).toBe('produktów')
    expect(pol(5, 'produkt', 'produkty', 'produktów')).toBe('produktów')
    expect(pol(12, 'produkt', 'produkty', 'produktów')).toBe('produktów')
    expect(pol(13, 'produkt', 'produkty', 'produktów')).toBe('produktów')
    expect(pol(14, 'produkt', 'produkty', 'produktów')).toBe('produktów')
  })

  it('applies the last-two-digit rule for larger numbers', () => {
    expect(pol(22, 'produkt', 'produkty', 'produktów')).toBe('produkty')
    expect(pol(25, 'produkt', 'produkty', 'produktów')).toBe('produktów')
    expect(pol(112, 'produkt', 'produkty', 'produktów')).toBe('produktów')
  })

  it('treats non-integers as the "many" form', () => {
    expect(pol(1.5, 'porcja', 'porcje', 'porcji')).toBe('porcji')
  })
})

describe('formatCount', () => {
  it('prefixes the count to the correct plural form', () => {
    expect(formatCount(1, 'posiłek', 'posiłki', 'posiłków')).toBe('1 posiłek')
    expect(formatCount(3, 'posiłek', 'posiłki', 'posiłków')).toBe('3 posiłki')
    expect(formatCount(7, 'posiłek', 'posiłki', 'posiłków')).toBe('7 posiłków')
  })
})

describe('formatDecimal / formatGrams / formatMacro', () => {
  it('formats with a comma decimal and trims trailing zeros', () => {
    expect(formatDecimal(12.5)).toBe('12,5')
    expect(formatDecimal(12)).toBe('12')
    expect(formatDecimal(12.34, 1)).toBe('12,3')
  })

  it('appends a gram unit', () => {
    expect(formatGrams(120)).toBe('120 g')
    expect(formatGrams(12.5)).toBe('12,5 g')
  })

  it('formatMacro uses one decimal place', () => {
    expect(formatMacro(8.25)).toBe('8,3')
  })
})

describe('formatKcal', () => {
  it('rounds to a whole number', () => {
    expect(formatKcal(1850.4)).toBe('1850')
    expect(formatKcal(320)).toBe('320')
  })

  it('groups thousands for larger values (pl-PL groups from five digits)', () => {
    // pl-PL has minimumGroupingDigits=2, so 4-digit values stay ungrouped.
    expect(normalizeSpaces(formatKcal(12500))).toBe('12 500')
  })
})

describe('parseDecimal', () => {
  it('accepts comma and dot separators', () => {
    expect(parseDecimal('12,5')).toBe(12.5)
    expect(parseDecimal('12.5')).toBe(12.5)
  })

  it('trims surrounding and inner whitespace', () => {
    expect(parseDecimal('  120 ')).toBe(120)
    expect(parseDecimal('1 5')).toBe(15)
  })

  it('returns null for empty or non-numeric input', () => {
    expect(parseDecimal('')).toBeNull()
    expect(parseDecimal('   ')).toBeNull()
    expect(parseDecimal('abc')).toBeNull()
    expect(parseDecimal('1,2,3')).toBeNull()
  })

  it('preserves a valid zero', () => {
    expect(parseDecimal('0')).toBe(0)
  })
})

describe('mealTypeLabel', () => {
  it('maps wire meal types to Polish labels', () => {
    expect(mealTypeLabel('Breakfast')).toBe('Śniadanie')
    expect(mealTypeLabel('Lunch')).toBe('Obiad')
    expect(mealTypeLabel('Dinner')).toBe('Kolacja')
    expect(mealTypeLabel('Snack')).toBe('Przekąska')
  })
})

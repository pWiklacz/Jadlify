import { describe, expect, it } from 'vitest'
import {
  displayFieldToGrams,
  EXTENDED_FIELD_GROUPS,
  formatNutrient,
  gramsToDisplay,
  gramsToDisplayField,
  nutrientMeta,
} from './nutrients'

describe('nutrient unit conversion', () => {
  it('scales grams into the display unit', () => {
    expect(gramsToDisplay(0.5, 'mg')).toBe(500)
    expect(gramsToDisplay(10.6, 'g')).toBe(10.6)
    expect(gramsToDisplay(0.0008, 'µg')).toBeCloseTo(800, 6)
  })

  it('seeds a form field from stored grams, blank for null', () => {
    expect(gramsToDisplayField(null, 'g')).toBe('')
    expect(gramsToDisplayField(undefined, 'mg')).toBe('')
    expect(gramsToDisplayField(10.6, 'g')).toBe('10,6')
    expect(gramsToDisplayField(0.5, 'mg')).toBe('500')
  })

  it('parses a display-unit field back to grams', () => {
    expect(displayFieldToGrams('500', 'mg')).toBe(0.5)
    expect(displayFieldToGrams('10,6', 'g')).toBeCloseTo(10.6, 10)
    expect(displayFieldToGrams('', 'g')).toBeNull()
    expect(displayFieldToGrams('   ', 'mg')).toBeNull()
  })

  it('returns undefined for non-numeric input so callers can flag it invalid', () => {
    expect(displayFieldToGrams('abc', 'g')).toBeUndefined()
    expect(displayFieldToGrams('1,2,3', 'mg')).toBeUndefined()
  })

  it('round-trips a stored gram value through the display field', () => {
    const grams = 0.5
    const field = gramsToDisplayField(grams, 'mg')
    expect(displayFieldToGrams(field, 'mg')).toBe(grams)
  })

  it('formats a stored value with its unit', () => {
    expect(formatNutrient(0.5, 'mg')).toBe('500 mg')
    expect(formatNutrient(10.6, 'g')).toBe('10,6 g')
    expect(formatNutrient(0.0008, 'µg')).toBe('800 µg')
  })
})

describe('extended field metadata', () => {
  it('groups the 14 fields into the four mockup groups in order', () => {
    expect(EXTENDED_FIELD_GROUPS.map((g) => g.legend)).toEqual([
      'Tłuszcze',
      'Węglowodany',
      'Minerały',
      'Witaminy',
    ])
    const total = EXTENDED_FIELD_GROUPS.reduce((sum, g) => sum + g.fields.length, 0)
    expect(total).toBe(14)
  })

  it('assigns the human-facing unit per nutrient', () => {
    expect(nutrientMeta('sodium').unit).toBe('mg')
    expect(nutrientMeta('vitaminA').unit).toBe('µg')
    expect(nutrientMeta('sugars').unit).toBe('g')
  })
})

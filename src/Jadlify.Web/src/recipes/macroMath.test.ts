import { describe, expect, it } from 'vitest'
import { calculateRecipePreview, formatMacro } from './macroMath'

describe('macroMath', () => {
  it('calculates whole-recipe and per-serving totals from per-100g values', () => {
    const preview = calculateRecipePreview(
      [
        {
          wholeRecipeGrams: 150,
          per100Grams: { calories: 200, protein: 20, fat: 10, carbohydrates: 5 },
        },
        {
          wholeRecipeGrams: 50,
          per100Grams: { calories: 100, protein: 2, fat: 1, carbohydrates: 20 },
        },
      ],
      4,
    )

    expect(preview.total).toEqual({
      calories: 350,
      protein: 31,
      fat: 15.5,
      carbohydrates: 17.5,
    })
    expect(preview.perServing).toEqual({
      calories: 87.5,
      protein: 7.8,
      fat: 3.9,
      carbohydrates: 4.4,
    })
  })

  it('rounds display values to one decimal without trailing zero noise', () => {
    expect(formatMacro(7.00000001)).toBe('7')
    expect(formatMacro(7.24)).toBe('7.2')
  })
})

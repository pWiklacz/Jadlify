import { formatDecimal, parseDecimal } from '../ui/formatters'
import type { ExtendedNutritionKey } from './types'

/**
 * Display/entry metadata for the 14 extended per-100g nutrients.
 *
 * The wire and storage keep every nutrient in **grams** (that is how Open Food
 * Facts reports them). Humans, however, read minerals in milligrams and some
 * vitamins in micrograms, so the catalog UI converts grams to a friendlier unit
 * for both display and entry and converts back on save. All conversion lives
 * here so it is defined once and unit-tested in isolation.
 */

export type NutrientUnit = 'g' | 'mg' | 'µg'

/** Grams → unit multiplier (e.g. 1 g = 1000 mg). */
const UNIT_FACTOR: Record<NutrientUnit, number> = {
  g: 1,
  mg: 1_000,
  µg: 1_000_000,
}

export interface ExtendedFieldMeta {
  key: ExtendedNutritionKey
  /** Polish field label (without the unit). */
  label: string
  /** The unit values are displayed and entered in. */
  unit: NutrientUnit
  /** Uppercase Polish group heading the field sits under. */
  group: string
}

/** The 14 extended fields in form/display order, grouped as the mockup groups them. */
export const EXTENDED_FIELDS: readonly ExtendedFieldMeta[] = [
  { key: 'saturatedFat', label: 'Tłuszcze nasycone', unit: 'g', group: 'Tłuszcze' },
  { key: 'monounsaturatedFat', label: 'Tłuszcze jednonienasycone', unit: 'g', group: 'Tłuszcze' },
  { key: 'polyunsaturatedFat', label: 'Tłuszcze wielonienasycone', unit: 'g', group: 'Tłuszcze' },
  { key: 'transFat', label: 'Tłuszcze trans', unit: 'g', group: 'Tłuszcze' },
  { key: 'sugars', label: 'Cukry', unit: 'g', group: 'Węglowodany' },
  { key: 'fiber', label: 'Błonnik', unit: 'g', group: 'Węglowodany' },
  { key: 'salt', label: 'Sól', unit: 'g', group: 'Minerały' },
  { key: 'sodium', label: 'Sód', unit: 'mg', group: 'Minerały' },
  { key: 'potassium', label: 'Potas', unit: 'mg', group: 'Minerały' },
  { key: 'calcium', label: 'Wapń', unit: 'mg', group: 'Minerały' },
  { key: 'iron', label: 'Żelazo', unit: 'mg', group: 'Minerały' },
  { key: 'vitaminA', label: 'Witamina A', unit: 'µg', group: 'Witaminy' },
  { key: 'vitaminC', label: 'Witamina C', unit: 'mg', group: 'Witaminy' },
  { key: 'vitaminD', label: 'Witamina D', unit: 'µg', group: 'Witaminy' },
] as const

/** The extended fields collapsed into ordered `{ legend, fields }` groups. */
export const EXTENDED_FIELD_GROUPS: readonly { legend: string; fields: ExtendedFieldMeta[] }[] =
  EXTENDED_FIELDS.reduce<{ legend: string; fields: ExtendedFieldMeta[] }[]>((groups, field) => {
    const current = groups[groups.length - 1]
    if (current && current.legend === field.group) {
      current.fields.push(field)
    } else {
      groups.push({ legend: field.group, fields: [field] })
    }
    return groups
  }, [])

const META_BY_KEY = new Map(EXTENDED_FIELDS.map((f) => [f.key, f]))

/** Metadata for an extended nutrient key. */
export function nutrientMeta(key: ExtendedNutritionKey): ExtendedFieldMeta {
  const meta = META_BY_KEY.get(key)
  if (!meta) {
    throw new Error(`Unknown nutrient key: ${key}`)
  }
  return meta
}

/** Converts a stored grams value into its display unit magnitude. */
export function gramsToDisplay(grams: number, unit: NutrientUnit): number {
  return grams * UNIT_FACTOR[unit]
}

/**
 * Seeds a form input from a stored grams value: `null` → empty string, otherwise
 * the value in its display unit as a Polish comma-decimal string (trailing zeros
 * trimmed). Uses enough precision to round-trip typical label values.
 */
export function gramsToDisplayField(grams: number | null | undefined, unit: NutrientUnit): string {
  if (grams == null) {
    return ''
  }
  return formatDecimal(gramsToDisplay(grams, unit), 4)
}

/**
 * Parses a form input (in the field's display unit) back to grams. Empty → null;
 * a non-numeric value → the `NaN` sentinel `undefined` so callers can flag it as
 * invalid rather than silently coercing it.
 */
export function displayFieldToGrams(
  input: string,
  unit: NutrientUnit,
): number | null | undefined {
  const trimmed = input.trim()
  if (trimmed === '') {
    return null
  }
  const value = parseDecimal(trimmed)
  if (value == null) {
    return undefined
  }
  return value / UNIT_FACTOR[unit]
}

/** Formats a stored grams value for read-only display, e.g. `formatNutrient(0.5, 'mg') → '500 mg'`. */
export function formatNutrient(grams: number, unit: NutrientUnit): string {
  return `${formatDecimal(gramsToDisplay(grams, unit), 3)} ${unit}`
}

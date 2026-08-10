/**
 * Pure grouping + labelling helpers for the shopping surface.
 *
 * The list arrives as one flat array of product lines, each carrying the meals it
 * came from. The three views ("Zakupy", "Wg posiłków", "Wg dni") are three
 * regroupings of that same payload — no extra request, no per-view fetch — so the
 * ordering rules live here once and every view (and the dashboard summary) agrees.
 */
import { todayIso } from '../planning/dateRange'
import { formatDayShort } from '../planning/plannerFormat'
import type { MealType } from '../planning/types'
import { PRODUCT_CATEGORIES, categoryLabel, type ProductCategory } from '../products/types'
import { formatCount, formatGrams } from '../ui/formatters'
import type { ShoppingListItem, ShoppingListStatus } from './types'

/** Polish label for a list's wire status. */
export function statusLabel(status: ShoppingListStatus): string {
  return status === 'Active' ? 'AKTYWNA' : 'UKOŃCZONA'
}

/** e.g. `3 z 12 kupione` — always words plus numbers, never a bare bar. */
export function progressLabel(bought: number, total: number): string {
  return `${bought} z ${total} kupione`
}

/** Bought share as a clamped 0–100 percentage (0 for an empty list). */
export function progressPercent(bought: number, total: number): number {
  if (total <= 0) {
    return 0
  }
  return Math.min(Math.round((bought / total) * 100), 100)
}

/** The trailing half of the progress line: what is still outstanding. */
export function progressRemainingLabel(bought: number, total: number): string {
  const left = Math.max(total - bought, 0)
  if (total > 0 && left === 0) {
    return 'wszystko kupione'
  }
  return `zostało ${formatCount(left, 'produkt', 'produkty', 'produktów')}`
}

/** The sentence under the detail screen's big progress bar. */
export function progressCaption(bought: number, total: number): string {
  const left = Math.max(total - bought, 0)
  if (total > 0 && left === 0) {
    return 'Wszystkie produkty kupione'
  }
  return `Zostało ${formatCount(left, 'produkt', 'produkty', 'produktów')} do kupienia`
}

/**
 * The call to action on the index card. A list that has been started reads
 * "Kontynuuj zakupy" so the card says where the user left off, rather than
 * inviting them to begin something already in progress.
 */
export function activeListCtaLabel(bought: number, total: number): string {
  if (total > 0 && bought === total) {
    return 'Otwórz listę'
  }
  return bought > 0 ? 'Kontynuuj zakupy' : 'Zacznij zakupy'
}

/** The label shown where a category heading is needed; `null` is a valid, unlabelled state. */
export function itemCategoryLabel(category: ProductCategory | null): string {
  return category === null ? 'Bez kategorii' : categoryLabel(category)
}

/** e.g. `2 dni · 3 czerwca – 4 czerwca`, or the single day when only one is selected. */
export function sourceDaysLabel(days: readonly string[]): string {
  if (days.length === 0) {
    return 'Brak wybranych dni'
  }
  const count = formatCount(days.length, 'dzień', 'dni', 'dni')
  if (days.length === 1) {
    return `${count} · ${formatDayShort(days[0])}`
  }
  const sorted = [...days].sort()
  return `${count} · ${formatDayShort(sorted[0])} – ${formatDayShort(sorted[sorted.length - 1])}`
}

/** The pre-filled name for a new list, derived from its span so it is meaningful in history. */
export function defaultListName(days: readonly string[]): string {
  if (days.length === 0) {
    return 'Lista zakupów'
  }
  const sorted = [...days].sort()
  if (sorted.length === 1) {
    return `Zakupy na ${formatDayShort(sorted[0])}`
  }
  return `Zakupy ${formatDayShort(sorted[0])} – ${formatDayShort(sorted[sorted.length - 1])}`
}

/** A wire timestamp as a Polish calendar day, e.g. `3 czerwca 2026`. */
export function formatTimestamp(iso: string): string {
  return new Intl.DateTimeFormat('pl-PL', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  }).format(new Date(iso))
}

/**
 * A source day as a card heading, e.g. `Wtorek, 3 czerwca` — capitalised, no year
 * (a list never spans a year boundary) and marked when it is today, because "which
 * of these days is now" is the first thing read off the "Wg dni" view.
 */
export function formatSourceDayTitle(iso: string): string {
  const [year, month, day] = iso.split('-').map(Number)
  const weekday = new Intl.DateTimeFormat('pl-PL', {
    weekday: 'long',
    timeZone: 'UTC',
  }).format(new Date(Date.UTC(year, month - 1, day)))
  const capitalised = weekday.charAt(0).toUpperCase() + weekday.slice(1)
  const suffix = iso === todayIso() ? ' (dzisiaj)' : ''
  return `${capitalised}, ${formatDayShort(iso)}${suffix}`
}

export interface CategoryGroup {
  category: ProductCategory | null
  label: string
  items: ShoppingListItem[]
}

/**
 * The default "Zakupy" ordering: aisle-style category groups in the curated
 * taxonomy order, with uncategorised products last so an unset category never
 * hides a line. Items keep the API's alphabetical order inside each group.
 */
export function groupByCategory(items: readonly ShoppingListItem[]): CategoryGroup[] {
  const buckets = new Map<string, ShoppingListItem[]>()
  for (const item of items) {
    const key = item.category ?? ''
    const bucket = buckets.get(key)
    if (bucket) {
      bucket.push(item)
    } else {
      buckets.set(key, [item])
    }
  }

  const groups: CategoryGroup[] = []
  for (const { value } of PRODUCT_CATEGORIES) {
    const bucket = buckets.get(value)
    if (bucket) {
      groups.push({ category: value, label: categoryLabel(value), items: bucket })
    }
  }
  const uncategorised = buckets.get('')
  if (uncategorised) {
    groups.push({ category: null, label: itemCategoryLabel(null), items: uncategorised })
  }
  return groups
}

/** How the "Zakupy" view is ordered. `category` is the aisle grouping; the rest are flat. */
export type ItemsOrder = 'category' | 'name' | 'unbought'

/** The toolbar's ordering choices, in menu order. The first is the default when categories exist. */
export const ITEMS_ORDERS: readonly { value: ItemsOrder; label: string }[] = [
  { value: 'category', label: 'Grupuj: kategorie' },
  { value: 'name', label: 'Sortuj: alfabetycznie' },
  { value: 'unbought', label: 'Sortuj: niekupione najpierw' },
] as const

/**
 * Ordering falls back to alphabetical when nothing on the list carries a category,
 * so the aisle grouping never degrades into a single "Bez kategorii" heap.
 */
export function defaultItemsOrder(items: readonly ShoppingListItem[]): ItemsOrder {
  return items.some((item) => item.category !== null) ? 'category' : 'name'
}

/** Case-insensitive product-name search, the only filter the shopping view offers. */
export function searchItems(
  items: readonly ShoppingListItem[],
  query: string,
): ShoppingListItem[] {
  const needle = query.trim().toLocaleLowerCase('pl')
  if (needle === '') {
    return [...items]
  }
  return items.filter((item) => item.productName.toLocaleLowerCase('pl').includes(needle))
}

/** A rendered block of the "Zakupy" view. `label` is empty for the flat (sorted) orders. */
export interface ItemsGroup {
  key: string
  label: string
  items: ShoppingListItem[]
}

/**
 * Applies an ordering to already-filtered lines. `category` keeps the aisle
 * headings; `name` and `unbought` collapse to a single unlabelled block, which is
 * what makes a search result read as one list rather than scattered headings.
 */
export function orderItems(
  items: readonly ShoppingListItem[],
  order: ItemsOrder,
): ItemsGroup[] {
  if (order === 'category') {
    return groupByCategory(items).map((group) => ({
      key: group.category ?? 'uncategorised',
      label: group.label,
      items: group.items,
    }))
  }

  const sorted = [...items].sort((a, b) => a.productName.localeCompare(b.productName, 'pl'))
  if (order === 'unbought') {
    sorted.sort((a, b) => Number(a.isBought) - Number(b.isBought))
  }
  return sorted.length > 0 ? [{ key: 'all', label: '', items: sorted }] : []
}

/** One product line inside a source view: the amount this recipe or day accounts for. */
export interface SourceIngredient {
  key: string
  productName: string
  grams: number
  isBought: boolean
}

/** "Wg posiłków": one card per recipe, with every day it is cooked on folded in. */
export interface RecipeGroup {
  sourceLabel: string
  /** Distinct planned meals this recipe accounts for across the selected days. */
  mealCount: number
  /** Distinct days it appears on, ascending. */
  dates: string[]
  ingredients: SourceIngredient[]
}

/**
 * "Wg posiłków": the same recipe cooked on several days is one card, with its
 * amounts summed — which is what makes the total on a shopping line explainable
 * ("400 g of rice because this dish appears three times"), rather than repeating
 * the recipe once per day.
 */
export function groupByRecipe(items: readonly ShoppingListItem[]): RecipeGroup[] {
  const groups = new Map<
    string,
    { meals: Set<string>; dates: Set<string>; ingredients: Map<string, SourceIngredient> }
  >()

  for (const item of items) {
    for (const source of item.sources) {
      let group = groups.get(source.sourceLabel)
      if (!group) {
        group = { meals: new Set(), dates: new Set(), ingredients: new Map() }
        groups.set(source.sourceLabel, group)
      }
      group.meals.add(`${source.date}#${source.mealType}`)
      group.dates.add(source.date)

      const existing = group.ingredients.get(item.productId)
      if (existing) {
        existing.grams += source.grams
      } else {
        group.ingredients.set(item.productId, {
          key: item.id,
          productName: item.productName,
          grams: source.grams,
          isBought: item.isBought,
        })
      }
    }
  }

  return [...groups.entries()]
    .map(([sourceLabel, group]) => ({
      sourceLabel,
      mealCount: group.meals.size,
      dates: [...group.dates].sort(),
      ingredients: [...group.ingredients.values()].sort((a, b) =>
        a.productName.localeCompare(b.productName, 'pl'),
      ),
    }))
    .sort((a, b) => a.sourceLabel.localeCompare(b.sourceLabel, 'pl'))
}

/** One planned meal inside a day card: its slot, what is cooked, and the amounts it needs. */
export interface DayMealGroup {
  key: string
  mealType: MealType
  sourceLabel: string
  ingredients: SourceIngredient[]
}

export interface DayGroup {
  date: string
  meals: DayMealGroup[]
}

/**
 * "Wg dni": ascending days, each split into its planned meals in canonical slot
 * order. This is the view that answers "what did this Tuesday put on my list".
 */
export function groupByDay(items: readonly ShoppingListItem[]): DayGroup[] {
  const days = new Map<string, Map<string, DayMealGroup>>()

  for (const item of items) {
    for (const source of item.sources) {
      let meals = days.get(source.date)
      if (!meals) {
        meals = new Map()
        days.set(source.date, meals)
      }
      const key = `${source.mealType}#${source.sourceLabel}`
      let meal = meals.get(key)
      if (!meal) {
        meal = {
          key,
          mealType: source.mealType,
          sourceLabel: source.sourceLabel,
          ingredients: [],
        }
        meals.set(key, meal)
      }
      meal.ingredients.push({
        key: `${item.id}-${key}`,
        productName: item.productName,
        grams: source.grams,
        isBought: item.isBought,
      })
    }
  }

  return [...days.entries()]
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([date, meals]) => ({
      date,
      meals: [...meals.values()]
        .sort(
          (a, b) =>
            mealTypeOrder(a.mealType) - mealTypeOrder(b.mealType) ||
            a.sourceLabel.localeCompare(b.sourceLabel, 'pl'),
        )
        .map((meal) => ({
          ...meal,
          ingredients: [...meal.ingredients].sort((a, b) =>
            a.productName.localeCompare(b.productName, 'pl'),
          ),
        })),
    }))
}

/** Meals sort by slot, not alphabetically, so a day reads breakfast → snack. */
const MEAL_TYPE_ORDER: Record<MealType, number> = {
  Breakfast: 0,
  Lunch: 1,
  Dinner: 2,
  Snack: 3,
}

function mealTypeOrder(type: MealType): number {
  return MEAL_TYPE_ORDER[type]
}

/** Total grams of a source group, shown next to its heading. */
export function totalGrams(ingredients: readonly SourceIngredient[]): string {
  return formatGrams(ingredients.reduce((sum, ingredient) => sum + ingredient.grams, 0))
}

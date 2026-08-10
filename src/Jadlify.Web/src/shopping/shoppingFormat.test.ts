import { describe, expect, it } from 'vitest'
import {
  activeListCtaLabel,
  defaultItemsOrder,
  defaultListName,
  groupByCategory,
  groupByDay,
  groupByRecipe,
  itemCategoryLabel,
  orderItems,
  progressCaption,
  progressLabel,
  progressPercent,
  progressRemainingLabel,
  searchItems,
  sourceDaysLabel,
  statusLabel,
} from './shoppingFormat'
import type { ShoppingListItem, ShoppingListItemSource } from './types'

function source(overrides: Partial<ShoppingListItemSource> = {}): ShoppingListItemSource {
  return {
    date: '2026-06-03',
    mealType: 'Breakfast',
    sourceLabel: 'Owsianka',
    grams: 50,
    ...overrides,
  }
}

function item(overrides: Partial<ShoppingListItem> = {}): ShoppingListItem {
  return {
    id: 'i1',
    productId: 'p1',
    productName: 'Płatki owsiane',
    category: 'GrainsAndBread',
    grams: 100,
    isBought: false,
    sources: [source()],
    ...overrides,
  }
}

describe('statusLabel', () => {
  it('maps the wire status onto its Polish label', () => {
    expect(statusLabel('Active')).toBe('AKTYWNA')
    expect(statusLabel('Completed')).toBe('UKOŃCZONA')
  })
})

describe('progressLabel / progressPercent', () => {
  it('states progress in words and numbers', () => {
    expect(progressLabel(3, 12)).toBe('3 z 12 kupione')
  })

  it('rounds the percentage and clamps to 100', () => {
    // Oracle: 3/12 = 25%; 1/3 = 33.33… → 33; 5/4 would exceed 100 and is clamped.
    expect(progressPercent(3, 12)).toBe(25)
    expect(progressPercent(1, 3)).toBe(33)
    expect(progressPercent(5, 4)).toBe(100)
  })

  it('reports 0% for an empty list rather than dividing by zero', () => {
    expect(progressPercent(0, 0)).toBe(0)
  })
})

describe('itemCategoryLabel', () => {
  it('treats a missing category as an explicit, valid state', () => {
    expect(itemCategoryLabel(null)).toBe('Bez kategorii')
    expect(itemCategoryLabel('Dairy')).toBe('Nabiał')
  })
})

describe('sourceDaysLabel', () => {
  it('names a single day', () => {
    expect(sourceDaysLabel(['2026-06-03'])).toBe('1 dzień · 3 czerwca')
  })

  it('spans the first and last day regardless of input order', () => {
    expect(sourceDaysLabel(['2026-06-05', '2026-06-03'])).toBe('2 dni · 3 czerwca – 5 czerwca')
  })

  it('handles an empty selection', () => {
    expect(sourceDaysLabel([])).toBe('Brak wybranych dni')
  })
})

describe('defaultListName', () => {
  it('derives a meaningful name from the selected span', () => {
    expect(defaultListName(['2026-06-03'])).toBe('Zakupy na 3 czerwca')
    expect(defaultListName(['2026-06-05', '2026-06-03'])).toBe('Zakupy 3 czerwca – 5 czerwca')
  })
})

describe('groupByCategory', () => {
  it('orders groups by the curated taxonomy and puts uncategorised last', () => {
    const groups = groupByCategory([
      item({ id: 'i1', category: null, productName: 'Coś' }),
      item({ id: 'i2', category: 'Dairy', productName: 'Mleko' }),
      item({ id: 'i3', category: 'Vegetables', productName: 'Marchew' }),
    ])

    expect(groups.map((group) => group.label)).toEqual(['Warzywa', 'Nabiał', 'Bez kategorii'])
  })

  it('keeps the API item order inside a group and omits empty categories', () => {
    const groups = groupByCategory([
      item({ id: 'i1', category: 'Dairy', productName: 'Jogurt' }),
      item({ id: 'i2', category: 'Dairy', productName: 'Mleko' }),
    ])

    expect(groups).toHaveLength(1)
    expect(groups[0].items.map((entry) => entry.productName)).toEqual(['Jogurt', 'Mleko'])
  })
})

describe('progressRemainingLabel / progressCaption', () => {
  it('counts what is still outstanding', () => {
    expect(progressRemainingLabel(3, 12)).toBe('zostało 9 produktów')
    expect(progressCaption(3, 12)).toBe('Zostało 9 produktów do kupienia')
  })

  it('switches to a done phrasing once nothing is left', () => {
    expect(progressRemainingLabel(4, 4)).toBe('wszystko kupione')
    expect(progressCaption(4, 4)).toBe('Wszystkie produkty kupione')
  })

  it('treats an empty list as having nothing bought, not as finished', () => {
    expect(progressRemainingLabel(0, 0)).toBe('zostało 0 produktów')
  })
})

describe('activeListCtaLabel', () => {
  it('says where the user left off', () => {
    expect(activeListCtaLabel(0, 4)).toBe('Zacznij zakupy')
    expect(activeListCtaLabel(1, 4)).toBe('Kontynuuj zakupy')
    expect(activeListCtaLabel(4, 4)).toBe('Otwórz listę')
  })
})

describe('searchItems', () => {
  it('matches product names case-insensitively on any substring', () => {
    const items = [item({ id: 'i1', productName: 'Płatki owsiane' }), item({ id: 'i2', productName: 'Mleko' })]

    expect(searchItems(items, 'MLE').map((entry) => entry.id)).toEqual(['i2'])
    expect(searchItems(items, 'owsia').map((entry) => entry.id)).toEqual(['i1'])
  })

  it('returns everything for a blank query', () => {
    const items = [item({ id: 'i1' }), item({ id: 'i2' })]

    expect(searchItems(items, '   ')).toHaveLength(2)
  })
})

describe('defaultItemsOrder / orderItems', () => {
  it('falls back to alphabetical when nothing carries a category', () => {
    expect(defaultItemsOrder([item({ category: null })])).toBe('name')
    expect(defaultItemsOrder([item({ category: 'Dairy' })])).toBe('category')
  })

  it('collapses the sorted orders into one unlabelled block', () => {
    const groups = orderItems(
      [
        item({ id: 'i1', productName: 'Mleko', category: 'Dairy' }),
        item({ id: 'i2', productName: 'Banan', category: 'Fruits' }),
      ],
      'name',
    )

    expect(groups).toHaveLength(1)
    expect(groups[0].label).toBe('')
    expect(groups[0].items.map((entry) => entry.productName)).toEqual(['Banan', 'Mleko'])
  })

  it('floats unbought lines to the top for the shopping-first order', () => {
    const groups = orderItems(
      [
        item({ id: 'i1', productName: 'Banan', isBought: true }),
        item({ id: 'i2', productName: 'Mleko', isBought: false }),
      ],
      'unbought',
    )

    expect(groups[0].items.map((entry) => entry.productName)).toEqual(['Mleko', 'Banan'])
  })
})

describe('groupByRecipe', () => {
  it('folds the same recipe across days into one group with summed amounts', () => {
    const groups = groupByRecipe([
      item({
        id: 'i1',
        sources: [
          source({ date: '2026-06-03', grams: 30 }),
          source({ date: '2026-06-05', grams: 20 }),
        ],
      }),
    ])

    expect(groups).toHaveLength(1)
    expect(groups[0].sourceLabel).toBe('Owsianka')
    expect(groups[0].mealCount).toBe(2)
    expect(groups[0].dates).toEqual(['2026-06-03', '2026-06-05'])
    expect(groups[0].ingredients[0].grams).toBe(50)
  })

  it('keeps different recipes apart and orders them alphabetically', () => {
    const groups = groupByRecipe([
      item({ id: 'i1', sources: [source({ sourceLabel: 'Owsianka' })] }),
      item({ id: 'i2', productId: 'p2', sources: [source({ sourceLabel: 'Naleśniki' })] }),
    ])

    expect(groups.map((group) => group.sourceLabel)).toEqual(['Naleśniki', 'Owsianka'])
  })
})

describe('groupByDay', () => {
  it('orders days ascending and nests meals in canonical slot order', () => {
    const groups = groupByDay([
      item({
        id: 'i1',
        sources: [
          source({ date: '2026-06-05', mealType: 'Dinner', sourceLabel: 'Zupa' }),
          source({ date: '2026-06-03', mealType: 'Lunch', sourceLabel: 'Sałatka' }),
          source({ date: '2026-06-03', mealType: 'Breakfast', sourceLabel: 'Owsianka' }),
        ],
      }),
    ])

    expect(groups.map((group) => group.date)).toEqual(['2026-06-03', '2026-06-05'])
    expect(groups[0].meals.map((meal) => meal.mealType)).toEqual(['Breakfast', 'Lunch'])
    expect(groups[1].meals.map((meal) => meal.sourceLabel)).toEqual(['Zupa'])
  })

  it('splits two dishes in the same slot into separate meals', () => {
    const groups = groupByDay([
      item({
        id: 'i1',
        sources: [
          source({ mealType: 'Lunch', sourceLabel: 'Zupa' }),
          source({ mealType: 'Lunch', sourceLabel: 'Sałatka' }),
        ],
      }),
    ])

    expect(groups[0].meals.map((meal) => meal.sourceLabel)).toEqual(['Sałatka', 'Zupa'])
  })
})

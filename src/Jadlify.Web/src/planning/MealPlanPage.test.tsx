import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Product } from '../products/types'
import type { RecipeCatalogResponse } from '../recipes/types'
import { MealPlanPage } from './MealPlanPage'
import { addDays, rangeFor } from './dateRange'
import type { MacroSummary, MealPlanDay, MealPlanEntry, MealPlanRange } from './types'

const mockApiClient = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  del: vi.fn(),
}))

vi.mock('../api/apiClient', () => ({ apiClient: mockApiClient }))
vi.mock('../auth/useSession', () => ({
  useSession: () => ({ session: { user: { id: 'u1' } }, isLoading: false }),
}))

const MACRO: MacroSummary = { calories: 200, protein: 10, fat: 6, carbohydrates: 25 }
const SELECTED = '2026-06-03'

let entriesByDate: Record<string, MealPlanEntry[]>
let goal: MacroSummary | null
let recipeItems: RecipeCatalogResponse['items']
let productItems: Product[]
let nextEntryId: number

function entry(overrides: Partial<MealPlanEntry> = {}): MealPlanEntry {
  return {
    id: 'e1',
    date: SELECTED,
    mealType: 'Breakfast',
    source: 'Recipe',
    recipeId: 'r1',
    recipeName: 'Owsianka',
    portions: 1,
    productId: null,
    productName: null,
    category: null,
    grams: null,
    ...overrides,
  }
}

function product(overrides: Partial<Product> = {}): Product {
  return {
    id: 'p1',
    name: 'Jogurt naturalny',
    barcode: null,
    calories: 60,
    protein: 5,
    fat: 3,
    carbohydrates: 4,
    packageSizeGrams: null,
    saturatedFat: null,
    monounsaturatedFat: null,
    polyunsaturatedFat: null,
    transFat: null,
    sugars: null,
    fiber: null,
    salt: null,
    sodium: null,
    potassium: null,
    calcium: null,
    iron: null,
    vitaminA: null,
    vitaminC: null,
    vitaminD: null,
    ...overrides,
  }
}

function sumMacros(entries: MealPlanEntry[]): MacroSummary {
  return entries.reduce<MacroSummary>(
    (total) => ({
      calories: total.calories + MACRO.calories,
      protein: total.protein + MACRO.protein,
      fat: total.fat + MACRO.fat,
      carbohydrates: total.carbohydrates + MACRO.carbohydrates,
    }),
    { calories: 0, protein: 0, fat: 0, carbohydrates: 0 },
  )
}

function buildRange(from: string, to: string): MealPlanRange {
  const days: MealPlanDay[] = []
  for (let cursor = from; cursor <= to; cursor = addDays(cursor, 1)) {
    const entries = entriesByDate[cursor] ?? []
    const total = sumMacros(entries)
    const remaining = goal
      ? {
          calories: goal.calories - total.calories,
          protein: goal.protein - total.protein,
          fat: goal.fat - total.fat,
          carbohydrates: goal.carbohydrates - total.carbohydrates,
        }
      : null
    days.push({
      date: cursor,
      entries,
      entryMacros: entries.map((item) => ({ entryId: item.id, macros: MACRO })),
      total,
      goal,
      remaining,
    })
  }
  return { from, to, days }
}

function rangeCalls(): string[] {
  return mockApiClient.get.mock.calls
    .map(([path]) => path as string)
    .filter((path) => path.startsWith('/api/meal-plan/range'))
}

beforeEach(() => {
  vi.clearAllMocks()
  entriesByDate = { [SELECTED]: [entry()] }
  goal = null
  recipeItems = [
    {
      id: 'r1',
      name: 'Owsianka',
      portions: 2,
      ingredientCount: 1,
      totalMacros: { calories: 400, protein: 20, fat: 12, carbohydrates: 50 },
      perServingMacros: { calories: 200, protein: 10, fat: 6, carbohydrates: 25 },
      isInPlan: true,
    },
  ]
  productItems = [product()]
  nextEntryId = 2

  mockApiClient.get.mockImplementation(async (path: string) => {
    if (path.startsWith('/api/meal-plan/range')) {
      const url = new URL(path, 'http://test')
      return buildRange(url.searchParams.get('from')!, url.searchParams.get('to')!)
    }
    if (path.startsWith('/api/recipes/catalog')) {
      return { items: recipeItems, total: recipeItems.length, skip: 0, take: 100 }
    }
    if (path.startsWith('/api/products')) {
      return productItems
    }
    throw new Error(`unexpected GET ${path}`)
  })

  mockApiClient.post.mockImplementation(async (path: string) => {
    if (path === '/api/meal-plan') {
      return { id: `new-${nextEntryId++}` }
    }
    if (/\/api\/meal-plan\/[^/]+\/copies$/.test(path)) {
      return { entries: [{ id: 'c1', date: '2026-06-04', mealType: 'Breakfast' }] }
    }
    if (/\/api\/meal-plan\/days\/[^/]+\/copies$/.test(path)) {
      return { entries: [{ id: 'c1', date: '2026-06-04', mealType: 'Breakfast' }] }
    }
    if (/\/api\/meal-plan\/[^/]+\/move$/.test(path)) {
      return undefined
    }
    throw new Error(`unexpected POST ${path}`)
  })

  mockApiClient.put.mockResolvedValue(undefined)
  mockApiClient.del.mockResolvedValue(undefined)
})

afterEach(() => {
  vi.clearAllMocks()
})

function renderPage(initialEntry = `/meal-plan?view=week&date=${SELECTED}`) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <QueryClientProvider client={queryClient}>
        <MealPlanPage />
      </QueryClientProvider>
    </MemoryRouter>,
  )
}

function dayPanel() {
  return screen.getByRole('region', { name: 'Szczegóły wybranego dnia' })
}

describe('MealPlanPage — range + URL state', () => {
  it('reads view/date from the URL and issues exactly one range request for the week', async () => {
    renderPage()
    await screen.findByRole('region', { name: 'Przegląd tygodnia' })

    const { from, to } = rangeFor('week', SELECTED)
    expect(mockApiClient.get).toHaveBeenCalledWith(`/api/meal-plan/range?from=${from}&to=${to}`)
    // The week (and its selected day) render from one request — no per-day fanout.
    expect(rangeCalls()).toEqual([`/api/meal-plan/range?from=${from}&to=${to}`])
    const region = screen.getByRole('region', { name: 'Przegląd tygodnia' })
    const cards =
      within(region).getAllByRole('button', { pressed: false }).length +
      within(region).getAllByRole('button', { pressed: true }).length
    expect(cards).toBe(7)
  })

  it('switches to the month view with a single new range request and 42 cells', async () => {
    const user = userEvent.setup()
    renderPage()
    await screen.findByRole('region', { name: 'Przegląd tygodnia' })

    await user.click(screen.getByRole('button', { name: 'Miesiąc' }))

    const monthRange = rangeFor('month', SELECTED)
    await waitFor(() =>
      expect(mockApiClient.get).toHaveBeenCalledWith(
        `/api/meal-plan/range?from=${monthRange.from}&to=${monthRange.to}`,
      ),
    )
    const grid = await screen.findByRole('region', { name: 'Kalendarz miesiąca' })
    const cells =
      within(grid).getAllByRole('button', { pressed: false }).length +
      within(grid).getAllByRole('button', { pressed: true }).length
    expect(cells).toBe(42)
  })

  it('shows only the day panel in the day view', async () => {
    renderPage(`/meal-plan?view=day&date=${SELECTED}`)

    await screen.findByRole('region', { name: 'Szczegóły wybranego dnia' })
    expect(screen.queryByRole('region', { name: 'Przegląd tygodnia' })).not.toBeInTheDocument()
    expect(screen.queryByRole('region', { name: 'Kalendarz miesiąca' })).not.toBeInTheDocument()
  })
})

describe('MealPlanPage — entry rendering', () => {
  it('renders an entry with its textual and numeric macro line', async () => {
    renderPage()
    expect(await screen.findByRole('link', { name: 'Owsianka' })).toBeInTheDocument()
    expect(within(dayPanel()).getByText('1 × porcja')).toBeInTheDocument()
    expect(within(dayPanel()).getAllByText(/10 B/).length).toBeGreaterThan(0)
  })

  it('renders the balance panel with remaining when a goal is configured', async () => {
    goal = { calories: 500, protein: 40, fat: 20, carbohydrates: 80 }
    renderPage()

    await screen.findByRole('link', { name: 'Owsianka' })
    expect(within(dayPanel()).getByText('Bilans dnia')).toBeInTheDocument()
    expect(within(dayPanel()).getAllByText(/zostało/).length).toBeGreaterThan(0)
  })

  it('prompts for a goal when none is configured', async () => {
    renderPage()
    await screen.findByRole('link', { name: 'Owsianka' })
    expect(within(dayPanel()).getByRole('link', { name: 'Ustaw dzienne cele' })).toHaveAttribute(
      'href',
      '/goals',
    )
  })
})

describe('MealPlanPage — add entry', () => {
  it('adds a recipe entry with half-portion steps and refetches the range', async () => {
    const user = userEvent.setup()
    renderPage()
    await screen.findByRole('link', { name: 'Owsianka' })
    const before = rangeCalls().length

    await user.click(within(dayPanel()).getByRole('button', { name: '+ Dodaj posiłek' }))
    const dialog = await screen.findByRole('dialog', { name: 'Dodaj posiłek' })

    await user.click(await within(dialog).findByRole('button', { name: /Owsianka/ }))
    await user.click(within(dialog).getByRole('button', { name: 'Obiad' }))
    await user.click(within(dialog).getByRole('button', { name: 'Zmniejsz: Liczba porcji' }))
    await user.click(within(dialog).getByRole('button', { name: 'Dodaj do planu' }))

    await waitFor(() =>
      expect(mockApiClient.post).toHaveBeenCalledWith(
        '/api/meal-plan',
        { date: SELECTED, mealType: 'Lunch', recipeId: 'r1', portions: 0.5 },
      ),
    )
    // The mutation invalidates the meal-plan prefix, so the range is read again.
    await waitFor(() => expect(rangeCalls().length).toBeGreaterThan(before))
  })

  it('adds a single product entry measured in grams', async () => {
    const user = userEvent.setup()
    renderPage()
    await screen.findByRole('link', { name: 'Owsianka' })

    await user.click(within(dayPanel()).getByRole('button', { name: '+ Dodaj posiłek' }))
    const dialog = await screen.findByRole('dialog', { name: 'Dodaj posiłek' })

    await user.click(within(dialog).getByRole('button', { name: 'Produkt' }))
    await user.click(await within(dialog).findByRole('button', { name: /Jogurt naturalny/ }))
    await user.click(within(dialog).getByRole('button', { name: 'Dodaj do planu' }))

    await waitFor(() =>
      expect(mockApiClient.post).toHaveBeenCalledWith(
        '/api/meal-plan',
        { date: SELECTED, mealType: 'Breakfast', productId: 'p1', grams: 100 },
      ),
    )
  })
})

describe('MealPlanPage — operations', () => {
  it('moves an entry to a new meal type', async () => {
    const user = userEvent.setup()
    renderPage()
    await screen.findByRole('link', { name: 'Owsianka' })

    await user.click(within(dayPanel()).getByRole('button', { name: 'Przenieś' }))
    const dialog = await screen.findByRole('dialog', { name: 'Przenieś posiłek' })
    await user.click(within(dialog).getByRole('button', { name: 'Kolacja' }))
    await user.click(within(dialog).getByRole('button', { name: 'Przenieś posiłek' }))

    await waitFor(() =>
      expect(mockApiClient.post).toHaveBeenCalledWith('/api/meal-plan/e1/move', {
        date: SELECTED,
        mealType: 'Dinner',
      }),
    )
  })

  it('copies an entry to selected target days', async () => {
    const user = userEvent.setup()
    renderPage()
    await screen.findByRole('link', { name: 'Owsianka' })

    await user.click(within(dayPanel()).getByRole('button', { name: 'Kopiuj do innych dni' }))
    const dialog = await screen.findByRole('dialog', { name: 'Kopiuj do innych dni' })
    const targets = within(dialog).getByRole('group', { name: 'Dni docelowe' })
    const firstOther = within(targets)
      .getAllByRole('button')
      .find((button) => button.textContent !== null && !button.textContent.includes('3 czerwca'))!
    await user.click(firstOther)
    await user.click(within(dialog).getByRole('button', { name: /Kopiuj do 1/ }))

    await waitFor(() =>
      expect(mockApiClient.post).toHaveBeenCalledWith(
        '/api/meal-plan/e1/copies',
        expect.objectContaining({ targetDates: expect.arrayContaining([expect.any(String)]) }),
      ),
    )
  })

  it('copies a whole day in Add mode', async () => {
    const user = userEvent.setup()
    renderPage()
    await screen.findByRole('link', { name: 'Owsianka' })

    await user.click(within(dayPanel()).getByRole('button', { name: 'Kopiuj dzień' }))
    const dialog = await screen.findByRole('dialog', { name: 'Kopiuj dzień' })
    const targets = within(dialog).getByRole('group', { name: 'Dni docelowe' })
    await user.click(within(targets).getAllByRole('button')[0])
    await user.click(within(dialog).getByRole('button', { name: /Kopiuj do 1/ }))

    await waitFor(() =>
      expect(mockApiClient.post).toHaveBeenCalledWith(
        `/api/meal-plan/days/${SELECTED}/copies`,
        expect.objectContaining({ mode: 'Add' }),
      ),
    )
  })

  it('requires an acknowledgement before a Replace copy-day', async () => {
    const user = userEvent.setup()
    renderPage()
    await screen.findByRole('link', { name: 'Owsianka' })

    await user.click(within(dayPanel()).getByRole('button', { name: 'Kopiuj dzień' }))
    const dialog = await screen.findByRole('dialog', { name: 'Kopiuj dzień' })
    await user.click(within(within(dialog).getByRole('group', { name: 'Dni docelowe' })).getAllByRole('button')[0])
    await user.click(within(dialog).getByRole('radio', { name: /Zastąp posiłki/ }))

    const confirm = within(dialog).getByRole('button', { name: /Kopiuj do 1/ })
    expect(confirm).toBeDisabled()

    await user.click(within(dialog).getByRole('checkbox'))
    expect(confirm).toBeEnabled()
  })

  it('deletes an entry and offers undo that re-creates it', async () => {
    const user = userEvent.setup()
    renderPage()
    await screen.findByRole('link', { name: 'Owsianka' })

    await user.click(within(dayPanel()).getByRole('button', { name: 'Usuń posiłek' }))
    await waitFor(() => expect(mockApiClient.del).toHaveBeenCalledWith('/api/meal-plan/e1'))

    const status = await screen.findByRole('status')
    await user.click(within(status).getByRole('button', { name: 'Cofnij' }))

    await waitFor(() =>
      expect(mockApiClient.post).toHaveBeenCalledWith(
        '/api/meal-plan',
        { date: SELECTED, mealType: 'Breakfast', recipeId: 'r1', portions: 1 },
      ),
    )
  })
})

describe('MealPlanPage — handoff', () => {
  it('opens the add dialog with the handed-off recipe pre-selected on the target day', async () => {
    renderPage('/meal-plan?addRecipe=r1&date=2026-06-05&returnTo=%2Frecipes%2Fr1')

    const dialog = await screen.findByRole('dialog', { name: 'Dodaj posiłek' })
    expect(within(dialog).getByLabelText('Dzień docelowy')).toHaveValue('2026-06-05')
    // Pre-selected source enables the confirm button.
    expect(within(dialog).getByRole('button', { name: 'Dodaj do planu' })).toBeEnabled()
  })
})

import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { todayIso } from '../planning/dateRange'
import type {
  MacroSummary,
  MealPlanDay,
  MealPlanEntry,
  MealPlanRange,
} from '../planning/types'
import type { ShoppingListIndex } from '../shopping/types'
import { LandingPage } from './LandingPage'

const mockApiClient = vi.hoisted(() => ({
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  patch: vi.fn(),
  del: vi.fn(),
}))

vi.mock('../api/apiClient', () => ({ apiClient: mockApiClient }))
vi.mock('../auth/useSession', () => ({
  useSession: () => ({ session: { user: { id: 'u1' } }, isLoading: false }),
}))

const MACRO: MacroSummary = { calories: 400, protein: 20, fat: 12, carbohydrates: 50 }
const GOAL: MacroSummary = { calories: 2000, protein: 120, fat: 60, carbohydrates: 250 }

let entries: MealPlanEntry[]
let goal: MacroSummary | null
let shoppingIndex: ShoppingListIndex
let productTotal: number
let recipeTotal: number
let shoppingFails: boolean

function entry(overrides: Partial<MealPlanEntry> = {}): MealPlanEntry {
  return {
    id: 'e1',
    date: todayIso(),
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

function buildRange(from: string, to: string): MealPlanRange {
  const total = entries.reduce<MacroSummary>(
    (sum) => ({
      calories: sum.calories + MACRO.calories,
      protein: sum.protein + MACRO.protein,
      fat: sum.fat + MACRO.fat,
      carbohydrates: sum.carbohydrates + MACRO.carbohydrates,
    }),
    { calories: 0, protein: 0, fat: 0, carbohydrates: 0 },
  )
  const day: MealPlanDay = {
    date: from,
    entries,
    entryMacros: entries.map((item) => ({ entryId: item.id, macros: MACRO })),
    total,
    goal,
    remaining: goal
      ? {
          calories: goal.calories - total.calories,
          protein: goal.protein - total.protein,
          fat: goal.fat - total.fat,
          carbohydrates: goal.carbohydrates - total.carbohydrates,
        }
      : null,
  }
  return { from, to, days: [day] }
}

function rangeCalls(): string[] {
  return mockApiClient.get.mock.calls
    .map(([path]) => path as string)
    .filter((path) => path.startsWith('/api/meal-plan/range'))
}

beforeEach(() => {
  vi.clearAllMocks()
  entries = [entry()]
  goal = GOAL
  productTotal = 12
  recipeTotal = 4
  shoppingFails = false
  shoppingIndex = { active: null, history: [] }

  mockApiClient.get.mockImplementation(async (path: string) => {
    if (path.startsWith('/api/meal-plan/range')) {
      const url = new URLSearchParams(path.slice(path.indexOf('?') + 1))
      return buildRange(url.get('from') ?? '', url.get('to') ?? '')
    }
    if (path === '/api/shopping-lists') {
      if (shoppingFails) {
        throw new Error('shopping down')
      }
      return shoppingIndex
    }
    if (path.startsWith('/api/products/catalog')) {
      return { items: [], total: productTotal, skip: 0, take: 100 }
    }
    if (path.startsWith('/api/recipes/catalog')) {
      return { items: [], total: recipeTotal, skip: 0, take: 100 }
    }
    if (path.startsWith('/api/products?')) {
      return []
    }
    throw new Error(`unexpected GET ${path}`)
  })
})

afterEach(() => {
  vi.clearAllMocks()
})

function renderPage(initialEntry = '/') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <QueryClientProvider client={queryClient}>
        <LandingPage />
      </QueryClientProvider>
    </MemoryRouter>,
  )
}

describe('LandingPage dashboard', () => {
  it('reads the selected day through a single one-day range request', async () => {
    renderPage()

    await screen.findByRole('region', { name: 'Śniadanie' })

    const today = todayIso()
    expect(rangeCalls()).toEqual([
      `/api/meal-plan/range?from=${today}&to=${today}`,
    ])
  })

  it('honours the date in the URL', async () => {
    renderPage('/?date=2026-06-03')

    await waitFor(() =>
      expect(rangeCalls()).toContain('/api/meal-plan/range?from=2026-06-03&to=2026-06-03'),
    )
  })

  it('steps the day and issues exactly one new range request', async () => {
    const user = userEvent.setup()
    renderPage('/?date=2026-06-03')

    await screen.findByRole('region', { name: 'Śniadanie' })
    const before = rangeCalls().length

    await user.click(screen.getByRole('button', { name: 'Następny dzień' }))

    await waitFor(() =>
      expect(rangeCalls()).toContain('/api/meal-plan/range?from=2026-06-04&to=2026-06-04'),
    )
    expect(rangeCalls()).toHaveLength(before + 1)
  })

  it('shows the balance against the goal in words, not colour alone', async () => {
    renderPage()

    const balance = await screen.findByRole('complementary', { name: 'Bilans dnia' })
    // Oracle: one entry of 400 kcal against a 2000 kcal goal leaves 1600.
    expect(within(balance).getByText('zostało 1600 kcal')).toBeInTheDocument()
    expect(within(balance).getByText(/Białko/)).toBeInTheDocument()
  })

  it('edits the day through the shared planner mutations', async () => {
    mockApiClient.put.mockResolvedValue(undefined)
    const user = userEvent.setup()
    renderPage()

    const group = await screen.findByRole('region', { name: 'Śniadanie' })
    await user.selectOptions(within(group).getByLabelText(/Typ posiłku/), 'Dinner')

    await waitFor(() =>
      expect(mockApiClient.put).toHaveBeenCalledWith('/api/meal-plan/e1', {
        mealType: 'Dinner',
        portions: 1,
      }),
    )
  })

  it('deletes an entry from the dashboard', async () => {
    mockApiClient.del.mockResolvedValue(undefined)
    const user = userEvent.setup()
    renderPage()

    const group = await screen.findByRole('region', { name: 'Śniadanie' })
    await user.click(within(group).getByRole('button', { name: 'Usuń posiłek' }))

    await waitFor(() => expect(mockApiClient.del).toHaveBeenCalledWith('/api/meal-plan/e1'))
    expect(await screen.findByText('Usunięto posiłek.')).toBeInTheDocument()
  })

  it('omits the planner-only batch actions', async () => {
    renderPage()

    const group = await screen.findByRole('region', { name: 'Śniadanie' })
    expect(within(group).queryByRole('button', { name: 'Przenieś' })).not.toBeInTheDocument()
    expect(
      within(group).queryByRole('button', { name: 'Kopiuj do innych dni' }),
    ).not.toBeInTheDocument()
  })

  it('opens the shared add-meal dialog for the selected day', async () => {
    const user = userEvent.setup()
    renderPage()

    await user.click(await screen.findByRole('button', { name: '+ Dodaj posiłek' }))

    expect(await screen.findByRole('dialog', { name: 'Dodaj posiłek' })).toBeInTheDocument()
  })

  describe('next step', () => {
    it('points at products when the catalog is empty', async () => {
      productTotal = 0
      recipeTotal = 0
      renderPage()

      const step = await screen.findByRole('region', { name: 'Następny krok' })
      expect(within(step).getByText('Dodaj pierwszy produkt')).toBeInTheDocument()
      expect(within(step).getByRole('link', { name: 'Przejdź do produktów' })).toHaveAttribute(
        'href',
        '/products',
      )
    })

    it('points at the goals screen when no goal is configured', async () => {
      goal = null
      renderPage()

      const step = await screen.findByRole('region', { name: 'Następny krok' })
      expect(within(step).getByRole('link', { name: 'Ustaw dzienne cele' })).toHaveAttribute(
        'href',
        '/goals',
      )
    })

    it('opens the add dialog in place when the day is empty', async () => {
      entries = []
      const user = userEvent.setup()
      renderPage()

      const step = await screen.findByRole('region', { name: 'Następny krok' })
      await user.click(within(step).getByRole('button', { name: 'Dodaj posiłek' }))

      expect(await screen.findByRole('dialog', { name: 'Dodaj posiłek' })).toBeInTheDocument()
    })

    it('points at the active list once everything is set up', async () => {
      shoppingIndex = {
        active: {
          id: 'l1',
          name: 'Zakupy na weekend',
          status: 'Active',
          createdAt: '2026-06-01T10:00:00+00:00',
          completedAt: null,
          itemCount: 4,
          boughtCount: 1,
          sourceDays: ['2026-06-03'],
        },
        history: [],
      }
      renderPage()

      const step = await screen.findByRole('region', { name: 'Następny krok' })
      expect(within(step).getByRole('link', { name: 'Przejdź do listy' })).toHaveAttribute(
        'href',
        '/shopping-list/l1',
      )
    })
  })

  describe('shopping slot', () => {
    it('summarises the active list with its progress', async () => {
      shoppingIndex = {
        active: {
          id: 'l1',
          name: 'Zakupy na weekend',
          status: 'Active',
          createdAt: '2026-06-01T10:00:00+00:00',
          completedAt: null,
          itemCount: 4,
          boughtCount: 1,
          sourceDays: ['2026-06-03'],
        },
        history: [],
      }
      renderPage()

      const card = await screen.findByRole('region', { name: 'Lista zakupów' })
      expect(within(card).getByRole('progressbar')).toHaveAttribute(
        'aria-valuetext',
        '1 z 4 kupione',
      )
      expect(within(card).getByRole('link', { name: 'Pokaż listę' })).toHaveAttribute(
        'href',
        '/shopping-list/l1',
      )
    })

    it('invites creating a list when none is active', async () => {
      renderPage()

      const card = await screen.findByRole('region', { name: 'Lista zakupów' })
      expect(
        within(card).getByRole('link', { name: 'Utwórz listę zakupów' }),
      ).toHaveAttribute('href', '/shopping-list')
    })

    it('degrades on its own when the shopping read fails', async () => {
      shoppingFails = true
      renderPage()

      const card = await screen.findByRole('region', { name: 'Lista zakupów' })
      expect(within(card).getByRole('alert')).toHaveTextContent(
        'Nie udało się pobrać list zakupów',
      )
      // The day itself still renders.
      expect(screen.getByRole('region', { name: 'Śniadanie' })).toBeInTheDocument()
    })
  })

  it('shows a retry-able error when the day cannot be read', async () => {
    mockApiClient.get.mockImplementation(async (path: string) => {
      if (path.startsWith('/api/meal-plan/range')) {
        throw new Error('plan down')
      }
      return { items: [], total: 0, skip: 0, take: 100 }
    })
    renderPage()

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Nie udało się pobrać planu dnia',
    )
    expect(screen.getByRole('button', { name: 'Wczytaj ponownie' })).toBeInTheDocument()
  })
})

import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Recipe } from '../recipes/types'
import { MealPlanPage } from './MealPlanPage'
import type { MealPlanEntry } from './types'

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

const recipe: Recipe = {
  id: 'r1',
  name: 'Porridge',
  portions: 2,
  ingredients: [],
  totalMacros: { calories: 400, protein: 20, fat: 12, carbohydrates: 50 },
  perServingMacros: { calories: 200, protein: 10, fat: 6, carbohydrates: 25 },
}

let recipes: Recipe[] = []
let entriesByDate: Record<string, MealPlanEntry[]> = {}

beforeEach(() => {
  vi.clearAllMocks()
  recipes = [recipe]
  entriesByDate = {}

  mockApiClient.get.mockImplementation(async (path: string) => {
    if (path === '/api/recipes') {
      return recipes
    }

    if (path.startsWith('/api/meal-plan?date=')) {
      const date = path.slice('/api/meal-plan?date='.length)
      return entriesByDate[date] ?? []
    }

    throw new Error(`unexpected GET ${path}`)
  })

  mockApiClient.post.mockImplementation(async (path: string, body: unknown) => {
    if (path === '/api/meal-plan') {
      const request = body as {
        date: string
        recipeId: string
        mealType: MealPlanEntry['mealType']
        portions: number
      }
      const created: MealPlanEntry = {
        id: `entry-${Object.values(entriesByDate).flat().length + 1}`,
        date: request.date,
        recipeId: request.recipeId,
        recipeName: recipes.find((item) => item.id === request.recipeId)?.name ?? 'Unknown recipe',
        mealType: request.mealType,
        portions: request.portions,
      }
      entriesByDate[request.date] = [...(entriesByDate[request.date] ?? []), created]
      return { id: created.id }
    }

    throw new Error(`unexpected POST ${path}`)
  })

  mockApiClient.put.mockImplementation(async (path: string, body: unknown) => {
    const id = path.replace('/api/meal-plan/', '')
    for (const date of Object.keys(entriesByDate)) {
      entriesByDate[date] = entriesByDate[date].map((entry) =>
        entry.id === id ? { ...entry, ...(body as Pick<MealPlanEntry, 'mealType' | 'portions'>) } : entry,
      )
    }
    return undefined
  })

  mockApiClient.del.mockImplementation(async (path: string) => {
    const id = path.replace('/api/meal-plan/', '')
    for (const date of Object.keys(entriesByDate)) {
      entriesByDate[date] = entriesByDate[date].filter((entry) => entry.id !== id)
    }
    return undefined
  })
})

afterEach(() => {
  vi.clearAllMocks()
})

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <MemoryRouter>
      <QueryClientProvider client={queryClient}>
        <MealPlanPage />
      </QueryClientProvider>
    </MemoryRouter>,
  )
}

function entry(overrides: Partial<MealPlanEntry> = {}): MealPlanEntry {
  return {
    id: 'e1',
    date: '2026-06-03',
    recipeId: 'r1',
    recipeName: 'Porridge',
    mealType: 'Breakfast',
    portions: 1,
    ...overrides,
  }
}

describe('MealPlanPage', () => {
  it('fetches entries for the selected date', async () => {
    const user = userEvent.setup()
    renderPage()

    const dateInput = await screen.findByLabelText('Date')
    await user.clear(dateInput)
    await user.type(dateInput, '2026-06-02')

    await waitFor(() =>
      expect(mockApiClient.get).toHaveBeenCalledWith('/api/meal-plan?date=2026-06-02'),
    )
  })

  it('adds a meal-plan entry with the selected recipe, meal type, and portions', async () => {
    const user = userEvent.setup()
    renderPage()

    await screen.findByRole('form', { name: 'Add meal-plan entry' })
    await user.selectOptions(screen.getByLabelText('Meal type'), 'Lunch')
    await user.clear(screen.getByLabelText('Portions'))
    await user.type(screen.getByLabelText('Portions'), '2')
    await user.click(screen.getByRole('button', { name: 'Add entry' }))

    await waitFor(() =>
      expect(mockApiClient.post).toHaveBeenCalledWith(
        '/api/meal-plan',
        expect.objectContaining({
          recipeId: 'r1',
          mealType: 'Lunch',
          portions: 2,
        }),
      ),
    )
    expect(await screen.findByRole('heading', { name: 'Porridge' })).toBeInTheDocument()
  })

  it('renders duplicate entries for the same recipe and meal type', async () => {
    entriesByDate['2026-06-04'] = [
      entry({ id: 'e1', date: '2026-06-04' }),
      entry({ id: 'e2', date: '2026-06-04' }),
    ]
    const user = userEvent.setup()
    renderPage()

    const dateInput = await screen.findByLabelText('Date')
    await user.clear(dateInput)
    await user.type(dateInput, '2026-06-04')

    expect(await screen.findAllByRole('heading', { name: 'Porridge' })).toHaveLength(2)
  })

  it('edits only meal type and portions', async () => {
    const today = new Date().toISOString().slice(0, 10)
    entriesByDate[today] = [entry({ date: today })]
    const user = userEvent.setup()
    renderPage()

    await screen.findByRole('heading', { name: 'Porridge' })
    await user.click(screen.getByRole('button', { name: 'Edit' }))

    const form = screen.getByRole('form', { name: 'Edit meal-plan entry' })
    await user.selectOptions(within(form).getByLabelText('Meal type'), 'Dinner')
    await user.clear(within(form).getByLabelText('Portions'))
    await user.type(within(form).getByLabelText('Portions'), '3')
    await user.click(within(form).getByRole('button', { name: 'Save entry' }))

    await waitFor(() =>
      expect(mockApiClient.put).toHaveBeenCalledWith('/api/meal-plan/e1', {
        mealType: 'Dinner',
        portions: 3,
      }),
    )
  })

  it('deletes only the chosen entry', async () => {
    const today = new Date().toISOString().slice(0, 10)
    entriesByDate[today] = [
      entry({ id: 'e1', recipeName: 'Porridge', date: today }),
      entry({ id: 'e2', recipeName: 'Soup', mealType: 'Lunch', date: today }),
    ]
    const user = userEvent.setup()
    renderPage()

    await screen.findByRole('heading', { name: 'Porridge' })
    const soupCard = screen.getByRole('heading', { name: 'Soup' }).closest('li')
    expect(soupCard).not.toBeNull()
    await user.click(within(soupCard!).getByRole('button', { name: 'Delete' }))

    await waitFor(() => expect(mockApiClient.del).toHaveBeenCalledWith('/api/meal-plan/e2'))
  })

  it('directs the user to create a recipe when none exist', async () => {
    recipes = []
    renderPage()

    expect(await screen.findByText(/no recipes are available/i)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Create a recipe' })).toHaveAttribute('href', '/recipes')
  })

  it('does not show daily summary, delta, or shopping-list output', async () => {
    renderPage()

    await screen.findByRole('form', { name: 'Add meal-plan entry' })

    expect(screen.queryByText(/remaining/i)).not.toBeInTheDocument()
    expect(screen.queryByText(/goal delta/i)).not.toBeInTheDocument()
    expect(screen.queryByText(/shopping list/i)).not.toBeInTheDocument()
  })
})

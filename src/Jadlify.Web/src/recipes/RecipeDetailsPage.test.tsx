import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/client'
import { RecipeDetailsPage } from './RecipeDetailsPage'
import type { Recipe } from './types'

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

let recipe: Recipe | null = null

function buildRecipe(overrides: Partial<Recipe> = {}): Recipe {
  return {
    id: 'r1',
    name: 'Owsianka',
    portions: 2,
    ingredients: [
      {
        productId: 'p1',
        productName: 'Płatki owsiane',
        wholeRecipeGrams: 150,
        per100Grams: { calories: 380, protein: 13, fat: 7, carbohydrates: 67 },
      },
      {
        productId: 'p2',
        productName: 'Mleko',
        wholeRecipeGrams: 200,
        per100Grams: { calories: 60, protein: 3.4, fat: 3, carbohydrates: 5 },
      },
    ],
    totalMacros: { calories: 690, protein: 26.3, fat: 16.5, carbohydrates: 110.5 },
    perServingMacros: { calories: 345, protein: 13.2, fat: 8.3, carbohydrates: 55.3 },
    isInPlan: false,
    ...overrides,
  }
}

beforeEach(() => {
  vi.clearAllMocks()
  recipe = buildRecipe()

  mockApiClient.get.mockImplementation(async (path: string) => {
    if (path === '/api/recipes/r1') {
      if (!recipe) {
        throw new ApiError(404, 'Not found')
      }
      return recipe
    }
    if (path.startsWith('/api/products?')) {
      return []
    }
    throw new Error(`unexpected GET ${path}`)
  })
  mockApiClient.del.mockResolvedValue(undefined)
})

afterEach(() => {
  vi.clearAllMocks()
})

/** Echoes the current location so navigation assertions read the real URL. */
function LocationProbe() {
  const location = useLocation()
  return <div data-testid="location">{`${location.pathname}${location.search}`}</div>
}

function renderPage(initialEntry = '/recipes/r1') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <QueryClientProvider client={queryClient}>
        <Routes>
          <Route path="/recipes/:id" element={<RecipeDetailsPage />} />
          <Route path="*" element={<div>inna trasa</div>} />
        </Routes>
        <LocationProbe />
      </QueryClientProvider>
    </MemoryRouter>,
  )
}

describe('RecipeDetailsPage', () => {
  it('shows per-serving macros, whole-recipe totals and ingredient grams', async () => {
    renderPage()

    expect(await screen.findByRole('heading', { name: 'Owsianka' })).toBeInTheDocument()
    expect(screen.getByText('2 porcje · 2 składniki')).toBeInTheDocument()
    expect(screen.getByText('345')).toBeInTheDocument()
    expect(screen.getByText('690 kcal')).toBeInTheDocument()
    expect(screen.getByText('Płatki owsiane')).toBeInTheDocument()
    expect(screen.getByText('150 g')).toBeInTheDocument()
    expect(screen.getByText('200 g')).toBeInTheDocument()
  })

  it('warns only when the recipe is used by a planned meal', async () => {
    renderPage()
    await screen.findByRole('heading', { name: 'Owsianka' })
    expect(
      screen.queryByText(/używany w zaplanowanych posiłkach/i),
    ).not.toBeInTheDocument()

    recipe = buildRecipe({ isInPlan: true })
    renderPage()

    expect(
      await screen.findByText(/używany w zaplanowanych posiłkach/i),
    ).toBeInTheDocument()
  })

  it('hands the recipe and return path off to the planner', async () => {
    const user = userEvent.setup()
    renderPage('/recipes/r1?sort=NameAsc')

    await screen.findByRole('heading', { name: 'Owsianka' })
    await user.click(screen.getByRole('button', { name: 'Dodaj do planu' }))

    const location = await screen.findByTestId('location')
    const search = new URLSearchParams(location.textContent!.split('?')[1])
    expect(location.textContent!.startsWith('/meal-plan?')).toBe(true)
    expect(search.get('addRecipe')).toBe('r1')
    expect(search.get('returnTo')).toBe('/recipes/r1?sort=NameAsc')
  })

  it('returns to the index with the filters that brought the user here', async () => {
    const user = userEvent.setup()
    renderPage('/recipes/r1')

    await screen.findByRole('heading', { name: 'Owsianka' })
    await user.click(screen.getByRole('button', { name: 'Wszystkie przepisy' }))

    expect(await screen.findByTestId('location')).toHaveTextContent('/recipes')
  })

  it('navigates back to the index after a successful delete', async () => {
    const user = userEvent.setup()
    renderPage()

    await screen.findByRole('heading', { name: 'Owsianka' })
    await user.click(screen.getByRole('button', { name: 'Usuń przepis' }))

    const dialog = await screen.findByRole('alertdialog', { name: 'Usunąć przepis?' })
    await user.click(within(dialog).getByRole('button', { name: 'Usuń przepis' }))

    await waitFor(() => expect(mockApiClient.del).toHaveBeenCalledWith('/api/recipes/r1'))
    expect(await screen.findByTestId('location')).toHaveTextContent('/recipes')
  })

  it('renders a dedicated not-found state for a missing recipe', async () => {
    recipe = null
    renderPage()

    expect(await screen.findByText('Nie znaleziono przepisu')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Wróć do przepisów' })).toBeInTheDocument()
  })
})

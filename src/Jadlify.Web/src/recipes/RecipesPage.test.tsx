import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/client'
import type { Product } from '../products/types'
import { RecipesPage } from './RecipesPage'
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

const nullExtended = {
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
}

const oats: Product = {
  id: 'p1',
  name: 'Oats',
  barcode: '111',
  calories: 380,
  protein: 13,
  fat: 7,
  carbohydrates: 67,
  ...nullExtended,
}

const milk: Product = {
  id: 'p2',
  name: 'Milk',
  barcode: '222',
  calories: 60,
  protein: 3.4,
  fat: 3,
  carbohydrates: 5,
  ...nullExtended,
}

let recipeList: Recipe[] = []
let productSearch: Product[] = []

beforeEach(() => {
  vi.clearAllMocks()
  recipeList = []
  productSearch = [oats, milk]

  mockApiClient.get.mockImplementation(async (path: string) => {
    if (path === '/api/recipes') {
      return recipeList
    }
    if (path.startsWith('/api/products?')) {
      return productSearch
    }
    throw new Error(`unexpected GET ${path}`)
  })
  mockApiClient.post.mockImplementation(async (path: string, body: unknown) => {
    if (path === '/api/recipes') {
      recipeList = [savedRecipe('r-new', body as { name: string; portions: number })]
      return { id: 'r-new' }
    }
    if (path === '/api/products') {
      const created = { id: 'p3', ...(body as Omit<Product, 'id'>) }
      productSearch = [created, ...productSearch]
      return created
    }
    throw new Error(`unexpected POST ${path}`)
  })
  mockApiClient.put.mockResolvedValue(undefined)
  mockApiClient.del.mockResolvedValue(undefined)
})

afterEach(() => {
  vi.clearAllMocks()
})

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={queryClient}>
      <RecipesPage />
    </QueryClientProvider>,
  )
}

function savedRecipe(id: string, values: { name: string; portions: number }): Recipe {
  return {
    id,
    name: values.name,
    portions: values.portions,
    ingredients: [
      {
        productId: oats.id,
        productName: oats.name,
        wholeRecipeGrams: 100,
        per100Grams: {
          calories: oats.calories,
          protein: oats.protein,
          fat: oats.fat,
          carbohydrates: oats.carbohydrates,
        },
      },
    ],
    totalMacros: { calories: 380, protein: 13, fat: 7, carbohydrates: 67 },
    perServingMacros: { calories: 190, protein: 6.5, fat: 3.5, carbohydrates: 33.5 },
  }
}

describe('RecipesPage', () => {
  it('renders the empty state', async () => {
    renderPage()

    expect(await screen.findByText(/no recipes yet/i)).toBeInTheDocument()
  })

  it('creates a recipe with live macro preview', async () => {
    const user = userEvent.setup()
    renderPage()

    await screen.findByText(/no recipes yet/i)
    await user.click(screen.getByRole('button', { name: 'Create recipe' }))

    await user.type(screen.getByLabelText('Name'), 'Breakfast bowl')
    await user.clear(screen.getByLabelText('Portions'))
    await user.type(screen.getByLabelText('Portions'), '2')
    await user.click(await screen.findByRole('button', { name: /oats/i }))
    await user.type(screen.getByLabelText('Whole recipe grams'), '150')

    expect(screen.getByText('Whole recipe')).toBeInTheDocument()
    expect(screen.getByText('570')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Save recipe' }))

    await waitFor(() =>
      expect(mockApiClient.post).toHaveBeenCalledWith('/api/recipes', {
        name: 'Breakfast bowl',
        portions: 2,
        ingredients: [{ productId: 'p1', wholeRecipeGrams: 150 }],
      }),
    )
    expect(await screen.findByRole('heading', { name: 'Breakfast bowl' })).toBeInTheDocument()
  })

  it('keeps save disabled until a complete recipe exists', async () => {
    const user = userEvent.setup()
    renderPage()

    await screen.findByText(/no recipes yet/i)
    await user.click(screen.getByRole('button', { name: 'Create recipe' }))

    expect(screen.getByRole('button', { name: 'Save recipe' })).toBeDisabled()
  })

  it('prevents selecting the same product twice', async () => {
    const user = userEvent.setup()
    renderPage()

    await screen.findByText(/no recipes yet/i)
    await user.click(screen.getByRole('button', { name: 'Create recipe' }))
    await user.click(await screen.findByRole('button', { name: /oats/i }))
    await user.click(screen.getByRole('button', { name: 'Add row' }))

    const oatsButtons = screen.getAllByRole('button', { name: /oats/i })
    const disabledDuplicateOption = oatsButtons.find((button) => button.textContent?.includes('Used'))

    expect(disabledDuplicateOption).toBeDisabled()
  })

  it('adds a missing product without losing the recipe draft', async () => {
    const user = userEvent.setup()
    renderPage()

    await screen.findByText(/no recipes yet/i)
    await user.click(screen.getByRole('button', { name: 'Create recipe' }))
    await user.type(screen.getByLabelText('Name'), 'Post workout')
    await user.click(screen.getByRole('button', { name: 'Add missing product' }))

    const productDialog = await screen.findByRole('dialog', { name: 'Dodaj produkt' })
    await user.type(within(productDialog).getByLabelText('Nazwa produktu'), 'Protein powder')
    await user.type(within(productDialog).getByLabelText(/^Kalorie/i), '400')
    await user.type(within(productDialog).getByLabelText(/^Białko/i), '80')
    // The redesigned form requires all four macros (0 is a valid value).
    await user.type(within(productDialog).getByLabelText(/^Tłuszcz/i), '0')
    await user.type(within(productDialog).getByLabelText(/^Węglowodany/i), '0')
    await user.click(within(productDialog).getByRole('button', { name: 'Zapisz produkt' }))

    expect(await screen.findByDisplayValue('Post workout')).toBeInTheDocument()
    const selectedNotice = await screen.findByText(/selected:/i)
    expect(selectedNotice).toHaveTextContent(/protein powder/i)
  })

  it('edits a recipe and submits the replacement body', async () => {
    const user = userEvent.setup()
    recipeList = [savedRecipe('r1', { name: 'Oat bowl', portions: 2 })]
    renderPage()

    await screen.findByRole('heading', { name: 'Oat bowl' })
    await user.click(screen.getByRole('button', { name: 'Edit' }))

    const portions = screen.getByLabelText('Portions')
    await user.clear(portions)
    await user.type(portions, '4')
    await user.click(screen.getByRole('button', { name: 'Save recipe' }))

    await waitFor(() =>
      expect(mockApiClient.put).toHaveBeenCalledWith('/api/recipes/r1', {
        name: 'Oat bowl',
        portions: 4,
        ingredients: [{ productId: 'p1', wholeRecipeGrams: 100 }],
      }),
    )
  })

  it('deletes a recipe after confirmation', async () => {
    const user = userEvent.setup()
    recipeList = [savedRecipe('r1', { name: 'Oat bowl', portions: 2 })]
    renderPage()

    await screen.findByRole('heading', { name: 'Oat bowl' })
    await user.click(screen.getByRole('button', { name: 'Delete' }))
    const dialog = await screen.findByRole('alertdialog')
    await user.click(within(dialog).getByRole('button', { name: 'Delete' }))

    expect(mockApiClient.del).toHaveBeenCalledWith('/api/recipes/r1')
  })

  it('surfaces recipe-in-use conflicts when delete fails with 409', async () => {
    const user = userEvent.setup()
    recipeList = [savedRecipe('r1', { name: 'Oat bowl', portions: 2 })]
    mockApiClient.del.mockRejectedValue(new ApiError(409, 'Recipe in use'))
    renderPage()

    await screen.findByRole('heading', { name: 'Oat bowl' })
    await user.click(screen.getByRole('button', { name: 'Delete' }))
    const dialog = await screen.findByRole('alertdialog')
    await user.click(within(dialog).getByRole('button', { name: 'Delete' }))

    expect(await within(dialog).findByText(/already used in a meal plan/i)).toBeInTheDocument()
  })
})

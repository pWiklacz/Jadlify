import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/client'
import type { Product } from '../products/types'
import { RecipesPage } from './RecipesPage'
import type { Recipe, RecipeCatalogResponse, RecipeSummary } from './types'

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
  name: 'Płatki owsiane',
  barcode: '111',
  calories: 380,
  protein: 13,
  fat: 7,
  carbohydrates: 67,
  ...nullExtended,
}

const milk: Product = {
  id: 'p2',
  name: 'Mleko',
  barcode: '222',
  calories: 60,
  protein: 3.4,
  fat: 3,
  carbohydrates: 5,
  ...nullExtended,
}

let catalog: RecipeSummary[] = []
let recipeDetails: Record<string, Recipe> = {}
let productSearch: Product[] = []
/** Every catalog URL the page requested, so tests can assert search/sort wiring. */
let catalogRequests: string[] = []

function summary(overrides: Partial<RecipeSummary> = {}): RecipeSummary {
  return {
    id: 'r1',
    name: 'Owsianka',
    portions: 2,
    ingredientCount: 1,
    totalMacros: { calories: 380, protein: 13, fat: 7, carbohydrates: 67 },
    perServingMacros: { calories: 190, protein: 6.5, fat: 3.5, carbohydrates: 33.5 },
    isInPlan: false,
    ...overrides,
  }
}

function detail(id: string, overrides: Partial<Recipe> = {}): Recipe {
  return {
    id,
    name: 'Owsianka',
    portions: 2,
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
    isInPlan: false,
    ...overrides,
  }
}

beforeEach(() => {
  vi.clearAllMocks()
  catalog = []
  recipeDetails = {}
  productSearch = [oats, milk]
  catalogRequests = []

  mockApiClient.get.mockImplementation(async (path: string) => {
    if (path.startsWith('/api/recipes/catalog')) {
      catalogRequests.push(path)
      const params = new URLSearchParams(path.slice(path.indexOf('?') + 1))
      const search = params.get('search')?.toLowerCase() ?? ''
      const matches = search
        ? catalog.filter((recipe) => recipe.name.toLowerCase().includes(search))
        : catalog
      const response: RecipeCatalogResponse = {
        items: matches,
        total: matches.length,
        skip: 0,
        take: 100,
      }
      return response
    }
    if (path.startsWith('/api/recipes/')) {
      const id = path.slice('/api/recipes/'.length)
      const found = recipeDetails[id]
      if (!found) {
        throw new ApiError(404, 'Not found')
      }
      return found
    }
    if (path.startsWith('/api/products?')) {
      return productSearch
    }
    throw new Error(`unexpected GET ${path}`)
  })

  mockApiClient.post.mockImplementation(async (path: string, body: unknown) => {
    if (path === '/api/recipes') {
      const request = body as { name: string; portions: number }
      catalog = [summary({ id: 'r-new', name: request.name, portions: request.portions })]
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
    <MemoryRouter initialEntries={['/recipes']}>
      <QueryClientProvider client={queryClient}>
        <RecipesPage />
      </QueryClientProvider>
    </MemoryRouter>,
  )
}

describe('RecipesPage', () => {
  it('shows the empty state when products exist but recipes do not', async () => {
    renderPage()

    expect(await screen.findByText('Nie masz jeszcze przepisów')).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Utwórz pierwszy przepis' }),
    ).toBeEnabled()
  })

  it('routes to products first when the product catalog is empty', async () => {
    productSearch = []
    renderPage()

    expect(await screen.findByText('Zacznij od dodania produktów')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Dodaj pierwszy produkt' })).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Utwórz pierwszy przepis' })).toBeDisabled()
  })

  it('renders per-serving macros, the count and the in-plan badge', async () => {
    catalog = [
      summary({ id: 'r1', name: 'Owsianka', isInPlan: true }),
      summary({ id: 'r2', name: 'Kurczak z ryżem', isInPlan: false }),
    ]
    renderPage()

    expect(await screen.findByText('2 przepisy')).toBeInTheDocument()

    const owsianka = screen.getByText('Owsianka').closest('li')
    expect(owsianka).not.toBeNull()
    expect(within(owsianka!).getByText('W planie')).toBeInTheDocument()
    expect(within(owsianka!).getByText('190')).toBeInTheDocument()
    expect(within(owsianka!).getByText('kcal / porcję')).toBeInTheDocument()
    expect(within(owsianka!).getByText('2 porcje · 1 składnik')).toBeInTheDocument()

    const kurczak = screen.getByText('Kurczak z ryżem').closest('li')
    expect(within(kurczak!).queryByText('W planie')).not.toBeInTheDocument()
  })

  it('sends the debounced search term to the catalog endpoint', async () => {
    const user = userEvent.setup()
    catalog = [summary({ id: 'r1', name: 'Owsianka' })]
    renderPage()

    await screen.findByText('Owsianka')
    await user.type(screen.getByLabelText('Szukaj przepisu po nazwie'), 'kurcz')

    await waitFor(() =>
      expect(catalogRequests.some((path) => path.includes('search=kurcz'))).toBe(true),
    )
    expect(await screen.findByText(/Brak przepisów dla „kurcz”/)).toBeInTheDocument()
  })

  it('requests the chosen sort order', async () => {
    const user = userEvent.setup()
    catalog = [summary()]
    renderPage()

    await screen.findByText('Owsianka')
    await user.selectOptions(
      screen.getByLabelText('Sortowanie przepisów'),
      'CaloriesPerServingAsc',
    )

    await waitFor(() =>
      expect(
        catalogRequests.some((path) => path.includes('sort=CaloriesPerServingAsc')),
      ).toBe(true),
    )
  })

  it('creates a recipe with a live macro summary', async () => {
    const user = userEvent.setup()
    renderPage()

    await screen.findByText('Nie masz jeszcze przepisów')
    await user.click(screen.getByRole('button', { name: 'Utwórz pierwszy przepis' }))

    const dialog = await screen.findByRole('dialog', { name: 'Nowy przepis' })
    await user.type(within(dialog).getByLabelText('Nazwa przepisu'), 'Miska śniadaniowa')
    await user.clear(within(dialog).getByLabelText('Liczba porcji'))
    await user.type(within(dialog).getByLabelText('Liczba porcji'), '2')

    await user.click(within(dialog).getByLabelText('Składnik 1'))
    await user.click(await screen.findByRole('button', { name: /Płatki owsiane/ }))
    await user.type(
      within(dialog).getByLabelText('Gramatura składnika 1 w gramach (dla całego przepisu)'),
      '150',
    )

    // Oracle: 150 g at 380 kcal/100 g = 570 kcal total; 2 portions -> 285 kcal per serving.
    const preview = within(dialog).getByRole('region', {
      name: 'Podsumowanie wartości odżywczych',
    })
    expect(within(preview).getByText('570')).toBeInTheDocument()
    expect(within(preview).getByText('285')).toBeInTheDocument()

    await user.click(within(dialog).getByRole('button', { name: 'Zapisz przepis' }))

    await waitFor(() =>
      expect(mockApiClient.post).toHaveBeenCalledWith('/api/recipes', {
        name: 'Miska śniadaniowa',
        portions: 2,
        ingredients: [{ productId: 'p1', wholeRecipeGrams: 150 }],
      }),
    )
    expect(await screen.findByText('Dodano przepis.')).toBeInTheDocument()
  })

  it('reports per-field errors instead of saving an incomplete recipe', async () => {
    const user = userEvent.setup()
    renderPage()

    await screen.findByText('Nie masz jeszcze przepisów')
    await user.click(screen.getByRole('button', { name: 'Utwórz pierwszy przepis' }))

    const dialog = await screen.findByRole('dialog', { name: 'Nowy przepis' })
    await user.click(within(dialog).getByRole('button', { name: 'Zapisz przepis' }))

    expect(await within(dialog).findByText('Podaj nazwę przepisu.')).toBeInTheDocument()
    expect(within(dialog).getByText('Wybierz produkt dla tego składnika.')).toBeInTheDocument()
    expect(mockApiClient.post).not.toHaveBeenCalled()
  })

  it('blocks selecting the same product in two ingredient rows', async () => {
    const user = userEvent.setup()
    renderPage()

    await screen.findByText('Nie masz jeszcze przepisów')
    await user.click(screen.getByRole('button', { name: 'Utwórz pierwszy przepis' }))

    const dialog = await screen.findByRole('dialog', { name: 'Nowy przepis' })
    await user.click(within(dialog).getByLabelText('Składnik 1'))
    await user.click(await screen.findByRole('button', { name: /Płatki owsiane/ }))
    await user.click(within(dialog).getByRole('button', { name: '+ Dodaj składnik' }))
    await user.click(within(dialog).getByLabelText('Składnik 2'))

    expect(await screen.findByRole('button', { name: /Płatki owsiane/ })).toBeDisabled()
  })

  it('reorders ingredient rows with the move buttons', async () => {
    const user = userEvent.setup()
    renderPage()

    await screen.findByText('Nie masz jeszcze przepisów')
    await user.click(screen.getByRole('button', { name: 'Utwórz pierwszy przepis' }))

    const dialog = await screen.findByRole('dialog', { name: 'Nowy przepis' })
    await user.click(within(dialog).getByLabelText('Składnik 1'))
    await user.click(await screen.findByRole('button', { name: /Płatki owsiane/ }))
    await user.click(within(dialog).getByRole('button', { name: '+ Dodaj składnik' }))
    await user.click(within(dialog).getByLabelText('Składnik 2'))
    await user.click(await screen.findByRole('button', { name: /Mleko/ }))

    expect(within(dialog).getByLabelText('Składnik 1')).toHaveTextContent('Płatki owsiane')
    await user.click(within(dialog).getByRole('button', { name: 'Przesuń składnik 2 wyżej' }))

    expect(within(dialog).getByLabelText('Składnik 1')).toHaveTextContent('Mleko')
    expect(within(dialog).getByLabelText('Składnik 2')).toHaveTextContent('Płatki owsiane')
  })

  it('guards unsaved changes when closing the builder', async () => {
    const user = userEvent.setup()
    renderPage()

    await screen.findByText('Nie masz jeszcze przepisów')
    await user.click(screen.getByRole('button', { name: 'Utwórz pierwszy przepis' }))

    const dialog = await screen.findByRole('dialog', { name: 'Nowy przepis' })
    await user.type(within(dialog).getByLabelText('Nazwa przepisu'), 'Robocza nazwa')
    await user.click(within(dialog).getByRole('button', { name: 'Anuluj' }))

    const guard = await screen.findByRole('alertdialog', { name: 'Masz niezapisane zmiany' })
    await user.click(within(guard).getByRole('button', { name: 'Zostań w formularzu' }))

    expect(screen.getByDisplayValue('Robocza nazwa')).toBeInTheDocument()
  })

  it('adds a missing product from inside the builder without losing the draft', async () => {
    const user = userEvent.setup()
    renderPage()

    await screen.findByText('Nie masz jeszcze przepisów')
    await user.click(screen.getByRole('button', { name: 'Utwórz pierwszy przepis' }))

    const dialog = await screen.findByRole('dialog', { name: 'Nowy przepis' })
    await user.type(within(dialog).getByLabelText('Nazwa przepisu'), 'Po treningu')
    await user.click(within(dialog).getByLabelText('Składnik 1'))
    await user.click(await screen.findByRole('button', { name: 'Dodaj nowy produkt' }))

    const productDialog = await screen.findByRole('dialog', { name: 'Dodaj produkt' })
    await user.type(within(productDialog).getByLabelText('Nazwa produktu'), 'Odżywka białkowa')
    await user.type(within(productDialog).getByLabelText(/^Kalorie/i), '400')
    await user.type(within(productDialog).getByLabelText(/^Białko/i), '80')
    await user.type(within(productDialog).getByLabelText(/^Tłuszcz/i), '0')
    await user.type(within(productDialog).getByLabelText(/^Węglowodany/i), '0')
    await user.click(within(productDialog).getByRole('button', { name: 'Zapisz produkt' }))

    expect(await screen.findByDisplayValue('Po treningu')).toBeInTheDocument()
    expect(await screen.findByLabelText('Składnik 1')).toHaveTextContent('Odżywka białkowa')
  })

  it('loads the full recipe when editing from the index', async () => {
    const user = userEvent.setup()
    catalog = [summary({ id: 'r1', name: 'Owsianka' })]
    recipeDetails.r1 = detail('r1')
    renderPage()

    await screen.findByText('Owsianka')
    await user.click(screen.getByRole('button', { name: 'Edytuj przepis Owsianka' }))

    const dialog = await screen.findByRole('dialog', { name: 'Edytuj przepis' })
    expect(within(dialog).getByLabelText('Składnik 1')).toHaveTextContent('Płatki owsiane')

    const portions = within(dialog).getByLabelText('Liczba porcji')
    await user.clear(portions)
    await user.type(portions, '4')
    await user.click(within(dialog).getByRole('button', { name: 'Zapisz zmiany' }))

    await waitFor(() =>
      expect(mockApiClient.put).toHaveBeenCalledWith('/api/recipes/r1', {
        name: 'Owsianka',
        portions: 4,
        ingredients: [{ productId: 'p1', wholeRecipeGrams: 100 }],
      }),
    )
  })

  it('deletes a recipe after confirmation', async () => {
    const user = userEvent.setup()
    catalog = [summary({ id: 'r1', name: 'Owsianka' })]
    renderPage()

    await screen.findByText('Owsianka')
    await user.click(screen.getByRole('button', { name: 'Usuń przepis Owsianka' }))

    const dialog = await screen.findByRole('alertdialog', { name: 'Usunąć przepis?' })
    await user.click(within(dialog).getByRole('button', { name: 'Usuń przepis' }))

    await waitFor(() => expect(mockApiClient.del).toHaveBeenCalledWith('/api/recipes/r1'))
  })

  it('explains a 409 instead of retrying a delete that cannot succeed', async () => {
    const user = userEvent.setup()
    catalog = [summary({ id: 'r1', name: 'Owsianka', isInPlan: true })]
    mockApiClient.del.mockRejectedValue(new ApiError(409, 'Recipe in use'))
    renderPage()

    await screen.findByText('Owsianka')
    await user.click(screen.getByRole('button', { name: 'Usuń przepis Owsianka' }))

    const dialog = await screen.findByRole('alertdialog', { name: 'Usunąć przepis?' })
    await user.click(within(dialog).getByRole('button', { name: 'Usuń przepis' }))

    const conflict = await screen.findByRole('alertdialog', {
      name: 'Nie można jeszcze usunąć przepisu',
    })
    expect(
      within(conflict).getByText(/używany w zaplanowanych posiłkach/i),
    ).toBeInTheDocument()
    expect(
      within(conflict).getByRole('link', { name: 'Przejdź do planu posiłków' }),
    ).toHaveAttribute('href', '/meal-plan')
  })

  it('offers a retry when the catalog fails to load', async () => {
    mockApiClient.get.mockImplementation(async (path: string) => {
      if (path.startsWith('/api/recipes/catalog')) {
        throw new ApiError(500, 'boom')
      }
      return productSearch
    })
    renderPage()

    expect(await screen.findByText('Nie udało się pobrać przepisów')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Wczytaj ponownie' })).toBeInTheDocument()
  })
})

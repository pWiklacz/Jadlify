import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ProductsPage } from './ProductsPage'
import type { BarcodeLookupResponse, Product } from './types'

// vi.hoisted so the object exists when the hoisted vi.mock factory runs.
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

/** The package-size + 14 extended fields, all null. */
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
  calories: 350,
  protein: 10,
  fat: 5,
  carbohydrates: 60,
  brand: 'Bio Planet',
  category: 'GrainsAndBread',
  ...nullExtended,
}

const apple: Product = {
  id: 'p2',
  name: 'Jabłko',
  barcode: null,
  calories: 52,
  protein: 0.3,
  fat: 0.2,
  carbohydrates: 14,
  brand: null,
  category: 'Fruits',
  ...nullExtended,
}

let catalogItems: Product[] = []
let barcodeResponse: BarcodeLookupResponse | null = null

function catalogPage(url: string) {
  const query = new URLSearchParams(url.split('?')[1] ?? '')
  const search = (query.get('search') ?? '').toLowerCase()
  const category = query.get('category')
  const sort = query.get('sort') ?? 'NameAsc'

  let items = catalogItems.filter((product) => {
    if (search && !`${product.name} ${product.barcode ?? ''}`.toLowerCase().includes(search)) {
      return false
    }
    if (category === 'None') {
      return product.category == null
    }
    if (category) {
      return product.category === category
    }
    return true
  })

  if (sort === 'CaloriesAsc') {
    items = [...items].sort((a, b) => a.calories - b.calories)
  } else {
    items = [...items].sort((a, b) => a.name.localeCompare(b.name, 'pl'))
  }

  return { items, total: items.length, skip: 0, take: 100 }
}

beforeEach(() => {
  vi.clearAllMocks()
  catalogItems = []
  barcodeResponse = null

  mockApiClient.get.mockImplementation(async (path: string) => {
    if (path.startsWith('/api/products/catalog')) {
      return catalogPage(path)
    }
    if (path.startsWith('/api/products/barcode/')) {
      return barcodeResponse
    }
    throw new Error(`unexpected GET ${path}`)
  })
  mockApiClient.post.mockImplementation(async (path: string, body: unknown) => {
    if (path === '/api/products') {
      const created = { id: 'new-1', ...(body as Omit<Product, 'id'>) }
      catalogItems = [created, ...catalogItems]
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

function renderPage(initialEntries: string[] = ['/products']) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={initialEntries}>
        <Routes>
          <Route path="/products" element={<ProductsPage />} />
          <Route path="/products/:id" element={<div>detail route</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('ProductsPage', () => {
  it('shows the empty-catalog state when there are no products and no filters', async () => {
    renderPage()

    expect(await screen.findByText(/twój katalog jest jeszcze pusty/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /dodaj pierwszy produkt/i })).toBeInTheDocument()
  })

  it('lists products as detail links with category and macros', async () => {
    catalogItems = [oats, apple]
    renderPage()

    const oatsLink = await screen.findByRole('link', { name: /płatki owsiane/i })
    expect(oatsLink).toHaveAttribute('href', '/products/p1')
    // Category chips live inside the grid (the same labels also appear in the
    // filter <select>, so scope the assertion to the product list).
    const grid = screen.getByRole('list')
    expect(within(grid).getByText('Produkty zbożowe i pieczywo')).toBeInTheDocument()
    expect(within(grid).getByText('Owoce')).toBeInTheDocument()
    expect(screen.getByText('2 produkty')).toBeInTheDocument()
  })

  it('creates a product with all four macros and shows it in the list', async () => {
    const user = userEvent.setup()
    renderPage()

    await screen.findByText(/twój katalog jest jeszcze pusty/i)
    await user.click(screen.getByRole('button', { name: /dodaj pierwszy produkt/i }))

    const dialog = await screen.findByRole('dialog', { name: 'Dodaj produkt' })
    await user.type(within(dialog).getByLabelText('Nazwa produktu'), 'Granola')
    await user.type(within(dialog).getByLabelText(/^Kalorie/i), '420')
    await user.type(within(dialog).getByLabelText(/^Białko/i), '9')
    await user.type(within(dialog).getByLabelText(/^Tłuszcz/i), '12')
    await user.type(within(dialog).getByLabelText(/^Węglowodany/i), '65')
    await user.click(within(dialog).getByRole('button', { name: 'Zapisz produkt' }))

    await waitFor(() =>
      expect(mockApiClient.post).toHaveBeenCalledWith(
        '/api/products',
        expect.objectContaining({
          name: 'Granola',
          calories: 420,
          protein: 9,
          fat: 12,
          carbohydrates: 65,
          brand: null,
          category: null,
        }),
      ),
    )
    expect(await screen.findByRole('link', { name: /granola/i })).toBeInTheDocument()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('blocks the save until every required macro is filled', async () => {
    const user = userEvent.setup()
    renderPage()

    await screen.findByText(/twój katalog jest jeszcze pusty/i)
    await user.click(screen.getByRole('button', { name: /dodaj pierwszy produkt/i }))

    const dialog = await screen.findByRole('dialog', { name: 'Dodaj produkt' })
    await user.type(within(dialog).getByLabelText('Nazwa produktu'), 'Niepełny')
    await user.type(within(dialog).getByLabelText(/^Kalorie/i), '100')
    await user.click(within(dialog).getByRole('button', { name: 'Zapisz produkt' }))

    expect(await within(dialog).findByText(/uzupełnij wszystkie cztery wartości/i)).toBeInTheDocument()
    expect(mockApiClient.post).not.toHaveBeenCalled()
  })

  it('filters by the debounced search term', async () => {
    const user = userEvent.setup()
    catalogItems = [oats, apple]
    renderPage()

    await screen.findByRole('link', { name: /płatki owsiane/i })
    await user.type(screen.getByLabelText(/szukaj produktu/i), 'jab')

    expect(await screen.findByText('1 produkt')).toBeInTheDocument()
    await waitFor(() =>
      expect(screen.queryByRole('link', { name: /płatki owsiane/i })).not.toBeInTheDocument(),
    )
    expect(screen.getByRole('link', { name: /jabłko/i })).toBeInTheDocument()
    // The active-filter chip is offered.
    expect(screen.getByText('„jab”')).toBeInTheDocument()
  })

  it('filters by category', async () => {
    const user = userEvent.setup()
    catalogItems = [oats, apple]
    renderPage()

    await screen.findByRole('link', { name: /płatki owsiane/i })
    await user.selectOptions(screen.getByLabelText(/filtruj według kategorii/i), 'Fruits')

    await waitFor(() =>
      expect(screen.queryByRole('link', { name: /płatki owsiane/i })).not.toBeInTheDocument(),
    )
    expect(screen.getByRole('link', { name: /jabłko/i })).toBeInTheDocument()
  })

  it('shows the no-results state and can clear filters', async () => {
    const user = userEvent.setup()
    catalogItems = [oats, apple]
    renderPage()

    await screen.findByRole('link', { name: /płatki owsiane/i })
    await user.type(screen.getByLabelText(/szukaj produktu/i), 'zzz')

    expect(await screen.findByText(/brak pasujących produktów/i)).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: /wyczyść wyszukiwanie i filtry/i }))

    expect(await screen.findByRole('link', { name: /płatki owsiane/i })).toBeInTheDocument()
  })

  it('edits a product and submits the update', async () => {
    const user = userEvent.setup()
    catalogItems = [oats]
    renderPage()

    await screen.findByRole('link', { name: /płatki owsiane/i })
    await user.click(screen.getByRole('button', { name: /edytuj produkt płatki owsiane/i }))

    const dialog = await screen.findByRole('dialog', { name: 'Edytuj produkt' })
    const nameInput = within(dialog).getByLabelText('Nazwa produktu')
    await user.clear(nameInput)
    await user.type(nameInput, 'Płatki górskie')
    await user.click(within(dialog).getByRole('button', { name: 'Zapisz zmiany' }))

    await waitFor(() =>
      expect(mockApiClient.put).toHaveBeenCalledWith(
        '/api/products/p1',
        expect.objectContaining({ name: 'Płatki górskie', category: 'GrainsAndBread' }),
      ),
    )
  })

  it('deletes a product after confirmation', async () => {
    const user = userEvent.setup()
    catalogItems = [apple]
    renderPage()

    await screen.findByRole('link', { name: /jabłko/i })
    await user.click(screen.getByRole('button', { name: /usuń produkt jabłko/i }))

    const dialog = await screen.findByRole('alertdialog', { name: /usunąć produkt/i })
    await user.click(within(dialog).getByRole('button', { name: 'Usuń produkt' }))

    expect(mockApiClient.del).toHaveBeenCalledWith('/api/products/p2')
    await waitFor(() => expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument())
  })

  it('pre-fills the form from a Found barcode lookup', async () => {
    const user = userEvent.setup()
    barcodeResponse = {
      outcome: 'Found',
      barcode: '3017624010701',
      existingProductId: null,
      name: 'Nutella',
      brand: 'Ferrero',
      category: 'PantryAndDryGoods',
      calories: 539,
      protein: 6.3,
      fat: 30.9,
      carbohydrates: 57.5,
      ...nullExtended,
      packageSizeGrams: 400,
      sodium: 0.107,
    }
    renderPage()

    await screen.findByText(/twój katalog jest jeszcze pusty/i)
    await user.click(screen.getByRole('button', { name: /dodaj pierwszy produkt/i }))
    const dialog = await screen.findByRole('dialog', { name: 'Dodaj produkt' })
    await user.type(within(dialog).getByLabelText(/kod kreskowy/i), '3017624010701')
    await user.click(within(dialog).getByRole('button', { name: 'Wyszukaj' }))

    await waitFor(() =>
      expect((within(dialog).getByLabelText('Nazwa produktu') as HTMLInputElement).value).toBe(
        'Nutella',
      ),
    )
    expect((within(dialog).getByLabelText(/^Kalorie/i) as HTMLInputElement).value).toBe('539')
    expect(within(dialog).getByText(/znaleziono dane produktu/i)).toBeInTheDocument()
    // Sodium 0.107 g is shown in milligrams (×1000).
    expect((within(dialog).getByLabelText(/^Sód/i) as HTMLInputElement).value).toBe('107')
  })

  it('keeps the barcode and reports a NotFound lookup', async () => {
    const user = userEvent.setup()
    barcodeResponse = {
      outcome: 'NotFound',
      barcode: '999',
      existingProductId: null,
      name: null,
      brand: null,
      category: null,
      calories: null,
      protein: null,
      fat: null,
      carbohydrates: null,
      ...nullExtended,
    }
    renderPage()

    await screen.findByText(/twój katalog jest jeszcze pusty/i)
    await user.click(screen.getByRole('button', { name: /dodaj pierwszy produkt/i }))
    const dialog = await screen.findByRole('dialog', { name: 'Dodaj produkt' })
    await user.type(within(dialog).getByLabelText(/kod kreskowy/i), '999')
    await user.click(within(dialog).getByRole('button', { name: 'Wyszukaj' }))

    expect(await within(dialog).findByText(/nie znaleziono danych/i)).toBeInTheDocument()
    expect((within(dialog).getByLabelText(/kod kreskowy/i) as HTMLInputElement).value).toBe('999')
  })

  it('offers to edit the existing product when the barcode is already in the catalog', async () => {
    const user = userEvent.setup()
    catalogItems = [oats]
    barcodeResponse = {
      outcome: 'AlreadyInCatalog',
      barcode: '111',
      existingProductId: 'p1',
      name: 'Płatki owsiane',
      brand: null,
      category: null,
      calories: 350,
      protein: 10,
      fat: 5,
      carbohydrates: 60,
      ...nullExtended,
    }
    renderPage()

    await screen.findByRole('link', { name: /płatki owsiane/i })
    await user.click(screen.getByRole('button', { name: 'Dodaj produkt' }))
    const createDialog = await screen.findByRole('dialog', { name: 'Dodaj produkt' })
    await user.type(within(createDialog).getByLabelText(/kod kreskowy/i), '111')
    await user.click(within(createDialog).getByRole('button', { name: 'Wyszukaj' }))

    await user.click(await screen.findByRole('button', { name: /otwórz istniejący produkt/i }))

    const editDialog = await screen.findByRole('dialog', { name: 'Edytuj produkt' })
    expect((within(editDialog).getByLabelText('Nazwa produktu') as HTMLInputElement).value).toBe(
      'Płatki owsiane',
    )
  })

  it('guards unsaved changes when closing the form', async () => {
    const user = userEvent.setup()
    renderPage()

    await screen.findByText(/twój katalog jest jeszcze pusty/i)
    await user.click(screen.getByRole('button', { name: /dodaj pierwszy produkt/i }))
    const dialog = await screen.findByRole('dialog', { name: 'Dodaj produkt' })
    await user.type(within(dialog).getByLabelText('Nazwa produktu'), 'Robocza nazwa')
    await user.click(within(dialog).getByRole('button', { name: 'Anuluj' }))

    const confirm = await screen.findByRole('alertdialog', { name: /masz niezapisane zmiany/i })
    await user.click(within(confirm).getByRole('button', { name: /zostań w formularzu/i }))

    // The form is still open.
    expect(screen.getByRole('dialog', { name: 'Dodaj produkt' })).toBeInTheDocument()
  })
})

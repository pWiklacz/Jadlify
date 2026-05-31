import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { type ReactElement } from 'react'
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

/** The package-size + 14 extended fields, all null — the default for products without extended data. */
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

const p1: Product = {
  id: 'p1',
  name: 'Existing Oats',
  barcode: '111',
  calories: 350,
  protein: 10,
  fat: 5,
  carbohydrates: 60,
  ...nullExtended,
}

let listResponse: Product[] = []
let barcodeResponse: BarcodeLookupResponse | null = null

beforeEach(() => {
  vi.clearAllMocks()
  listResponse = []
  barcodeResponse = null

  mockApiClient.get.mockImplementation(async (path: string) => {
    if (path === '/api/products') {
      return listResponse
    }
    if (path.startsWith('/api/products/barcode/')) {
      return barcodeResponse
    }
    throw new Error(`unexpected GET ${path}`)
  })
  mockApiClient.put.mockResolvedValue(undefined)
  mockApiClient.del.mockResolvedValue(undefined)
})

afterEach(() => {
  vi.clearAllMocks()
})

function renderPage(ui: ReactElement = <ProductsPage />) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>)
}

describe('ProductsPage', () => {
  it('renders the empty state when the user has no products', async () => {
    listResponse = []
    renderPage()

    expect(await screen.findByText(/no products yet/i)).toBeInTheDocument()
  })

  it('lists the user products', async () => {
    listResponse = [p1]
    renderPage()

    expect(await screen.findByRole('heading', { name: 'Existing Oats' })).toBeInTheDocument()
    expect(screen.getByText('Barcode: 111')).toBeInTheDocument()
  })

  it('creates a product manually and shows it in the list', async () => {
    const user = userEvent.setup()
    mockApiClient.post.mockImplementation(async (_path: string, body: unknown) => {
      const created = { id: 'new-1', ...(body as Omit<Product, 'id'>) }
      listResponse = [created]
      return created
    })
    renderPage()

    await screen.findByText(/no products yet/i)
    await user.click(screen.getByRole('button', { name: 'Add product' }))

    await user.type(screen.getByLabelText('Name'), 'Granola')
    await user.type(screen.getByLabelText(/^Calories/i), '420')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(mockApiClient.post).toHaveBeenCalledWith('/api/products', {
      name: 'Granola',
      barcode: null,
      calories: 420,
      protein: 0,
      fat: 0,
      carbohydrates: 0,
      ...nullExtended,
    })
    expect(await screen.findByRole('heading', { name: 'Granola' })).toBeInTheDocument()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('pre-fills the form from a Found barcode lookup', async () => {
    const user = userEvent.setup()
    barcodeResponse = {
      outcome: 'Found',
      barcode: '3017624010701',
      existingProductId: null,
      name: 'Nutella',
      brand: 'Ferrero',
      calories: 539,
      protein: 6.3,
      fat: 30.9,
      carbohydrates: 57.5,
      ...nullExtended,
      packageSizeGrams: 400,
      saturatedFat: 10.6,
      sugars: 56.3,
    }
    renderPage()

    await screen.findByText(/no products yet/i)
    await user.click(screen.getByRole('button', { name: 'Add product' }))
    await user.type(screen.getByLabelText('Barcode'), '3017624010701')
    await user.click(screen.getByRole('button', { name: /look up/i }))

    await waitFor(() =>
      expect((screen.getByLabelText('Name') as HTMLInputElement).value).toBe('Nutella'),
    )
    expect((screen.getByLabelText(/^Calories/i) as HTMLInputElement).value).toBe('539')
    expect((screen.getByLabelText(/^Protein/i) as HTMLInputElement).value).toBe('6.3')
    expect(screen.getByRole('status')).toHaveTextContent(/pre-filled/i)
    // The extended section auto-expands and pre-fills the fields OFF returned.
    expect((screen.getByLabelText(/package size/i) as HTMLInputElement).value).toBe('400')
    expect((screen.getByLabelText(/^Saturated fat/i) as HTMLInputElement).value).toBe('10.6')
    expect((screen.getByLabelText(/^Sugars/i) as HTMLInputElement).value).toBe('56.3')
  })

  it('keeps the barcode and blanks the macros on a NotFound lookup', async () => {
    const user = userEvent.setup()
    barcodeResponse = {
      outcome: 'NotFound',
      barcode: '999',
      existingProductId: null,
      name: null,
      brand: null,
      calories: null,
      protein: null,
      fat: null,
      carbohydrates: null,
      ...nullExtended,
    }
    renderPage()

    await screen.findByText(/no products yet/i)
    await user.click(screen.getByRole('button', { name: 'Add product' }))
    await user.type(screen.getByLabelText('Barcode'), '999')
    await user.type(screen.getByLabelText(/^Calories/i), '50')
    await user.click(screen.getByRole('button', { name: /look up/i }))

    expect(await screen.findByText(/no data found/i)).toBeInTheDocument()
    expect((screen.getByLabelText('Barcode') as HTMLInputElement).value).toBe('999')
    expect((screen.getByLabelText(/^Calories/i) as HTMLInputElement).value).toBe('')
  })

  it('offers to edit the existing product when the barcode is already in the catalog', async () => {
    const user = userEvent.setup()
    listResponse = [p1]
    barcodeResponse = {
      outcome: 'AlreadyInCatalog',
      barcode: '111',
      existingProductId: 'p1',
      name: 'Existing Oats',
      brand: null,
      calories: 350,
      protein: 10,
      fat: 5,
      carbohydrates: 60,
      ...nullExtended,
    }
    renderPage()

    await screen.findByRole('heading', { name: 'Existing Oats' })
    await user.click(screen.getByRole('button', { name: 'Add product' }))
    await user.type(screen.getByLabelText('Barcode'), '111')
    await user.click(screen.getByRole('button', { name: /look up/i }))

    expect(await screen.findByText(/already in your catalog/i)).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: /edit existing product/i }))

    const dialog = await screen.findByRole('dialog')
    expect(within(dialog).getByRole('heading', { name: 'Edit product' })).toBeInTheDocument()
    expect((screen.getByLabelText('Name') as HTMLInputElement).value).toBe('Existing Oats')
  })

  it('edits a product and submits the update', async () => {
    const user = userEvent.setup()
    listResponse = [p1]
    renderPage()

    await screen.findByRole('heading', { name: 'Existing Oats' })
    await user.click(screen.getByRole('button', { name: 'Edit' }))

    const nameInput = screen.getByLabelText('Name')
    await user.clear(nameInput)
    await user.type(nameInput, 'Rolled Oats')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(mockApiClient.put).toHaveBeenCalledWith('/api/products/p1', {
      name: 'Rolled Oats',
      barcode: '111',
      calories: 350,
      protein: 10,
      fat: 5,
      carbohydrates: 60,
      ...nullExtended,
    })
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
  })

  it('shows a per-package macro line when package size is set', async () => {
    listResponse = [{ ...p1, packageSizeGrams: 400 }]
    renderPage()

    await screen.findByRole('heading', { name: 'Existing Oats' })
    // 350 kcal/100g × 400 g = 1400 kcal per package.
    expect(screen.getByText(/per package: 1400 kcal/i)).toBeInTheDocument()
  })

  it('submits manually entered extended fields', async () => {
    const user = userEvent.setup()
    mockApiClient.post.mockImplementation(async (_path: string, body: unknown) => ({
      id: 'new-1',
      ...(body as Omit<Product, 'id'>),
    }))
    renderPage()

    await screen.findByText(/no products yet/i)
    await user.click(screen.getByRole('button', { name: 'Add product' }))
    await user.type(screen.getByLabelText('Name'), 'Yogurt')
    await user.type(screen.getByLabelText(/^Calories/i), '60')

    await user.click(screen.getByRole('button', { name: /additional nutrition/i }))
    await user.type(screen.getByLabelText(/package size/i), '500')
    await user.type(screen.getByLabelText(/^Sugars/i), '4.5')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(mockApiClient.post).toHaveBeenCalledWith(
      '/api/products',
      expect.objectContaining({
        name: 'Yogurt',
        calories: 60,
        packageSizeGrams: 500,
        sugars: 4.5,
        saturatedFat: null,
      }),
    )
  })

  it('deletes a product after confirmation', async () => {
    const user = userEvent.setup()
    listResponse = [p1]
    renderPage()

    await screen.findByRole('heading', { name: 'Existing Oats' })
    await user.click(screen.getByRole('button', { name: 'Delete' }))

    const dialog = await screen.findByRole('alertdialog')
    await user.click(within(dialog).getByRole('button', { name: 'Delete' }))

    expect(mockApiClient.del).toHaveBeenCalledWith('/api/products/p1')
    await waitFor(() => expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument())
  })
})

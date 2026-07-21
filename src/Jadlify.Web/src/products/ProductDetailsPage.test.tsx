import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/client'
import { ProductDetailsPage } from './ProductDetailsPage'
import type { Product } from './types'

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
  packageSizeGrams: 500,
  ...nullExtended,
  saturatedFat: 1.2,
  sodium: 0.107,
  vitaminA: 0.0008,
}

beforeEach(() => {
  vi.clearAllMocks()
  mockApiClient.get.mockImplementation(async (path: string) => {
    if (path === '/api/products/p1') {
      return oats
    }
    throw new Error(`unexpected GET ${path}`)
  })
  mockApiClient.put.mockResolvedValue(undefined)
  mockApiClient.del.mockResolvedValue(undefined)
})

afterEach(() => {
  vi.clearAllMocks()
})

function renderDetail(path = '/products/p1') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/products" element={<div>lista produktów</div>} />
          <Route path="/products/:id" element={<ProductDetailsPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('ProductDetailsPage', () => {
  it('renders the product, metadata and extended nutrition in human units', async () => {
    renderDetail()

    expect(await screen.findByRole('heading', { name: 'Płatki owsiane' })).toBeInTheDocument()
    expect(screen.getByText('Bio Planet')).toBeInTheDocument()
    expect(screen.getByText('Produkty zbożowe i pieczywo')).toBeInTheDocument()
    expect(screen.getByText('Kod: 111')).toBeInTheDocument()
    expect(screen.getByText('Opakowanie: 500 g')).toBeInTheDocument()
    // Whole-package calculation section is present.
    expect(screen.getByText(/całe opakowanie · 500 g/i)).toBeInTheDocument()
    // Extended values converted from grams to their display units.
    expect(screen.getByText('1,2 g')).toBeInTheDocument()
    expect(screen.getByText('107 mg')).toBeInTheDocument()
    expect(screen.getByText('800 µg')).toBeInTheDocument()
  })

  it('opens the edit form seeded with the product', async () => {
    const user = userEvent.setup()
    renderDetail()

    await screen.findByRole('heading', { name: 'Płatki owsiane' })
    await user.click(screen.getByRole('button', { name: 'Edytuj produkt' }))

    const dialog = await screen.findByRole('dialog', { name: 'Edytuj produkt' })
    expect((within(dialog).getByLabelText('Nazwa produktu') as HTMLInputElement).value).toBe(
      'Płatki owsiane',
    )
    // Sodium (0.107 g) is seeded in milligrams.
    expect((within(dialog).getByLabelText(/^Sód/i) as HTMLInputElement).value).toBe('107')
  })

  it('deletes the product and returns to the catalog', async () => {
    const user = userEvent.setup()
    renderDetail()

    await screen.findByRole('heading', { name: 'Płatki owsiane' })
    await user.click(screen.getByRole('button', { name: 'Usuń produkt' }))

    const dialog = await screen.findByRole('alertdialog', { name: /usunąć produkt/i })
    await user.click(within(dialog).getByRole('button', { name: 'Usuń produkt' }))

    await waitFor(() => expect(mockApiClient.del).toHaveBeenCalledWith('/api/products/p1'))
    expect(await screen.findByText('lista produktów')).toBeInTheDocument()
  })

  it('shows a not-found message for a missing product', async () => {
    mockApiClient.get.mockRejectedValue(new ApiError(404, 'Not found'))
    renderDetail('/products/missing')

    expect(await screen.findByText(/nie znaleziono produktu/i)).toBeInTheDocument()
  })
})

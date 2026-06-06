import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ShoppingListPage } from './ShoppingListPage'
import type { ShoppingList } from './types'

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

let listsByDate: Record<string, ShoppingList> = {}

beforeEach(() => {
  vi.clearAllMocks()
  listsByDate = {}

  mockApiClient.get.mockImplementation(async (path: string) => {
    if (path.startsWith('/api/shopping-list?date=')) {
      const date = decodeURIComponent(path.slice('/api/shopping-list?date='.length))
      return listsByDate[date] ?? emptyList(date)
    }

    throw new Error(`unexpected GET ${path}`)
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
        <ShoppingListPage />
      </QueryClientProvider>
    </MemoryRouter>,
  )
}

function emptyList(date: string): ShoppingList {
  return {
    date,
    items: [],
    warnings: [],
  }
}

describe('ShoppingListPage', () => {
  it('fetches the shopping list for the selected date', async () => {
    const user = userEvent.setup()
    renderPage()

    const dateInput = await screen.findByLabelText('Date')
    await user.clear(dateInput)
    await user.type(dateInput, '2026-06-02')

    await waitFor(() =>
      expect(mockApiClient.get).toHaveBeenCalledWith('/api/shopping-list?date=2026-06-02'),
    )
  })

  it('renders items with one-decimal gram formatting in API order', async () => {
    listsByDate['2026-06-03'] = {
      date: '2026-06-03',
      items: [
        { productId: 'p2', productName: 'Apple', grams: 125 },
        { productId: 'p1', productName: 'Oats', grams: 62.25 },
      ],
      warnings: [],
    }
    const user = userEvent.setup()
    renderPage()

    const dateInput = await screen.findByLabelText('Date')
    await user.clear(dateInput)
    await user.type(dateInput, '2026-06-03')

    const list = await screen.findByRole('list')
    const rows = within(list).getAllByRole('listitem')
    expect(rows).toHaveLength(2)
    expect(rows[0]).toHaveTextContent('Apple')
    expect(rows[0]).toHaveTextContent('125.0 g')
    expect(rows[1]).toHaveTextContent('Oats')
    expect(rows[1]).toHaveTextContent('62.3 g')
  })

  it('shows an empty state with a meal-plan link', async () => {
    renderPage()

    expect(await screen.findByText(/no planned ingredients/i)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Plan recipes' })).toHaveAttribute(
      'href',
      '/meal-plan',
    )
  })

  it('renders warning messages when the API reports gaps', async () => {
    listsByDate['2026-06-04'] = {
      date: '2026-06-04',
      items: [],
      warnings: [
        {
          entryId: 'e1',
          recipeId: 'r-missing',
          message: 'Recipe r-missing was not found for meal-plan entry e1.',
        },
      ],
    }
    const user = userEvent.setup()
    renderPage()

    const dateInput = await screen.findByLabelText('Date')
    await user.clear(dateInput)
    await user.type(dateInput, '2026-06-04')

    expect(await screen.findByText(/this list may be incomplete/i)).toBeInTheDocument()
    expect(screen.getByText(/recipe r-missing was not found/i)).toBeInTheDocument()
  })

  it('shows a loading state while the request is pending', () => {
    mockApiClient.get.mockReturnValue(new Promise(() => undefined))

    renderPage()

    expect(screen.getByText('Loading shopping list.')).toBeInTheDocument()
  })

  it('shows an error state when the request fails', async () => {
    mockApiClient.get.mockRejectedValue(new Error('network failed'))

    renderPage()

    expect(await screen.findByRole('alert')).toHaveTextContent(/could not load/i)
  })
})

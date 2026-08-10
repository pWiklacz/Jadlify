import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/client'
import { addDays, todayIso } from '../planning/dateRange'
import type { MealPlanDay, MealPlanRange } from '../planning/types'
import { ShoppingListsPage } from './ShoppingListsPage'
import type { ShoppingListIndex, ShoppingListSummary } from './types'

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

let index: ShoppingListIndex

function summary(overrides: Partial<ShoppingListSummary> = {}): ShoppingListSummary {
  return {
    id: 'l1',
    name: 'Zakupy na weekend',
    status: 'Active',
    createdAt: '2026-06-01T10:00:00+00:00',
    completedAt: null,
    itemCount: 4,
    boughtCount: 1,
    sourceDays: ['2026-06-03', '2026-06-04'],
    ...overrides,
  }
}

/** A planner range where every day holds one entry, so the creator can show meal counts. */
function buildRange(from: string, to: string): MealPlanRange {
  const days: MealPlanDay[] = []
  for (let cursor = from; cursor <= to; cursor = addDays(cursor, 1)) {
    days.push({
      date: cursor,
      entries: [],
      entryMacros: [],
      total: { calories: 0, protein: 0, fat: 0, carbohydrates: 0 },
      goal: null,
      remaining: null,
    })
  }
  return { from, to, days }
}

beforeEach(() => {
  vi.clearAllMocks()
  index = { active: null, history: [] }

  mockApiClient.get.mockImplementation(async (path: string) => {
    if (path === '/api/shopping-lists') {
      return index
    }
    if (path.startsWith('/api/meal-plan/range')) {
      const url = new URLSearchParams(path.slice(path.indexOf('?') + 1))
      return buildRange(url.get('from') ?? '', url.get('to') ?? '')
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
        <ShoppingListsPage />
      </QueryClientProvider>
    </MemoryRouter>,
  )
}

describe('ShoppingListsPage', () => {
  it('shows the empty state with a create action when no list exists', async () => {
    renderPage()

    expect(await screen.findByText('Nie masz jeszcze listy zakupów')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Utwórz pierwszą listę' })).toBeInTheDocument()
  })

  it('renders the active list with its progress and a link to the detail view', async () => {
    index = { active: summary(), history: [] }
    renderPage()

    const active = await screen.findByRole('region', { name: 'Aktywna lista' })
    expect(within(active).getByText('Zakupy na weekend')).toBeInTheDocument()
    expect(within(active).getByText('AKTYWNA')).toBeInTheDocument()
    expect(within(active).getByRole('progressbar')).toHaveAttribute(
      'aria-valuetext',
      '1 z 4 kupione',
    )
    // A started list invites the user back in rather than offering a fresh start.
    expect(within(active).getByRole('link', { name: /Kontynuuj zakupy/ })).toHaveAttribute(
      'href',
      '/shopping-list/l1',
    )
  })

  it('separates completed lists into the history section', async () => {
    index = {
      active: null,
      history: [
        summary({
          id: 'l9',
          name: 'Zakupy z maja',
          status: 'Completed',
          completedAt: '2026-05-30T18:00:00+00:00',
          itemCount: 3,
          boughtCount: 3,
        }),
      ],
    }
    renderPage()

    const history = await screen.findByRole('region', { name: 'Poprzednie listy' })
    expect(within(history).getByText('Zakupy z maja')).toBeInTheDocument()
    expect(within(history).getByText('UKOŃCZONA')).toBeInTheDocument()
    expect(within(history).getByRole('link', { name: /Zakupy z maja/ })).toHaveAttribute(
      'href',
      '/shopping-list/l9',
    )

    // The active slot still invites creating the next list.
    const active = screen.getByRole('region', { name: 'Aktywna lista' })
    expect(within(active).getByText('Brak aktywnej listy')).toBeInTheDocument()
  })

  it('shows a retry-able error state when the index request fails', async () => {
    mockApiClient.get.mockRejectedValue(new Error('network down'))
    renderPage()

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Nie udało się pobrać list zakupów',
    )
    expect(screen.getByRole('button', { name: 'Wczytaj ponownie' })).toBeInTheDocument()
  })

  it('creates a list through the two-step day picker', async () => {
    mockApiClient.post.mockResolvedValue({
      id: 'l2',
      name: 'Zakupy na tydzień',
      status: 'Active',
      version: 1,
      createdAt: '2026-06-01T10:00:00+00:00',
      completedAt: null,
      sourceDays: [],
      items: [],
    })
    const user = userEvent.setup()
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Utwórz pierwszą listę' }))

    const dialog = await screen.findByRole('dialog', { name: 'Nowa lista zakupów' })
    expect(within(dialog).getByText('1 · Wybierz dni')).toBeInTheDocument()

    await user.click(within(dialog).getByRole('button', { name: 'Najbliższe 7 dni' }))
    await user.click(within(dialog).getByRole('button', { name: 'Sprawdź i wygeneruj' }))

    expect(within(dialog).getByText('2 · Sprawdź i wygeneruj')).toBeInTheDocument()
    const nameInput = within(dialog).getByLabelText(/Nazwa listy/i)
    await user.clear(nameInput)
    await user.type(nameInput, 'Zakupy na tydzień')

    await user.click(within(dialog).getByRole('button', { name: 'Wygeneruj listę zakupów' }))

    const today = todayIso()
    const expectedDays = Array.from({ length: 7 }, (_, offset) => addDays(today, offset))
    await waitFor(() =>
      expect(mockApiClient.post).toHaveBeenCalledWith('/api/shopping-lists', {
        name: 'Zakupy na tydzień',
        days: expectedDays,
      }),
    )
    expect(await screen.findByText('Utworzono listę zakupów.')).toBeInTheDocument()
  })

  it('refuses to advance without a selected day', async () => {
    const user = userEvent.setup()
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Utwórz pierwszą listę' }))
    const dialog = await screen.findByRole('dialog', { name: 'Nowa lista zakupów' })
    await user.click(within(dialog).getByRole('button', { name: 'Sprawdź i wygeneruj' }))

    expect(
      within(dialog).getByText('Zaznacz co najmniej jeden dzień, aby wygenerować listę.'),
    ).toBeInTheDocument()
    expect(mockApiClient.post).not.toHaveBeenCalled()
  })

  it('explains the 409 when an active list already exists', async () => {
    mockApiClient.post.mockRejectedValue(new ApiError(409, 'conflict'))
    const user = userEvent.setup()
    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Utwórz pierwszą listę' }))
    const dialog = await screen.findByRole('dialog', { name: 'Nowa lista zakupów' })
    await user.click(within(dialog).getByRole('button', { name: 'Najbliższe 7 dni' }))
    await user.click(within(dialog).getByRole('button', { name: 'Sprawdź i wygeneruj' }))
    await user.click(within(dialog).getByRole('button', { name: 'Wygeneruj listę zakupów' }))

    expect(
      await screen.findByText(
        'Masz już aktywną listę zakupów. Ukończ ją, zanim utworzysz nową.',
      ),
    ).toBeInTheDocument()
  })
})

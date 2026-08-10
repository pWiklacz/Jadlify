import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/client'
import { ShoppingListDetailPage } from './ShoppingListDetailPage'
import type {
  ShoppingListDetail,
  ShoppingListDiff,
  ShoppingListItem,
  ShoppingListItemSource,
} from './types'

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

const LIST_ID = 'l1'

let detail: ShoppingListDetail
let diff: ShoppingListDiff

function source(overrides: Partial<ShoppingListItemSource> = {}): ShoppingListItemSource {
  return {
    date: '2026-06-03',
    mealType: 'Breakfast',
    sourceLabel: 'Owsianka',
    grams: 50,
    ...overrides,
  }
}

function item(overrides: Partial<ShoppingListItem> = {}): ShoppingListItem {
  return {
    id: 'i1',
    productId: 'p1',
    productName: 'Płatki owsiane',
    category: 'GrainsAndBread',
    grams: 100,
    isBought: false,
    sources: [source()],
    ...overrides,
  }
}

function buildDetail(overrides: Partial<ShoppingListDetail> = {}): ShoppingListDetail {
  return {
    id: LIST_ID,
    name: 'Zakupy na weekend',
    status: 'Active',
    version: 4,
    createdAt: '2026-06-01T10:00:00+00:00',
    completedAt: null,
    sourceDays: ['2026-06-03', '2026-06-04'],
    items: [
      item(),
      item({
        id: 'i2',
        productId: 'p2',
        productName: 'Mleko',
        category: 'Dairy',
        grams: 250,
        sources: [source({ mealType: 'Dinner', date: '2026-06-04', sourceLabel: 'Naleśniki' })],
      }),
    ],
    ...overrides,
  }
}

function emptyDiff(overrides: Partial<ShoppingListDiff> = {}): ShoppingListDiff {
  return {
    listVersion: 4,
    sourceFingerprint: 'fp-1',
    hasChanges: false,
    added: [],
    removed: [],
    changed: [],
    sourceOnly: [],
    ...overrides,
  }
}

beforeEach(() => {
  vi.clearAllMocks()
  detail = buildDetail()
  diff = emptyDiff()

  mockApiClient.get.mockImplementation(async (path: string) => {
    if (path === `/api/shopping-lists/${LIST_ID}`) {
      return detail
    }
    if (path === `/api/shopping-lists/${LIST_ID}/diff`) {
      return diff
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
    <MemoryRouter initialEntries={[`/shopping-list/${LIST_ID}`]}>
      <QueryClientProvider client={queryClient}>
        <Routes>
          <Route path="/shopping-list/:id" element={<ShoppingListDetailPage />} />
        </Routes>
      </QueryClientProvider>
    </MemoryRouter>,
  )
}

function detailCalls(): string[] {
  return mockApiClient.get.mock.calls
    .map(([path]) => path as string)
    .filter((path) => path === `/api/shopping-lists/${LIST_ID}`)
}

describe('ShoppingListDetailPage', () => {
  /** The progress readout lives on the bar's `aria-valuetext`, whatever the visual variant. */
  async function findProgressText(): Promise<string | null> {
    const bar = await screen.findByRole('progressbar')
    return bar.getAttribute('aria-valuetext')
  }

  it('renders the list grouped by category with its progress', async () => {
    detail = buildDetail({ items: [item({ isBought: true }), item({ id: 'i2', productId: 'p2', productName: 'Mleko', category: 'Dairy' })] })
    renderPage()

    expect(await findProgressText()).toBe('1 z 2 kupione')

    const dairy = screen.getByRole('region', { name: 'Nabiał' })
    expect(within(dairy).getByRole('checkbox', { name: 'Mleko' })).toBeInTheDocument()

    const grains = screen.getByRole('region', { name: 'Produkty zbożowe i pieczywo' })
    expect(within(grains).getByRole('checkbox', { name: 'Płatki owsiane' })).toBeChecked()
  })

  it('shows the aggregated amount and, on demand, the meals it came from', async () => {
    const user = userEvent.setup()
    renderPage()

    const grains = await screen.findByRole('region', { name: 'Produkty zbożowe i pieczywo' })
    expect(within(grains).getByText('100 g')).toBeInTheDocument()

    // The breakdown is collapsed until asked for: a shopping list reads as amounts first.
    expect(within(grains).queryByText('Owsianka')).not.toBeInTheDocument()
    await user.click(within(grains).getByRole('button', { expanded: false }))

    expect(within(grains).getByText('Owsianka')).toBeInTheDocument()
    expect(within(grains).getByText('3 czerwca')).toBeInTheDocument()
    expect(within(grains).getByText('Śniadanie')).toBeInTheDocument()
    expect(within(grains).getByText('50 g')).toBeInTheDocument()
  })

  it('ticks an item optimistically and sends the expected version', async () => {
    let resolveToggle: ((value: ShoppingListDetail) => void) | undefined
    mockApiClient.patch.mockImplementation(
      () => new Promise<ShoppingListDetail>((resolve) => (resolveToggle = resolve)),
    )
    const user = userEvent.setup()
    renderPage()

    const checkbox = await screen.findByRole('checkbox', { name: 'Płatki owsiane' })
    await user.click(checkbox)

    // Optimistic: the tick lands before the request settles.
    expect(checkbox).toBeChecked()
    expect(mockApiClient.patch).toHaveBeenCalledWith(
      `/api/shopping-lists/${LIST_ID}/items/i1`,
      { isBought: true, expectedVersion: 4 },
    )

    resolveToggle?.({
      ...detail,
      version: 5,
      items: detail.items.map((entry) =>
        entry.id === 'i1' ? { ...entry, isBought: true } : entry,
      ),
    })
    await waitFor(() =>
      expect(screen.getByRole('progressbar')).toHaveAttribute(
        'aria-valuetext',
        '1 z 2 kupione',
      ),
    )
  })

  it('rolls the tick back when the write fails', async () => {
    mockApiClient.patch.mockRejectedValue(new Error('network down'))
    const user = userEvent.setup()
    renderPage()

    const checkbox = await screen.findByRole('checkbox', { name: 'Płatki owsiane' })
    await user.click(checkbox)

    await waitFor(() => expect(checkbox).not.toBeChecked())
    expect(
      await screen.findByText('Nie udało się zapisać zaznaczenia. Spróbuj ponownie.'),
    ).toBeInTheDocument()
  })

  it('regroups by recipe and by day without refetching the list', async () => {
    const user = userEvent.setup()
    renderPage()

    await screen.findByRole('checkbox', { name: 'Płatki owsiane' })
    const callsBefore = detailCalls().length

    await user.click(screen.getByRole('button', { name: 'Wg posiłków' }))
    const oatmeal = await screen.findByRole('region', { name: 'Owsianka' })
    expect(within(oatmeal).getByText('Płatki owsiane')).toBeInTheDocument()
    expect(screen.getByRole('region', { name: 'Naleśniki' })).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Wg dni' }))
    expect(
      await screen.findByRole('region', { name: 'Środa, 3 czerwca' }),
    ).toBeInTheDocument()
    expect(screen.getByRole('region', { name: 'Czwartek, 4 czerwca' })).toBeInTheDocument()

    expect(detailCalls()).toHaveLength(callsBefore)
  })

  it('freezes a completed list: read-only ticks and no complete action', async () => {
    detail = buildDetail({
      status: 'Completed',
      completedAt: '2026-06-05T18:00:00+00:00',
      items: [item({ isBought: true })],
    })
    renderPage()

    const checkbox = await screen.findByRole('checkbox', { name: 'Płatki owsiane' })
    expect(checkbox).toBeDisabled()
    expect(
      screen.queryByRole('button', { name: 'Oznacz listę jako ukończoną' }),
    ).not.toBeInTheDocument()
    expect(screen.getByText('UKOŃCZONA')).toBeInTheDocument()

    // A completed list is a frozen snapshot; it never asks the API for a diff.
    expect(
      mockApiClient.get.mock.calls.filter(([path]) => String(path).endsWith('/diff')),
    ).toHaveLength(0)
  })

  it('completes the active list with its expected version', async () => {
    mockApiClient.post.mockResolvedValue(
      buildDetail({ status: 'Completed', version: 5, completedAt: '2026-06-05T18:00:00+00:00' }),
    )
    const user = userEvent.setup()
    renderPage()

    await user.click(
      await screen.findByRole('button', { name: 'Oznacz listę jako ukończoną' }),
    )

    await waitFor(() =>
      expect(mockApiClient.post).toHaveBeenCalledWith(
        `/api/shopping-lists/${LIST_ID}/complete`,
        { expectedVersion: 4 },
      ),
    )
    expect(await screen.findByText('Lista została ukończona.')).toBeInTheDocument()
  })

  describe('plan drift', () => {
    beforeEach(() => {
      diff = emptyDiff({
        hasChanges: true,
        added: [
          {
            productId: 'p3',
            productName: 'Masło',
            category: 'Dairy',
            previousGrams: null,
            newGrams: 30,
          },
        ],
        removed: [
          {
            productId: 'p2',
            productName: 'Mleko',
            category: 'Dairy',
            previousGrams: 250,
            newGrams: null,
          },
        ],
        changed: [
          {
            productId: 'p1',
            productName: 'Płatki owsiane',
            category: 'GrainsAndBread',
            previousGrams: 100,
            newGrams: 150,
          },
        ],
      })
    })

    it('banners the change and shows the three diff sections with before/after amounts', async () => {
      const user = userEvent.setup()
      renderPage()

      expect(await screen.findByText(/Twój plan posiłków zmienił się/)).toBeInTheDocument()
      await user.click(screen.getByRole('button', { name: 'Przejrzyj zmiany' }))

      const dialog = await screen.findByRole('dialog', { name: 'Plan posiłków się zmienił' })
      const added = within(dialog).getByRole('region', { name: 'Dojdą nowe produkty' })
      expect(within(added).getByText('30 g')).toBeInTheDocument()

      const removed = within(dialog).getByRole('region', { name: 'Znikną z listy' })
      expect(within(removed).getByText('250 g')).toBeInTheDocument()

      // Before/after are separate elements now; the pairing is what matters.
      const changed = within(dialog).getByRole('region', { name: 'Zmieni się ilość' })
      expect(within(changed).getByText('z 100 g na 150 g')).toBeInTheDocument()
      expect(within(changed).getByText('100 g')).toBeInTheDocument()
      expect(within(changed).getByText('150 g')).toBeInTheDocument()
    })

    it('does not mutate when the diff is dismissed', async () => {
      const user = userEvent.setup()
      renderPage()

      await user.click(await screen.findByRole('button', { name: 'Przejrzyj zmiany' }))
      const dialog = await screen.findByRole('dialog', { name: 'Plan posiłków się zmienił' })
      await user.click(within(dialog).getByRole('button', { name: 'Zostaw bez zmian' }))

      await waitFor(() =>
        expect(
          screen.queryByRole('dialog', { name: 'Plan posiłków się zmienił' }),
        ).not.toBeInTheDocument(),
      )
      expect(mockApiClient.post).not.toHaveBeenCalled()
    })

    it('applies the refresh with the versions the preview showed', async () => {
      mockApiClient.post.mockResolvedValue(buildDetail({ version: 5 }))
      const user = userEvent.setup()
      renderPage()

      await user.click(await screen.findByRole('button', { name: 'Przejrzyj zmiany' }))
      const dialog = await screen.findByRole('dialog', { name: 'Plan posiłków się zmienił' })
      await user.click(within(dialog).getByRole('button', { name: 'Zaktualizuj listę' }))

      await waitFor(() =>
        expect(mockApiClient.post).toHaveBeenCalledWith(
          `/api/shopping-lists/${LIST_ID}/refresh`,
          { expectedVersion: 4, expectedSourceFingerprint: 'fp-1' },
        ),
      )
      expect(await screen.findByText('Zaktualizowano listę zakupów.')).toBeInTheDocument()
    })

    it('keeps the dialog open and re-previews when the plan moved again (409)', async () => {
      mockApiClient.post.mockRejectedValue(new ApiError(409, 'source changed'))
      const user = userEvent.setup()
      renderPage()

      await user.click(await screen.findByRole('button', { name: 'Przejrzyj zmiany' }))
      const dialog = await screen.findByRole('dialog', { name: 'Plan posiłków się zmienił' })

      // The next preview reports a newer fingerprint.
      diff = { ...diff, listVersion: 4, sourceFingerprint: 'fp-2' }
      await user.click(within(dialog).getByRole('button', { name: 'Zaktualizuj listę' }))

      expect(
        await within(dialog).findByText(/Twój plan posiłków zmienił się ponownie/),
      ).toBeInTheDocument()
      expect(
        screen.getByRole('dialog', { name: 'Plan posiłków się zmienił' }),
      ).toBeInTheDocument()

      // A second confirmation carries the re-read fingerprint, not the stale one.
      mockApiClient.post.mockResolvedValue(buildDetail({ version: 5 }))
      await user.click(within(dialog).getByRole('button', { name: 'Zaktualizuj listę' }))

      await waitFor(() =>
        expect(mockApiClient.post).toHaveBeenLastCalledWith(
          `/api/shopping-lists/${LIST_ID}/refresh`,
          { expectedVersion: 4, expectedSourceFingerprint: 'fp-2' },
        ),
      )
    })
  })
})

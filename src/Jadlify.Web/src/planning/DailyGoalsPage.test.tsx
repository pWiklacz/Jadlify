import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { DailyGoalsPage } from './DailyGoalsPage'
import type { DailyGoal } from './types'

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

let dailyGoal: DailyGoal | null = null

beforeEach(() => {
  vi.clearAllMocks()
  dailyGoal = null

  mockApiClient.get.mockImplementation(async (path: string) => {
    if (path === '/api/daily-goal') {
      return dailyGoal
    }
    throw new Error(`unexpected GET ${path}`)
  })

  mockApiClient.put.mockImplementation(async (path: string, body: unknown) => {
    if (path === '/api/daily-goal') {
      dailyGoal = body as DailyGoal
      return undefined
    }
    throw new Error(`unexpected PUT ${path}`)
  })
})

afterEach(() => {
  vi.clearAllMocks()
})

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={queryClient}>
      <DailyGoalsPage />
    </QueryClientProvider>,
  )
}

describe('DailyGoalsPage', () => {
  it('shows the empty state and saves the current goal', async () => {
    const user = userEvent.setup()
    renderPage()

    expect(await screen.findByText(/no daily goal is configured/i)).toBeInTheDocument()

    await user.type(screen.getByLabelText('Calories'), '2200')
    await user.type(screen.getByLabelText('Protein (g)'), '160')
    await user.type(screen.getByLabelText('Fat (g)'), '70')
    await user.type(screen.getByLabelText('Carbohydrates (g)'), '240')
    await user.click(screen.getByRole('button', { name: 'Save goal' }))

    await waitFor(() =>
      expect(mockApiClient.put).toHaveBeenCalledWith('/api/daily-goal', {
        calories: 2200,
        protein: 160,
        fat: 70,
        carbohydrates: 240,
      }),
    )
    expect(await screen.findByText('Saved.')).toBeInTheDocument()
  })
})

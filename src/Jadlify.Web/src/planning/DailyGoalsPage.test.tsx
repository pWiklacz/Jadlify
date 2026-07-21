import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/client'
import { DailyGoalsPage } from './DailyGoalsPage'
import type { DailyGoal, DailyMacroSummary } from './types'

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
let daySummary: DailyMacroSummary

beforeEach(() => {
  vi.clearAllMocks()
  dailyGoal = null
  daySummary = {
    date: '2026-07-21',
    entries: [],
    total: { calories: 1200, protein: 70, fat: 40, carbohydrates: 130 },
    goal: null,
    remaining: null,
  }

  mockApiClient.get.mockImplementation(async (path: string) => {
    if (path === '/api/daily-goal') {
      return dailyGoal
    }
    if (path.startsWith('/api/meal-plan/summary?date=')) {
      return daySummary
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
    <MemoryRouter>
      <QueryClientProvider client={queryClient}>
        <DailyGoalsPage />
      </QueryClientProvider>
    </MemoryRouter>,
  )
}

/** Fills the four goal fields; the form must already be open. */
async function fillGoal(
  user: ReturnType<typeof userEvent.setup>,
  values: { calories: string; protein: string; fat: string; carbohydrates: string },
) {
  const form = screen.getByRole('form', { name: 'Formularz dziennych celów' })
  await user.clear(within(form).getByLabelText('Kalorie'))
  await user.type(within(form).getByLabelText('Kalorie'), values.calories)
  await user.clear(within(form).getByLabelText('Białko (g)'))
  await user.type(within(form).getByLabelText('Białko (g)'), values.protein)
  await user.clear(within(form).getByLabelText('Tłuszcz (g)'))
  await user.type(within(form).getByLabelText('Tłuszcz (g)'), values.fat)
  await user.clear(within(form).getByLabelText('Węglowodany (g)'))
  await user.type(within(form).getByLabelText('Węglowodany (g)'), values.carbohydrates)
  return form
}

describe('DailyGoalsPage', () => {
  it('shows the empty state and explains what still works without a goal', async () => {
    renderPage()

    expect(await screen.findByText('Nie masz jeszcze dziennych celów')).toBeInTheDocument()
    expect(screen.getByText('Plan posiłków i lista zakupów działają normalnie.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Ustaw dzienne cele' })).toBeInTheDocument()
  })

  it('saves a first goal and returns to the summary view', async () => {
    const user = userEvent.setup()
    renderPage()

    await screen.findByText('Nie masz jeszcze dziennych celów')
    await user.click(screen.getByRole('button', { name: 'Ustaw dzienne cele' }))
    // 160*4 + 240*4 + 70*9 = 2230 kcal, within tolerance of 2200 (max(75, 110)).
    await fillGoal(user, {
      calories: '2200',
      protein: '160',
      fat: '70',
      carbohydrates: '240',
    })
    await user.click(screen.getByRole('button', { name: 'Zapisz cele' }))

    await waitFor(() =>
      expect(mockApiClient.put).toHaveBeenCalledWith('/api/daily-goal', {
        calories: 2200,
        protein: 160,
        fat: 70,
        carbohydrates: 240,
      }),
    )
    expect(await screen.findByText('Zapisano dzienne cele.')).toBeInTheDocument()
    expect(await screen.findByText('Aktualny cel dzienny')).toBeInTheDocument()
  })

  it('renders the current goal and opens the editor prefilled', async () => {
    const user = userEvent.setup()
    dailyGoal = { calories: 2600, protein: 150, fat: 80, carbohydrates: 290 }
    renderPage()

    expect(await screen.findByText('Aktualny cel dzienny')).toBeInTheDocument()
    expect(screen.getByText('2600')).toBeInTheDocument()
    expect(screen.getByText('kcal / dzień')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Edytuj cele' }))

    const form = screen.getByRole('form', { name: 'Formularz dziennych celów' })
    expect(within(form).getByLabelText('Kalorie')).toHaveValue('2600')
    expect(within(form).getByLabelText('Białko (g)')).toHaveValue('150')
  })

  it('accepts a comma decimal separator', async () => {
    const user = userEvent.setup()
    renderPage()

    await screen.findByText('Nie masz jeszcze dziennych celów')
    await user.click(screen.getByRole('button', { name: 'Ustaw dzienne cele' }))
    await fillGoal(user, {
      calories: '2000',
      protein: '150,5',
      fat: '66',
      carbohydrates: '215',
    })
    await user.click(screen.getByRole('button', { name: 'Zapisz cele' }))

    await waitFor(() =>
      expect(mockApiClient.put).toHaveBeenCalledWith(
        '/api/daily-goal',
        expect.objectContaining({ protein: 150.5 }),
      ),
    )
  })

  it('warns about inconsistent macros without blocking the save', async () => {
    const user = userEvent.setup()
    renderPage()

    await screen.findByText('Nie masz jeszcze dziennych celów')
    await user.click(screen.getByRole('button', { name: 'Ustaw dzienne cele' }))
    // 10*4 + 10*4 + 10*9 = 170 kcal against a 2600 kcal target — far outside tolerance.
    await fillGoal(user, { calories: '2600', protein: '10', fat: '10', carbohydrates: '10' })

    expect(
      await screen.findByText(/odpowiadają około 170 kcal, a ustawiony cel to 2600 kcal/),
    ).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Zapisz cele' }))

    await waitFor(() =>
      expect(mockApiClient.put).toHaveBeenCalledWith(
        '/api/daily-goal',
        expect.objectContaining({ calories: 2600 }),
      ),
    )
  })

  it('reports per-field errors and focuses the first invalid field', async () => {
    const user = userEvent.setup()
    renderPage()

    await screen.findByText('Nie masz jeszcze dziennych celów')
    await user.click(screen.getByRole('button', { name: 'Ustaw dzienne cele' }))
    await user.click(screen.getByRole('button', { name: 'Zapisz cele' }))

    expect(await screen.findByText('Podaj dodatnią liczbę kalorii, np. 2600.')).toBeInTheDocument()
    expect(screen.getByLabelText('Kalorie')).toHaveFocus()
    expect(mockApiClient.put).not.toHaveBeenCalled()
  })

  it('rejects a zero calorie target but accepts zero macros', async () => {
    const user = userEvent.setup()
    renderPage()

    await screen.findByText('Nie masz jeszcze dziennych celów')
    await user.click(screen.getByRole('button', { name: 'Ustaw dzienne cele' }))
    await fillGoal(user, { calories: '0', protein: '0', fat: '0', carbohydrates: '0' })
    await user.click(screen.getByRole('button', { name: 'Zapisz cele' }))

    expect(await screen.findByText('Podaj dodatnią liczbę kalorii, np. 2600.')).toBeInTheDocument()
    expect(screen.queryByText(/Podaj wartość 0 lub większą/)).not.toBeInTheDocument()
    expect(mockApiClient.put).not.toHaveBeenCalled()
  })

  it('previews the entered target against the planned day as text, not colour alone', async () => {
    const user = userEvent.setup()
    renderPage()

    await screen.findByText('Nie masz jeszcze dziennych celów')
    await user.click(screen.getByRole('button', { name: 'Ustaw dzienne cele' }))
    await fillGoal(user, {
      calories: '2200',
      protein: '160',
      fat: '70',
      carbohydrates: '240',
    })

    const preview = screen.getByRole('region', { name: 'Porównanie z planem dnia' })
    // Planned 1200 kcal against a 2200 kcal target -> 1000 kcal left.
    expect(await within(preview).findByText('zostało 1000 kcal')).toBeInTheDocument()
    expect(within(preview).getByText('zostało 90 g')).toBeInTheDocument()
  })

  it('guards unsaved changes when leaving the editor', async () => {
    const user = userEvent.setup()
    dailyGoal = { calories: 2600, protein: 150, fat: 80, carbohydrates: 290 }
    renderPage()

    await screen.findByText('Aktualny cel dzienny')
    await user.click(screen.getByRole('button', { name: 'Edytuj cele' }))
    await user.clear(screen.getByLabelText('Kalorie'))
    await user.type(screen.getByLabelText('Kalorie'), '1800')
    await user.click(screen.getByRole('button', { name: 'Anuluj' }))

    const guard = await screen.findByRole('alertdialog', { name: 'Masz niezapisane zmiany' })
    await user.click(within(guard).getByRole('button', { name: 'Wróć do edycji' }))
    expect(screen.getByLabelText('Kalorie')).toHaveValue('1800')

    await user.click(screen.getByRole('button', { name: 'Anuluj' }))
    const reopened = await screen.findByRole('alertdialog', { name: 'Masz niezapisane zmiany' })
    await user.click(within(reopened).getByRole('button', { name: 'Odrzuć zmiany' }))

    // The saved goal is untouched by a discarded edit.
    expect(await screen.findByText('2600')).toBeInTheDocument()
  })

  it('never offers deleting the goal', async () => {
    dailyGoal = { calories: 2600, protein: 150, fat: 80, carbohydrates: 290 }
    renderPage()

    await screen.findByText('Aktualny cel dzienny')

    expect(screen.queryByRole('button', { name: /usuń/i })).not.toBeInTheDocument()
    expect(mockApiClient.del).not.toHaveBeenCalled()
  })

  it('offers a retry when the goal fails to load', async () => {
    mockApiClient.get.mockImplementation(async (path: string) => {
      if (path === '/api/daily-goal') {
        throw new ApiError(500, 'boom')
      }
      return daySummary
    })
    renderPage()

    expect(await screen.findByText('Nie udało się wczytać celów')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Wczytaj ponownie' })).toBeInTheDocument()
  })
})

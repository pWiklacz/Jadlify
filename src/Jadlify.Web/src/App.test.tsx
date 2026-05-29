import { render, screen } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { describe, expect, it, vi } from 'vitest'
import App from './App'
import { SessionProvider } from './auth/SessionProvider'

// Deterministic "signed out" Supabase: no session, no auth-state changes.
vi.mock('./lib/supabase', () => ({
  supabase: {
    auth: {
      getSession: vi.fn(async () => ({ data: { session: null }, error: null })),
      onAuthStateChange: vi.fn(() => ({
        data: { subscription: { unsubscribe: vi.fn() } },
      })),
    },
  },
}))

describe('App', () => {
  it('redirects an anonymous visitor to the login placeholder', async () => {
    const queryClient = new QueryClient()

    render(
      <SessionProvider>
        <QueryClientProvider client={queryClient}>
          <App />
        </QueryClientProvider>
      </SessionProvider>,
    )

    expect(
      await screen.findByRole('heading', { name: /sign in/i }),
    ).toBeInTheDocument()
  })
})

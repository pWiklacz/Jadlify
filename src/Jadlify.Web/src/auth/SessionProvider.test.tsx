import { act, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Session } from '@supabase/supabase-js'

type AuthChangeCallback = (event: string, session: Session | null) => void

// `mock`-prefixed names so Vitest allows referencing them inside the hoisted factory.
const mockListeners: AuthChangeCallback[] = []
const mockState: { session: Session | null } = { session: null }
const mockUnsubscribe = vi.fn()

vi.mock('../lib/supabase', () => ({
  supabase: {
    auth: {
      getSession: vi.fn(async () => ({ data: { session: mockState.session }, error: null })),
      onAuthStateChange: vi.fn((cb: AuthChangeCallback) => {
        mockListeners.push(cb)
        return { data: { subscription: { unsubscribe: mockUnsubscribe } } }
      }),
    },
  },
}))

import { SessionProvider } from './SessionProvider'
import { useSession } from './useSession'

function SessionProbe() {
  const { session, isLoading } = useSession()
  return (
    <div>
      <span data-testid="loading">{String(isLoading)}</span>
      <span data-testid="user">{session?.user?.id ?? 'none'}</span>
    </div>
  )
}

describe('SessionProvider', () => {
  beforeEach(() => {
    mockListeners.length = 0
    mockState.session = null
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('exposes the resolved session and clears the loading flag', async () => {
    render(
      <SessionProvider>
        <SessionProbe />
      </SessionProvider>,
    )

    await waitFor(() => expect(screen.getByTestId('loading')).toHaveTextContent('false'))
    expect(screen.getByTestId('user')).toHaveTextContent('none')
  })

  it('reacts to a Supabase auth-state change', async () => {
    render(
      <SessionProvider>
        <SessionProbe />
      </SessionProvider>,
    )

    await waitFor(() => expect(screen.getByTestId('loading')).toHaveTextContent('false'))

    const signedIn = { user: { id: 'user-xyz' } } as unknown as Session
    act(() => {
      for (const cb of mockListeners) {
        cb('SIGNED_IN', signedIn)
      }
    })

    await waitFor(() => expect(screen.getByTestId('user')).toHaveTextContent('user-xyz'))
  })
})

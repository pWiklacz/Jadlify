import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import type { Session } from '@supabase/supabase-js'
import { RequireAuth } from './RequireAuth'
import { SessionContext } from './SessionContext'
import type { SessionState } from './SessionContext'

function renderGuard(state: SessionState) {
  return render(
    <SessionContext.Provider value={state}>
      <MemoryRouter initialEntries={['/']}>
        <Routes>
          <Route element={<RequireAuth />}>
            <Route path="/" element={<div>Protected content</div>} />
          </Route>
          <Route path="/login" element={<div>Login placeholder</div>} />
        </Routes>
      </MemoryRouter>
    </SessionContext.Provider>,
  )
}

// Minimal stand-in; the guard only checks presence, not shape.
const fakeSession = { access_token: 'token' } as unknown as Session

describe('RequireAuth', () => {
  it('redirects to /login when there is no session', () => {
    renderGuard({ session: null, isLoading: false })

    expect(screen.getByText('Login placeholder')).toBeInTheDocument()
    expect(screen.queryByText('Protected content')).not.toBeInTheDocument()
  })

  it('renders the protected outlet when a session is present', () => {
    renderGuard({ session: fakeSession, isLoading: false })

    expect(screen.getByText('Protected content')).toBeInTheDocument()
  })

  it('shows a loader (no redirect) while the session is resolving', () => {
    renderGuard({ session: null, isLoading: true })

    expect(screen.getByRole('status')).toBeInTheDocument()
    expect(screen.queryByText('Login placeholder')).not.toBeInTheDocument()
    expect(screen.queryByText('Protected content')).not.toBeInTheDocument()
  })
})

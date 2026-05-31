import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { Session, User } from '@supabase/supabase-js'
import { LoginPage } from './LoginPage'
import { SessionContext } from '../auth/SessionContext'
import type { SessionState } from '../auth/SessionContext'
import { supabase } from '../lib/supabase'

// Supabase is auth-only here; the form calls these two methods on submit.
vi.mock('../lib/supabase', () => ({
  supabase: {
    auth: {
      signInWithPassword: vi.fn(),
      signUp: vi.fn(),
    },
  },
}))

const signInWithPassword = vi.mocked(supabase.auth.signInWithPassword)
const signUp = vi.mocked(supabase.auth.signUp)

// Minimal stand-ins; the form only needs presence, not full shape.
const fakeUser = { id: 'uid', email: 'user@example.com' } as unknown as User
const fakeSession = { access_token: 'token', user: fakeUser } as unknown as Session

function renderLogin(state: SessionState = { session: null, isLoading: false }) {
  return render(
    <SessionContext.Provider value={state}>
      <MemoryRouter initialEntries={['/login']}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/" element={<div>Protected home</div>} />
        </Routes>
      </MemoryRouter>
    </SessionContext.Provider>,
  )
}

afterEach(() => {
  vi.clearAllMocks()
})

describe('LoginPage', () => {
  it('signs in and navigates to the protected home on success', async () => {
    const user = userEvent.setup()
    signInWithPassword.mockResolvedValue({
      data: { user: fakeUser, session: fakeSession },
      error: null,
    })
    renderLogin()

    await user.type(screen.getByLabelText('E-mail'), 'user@example.com')
    await user.type(screen.getByLabelText('Hasło'), 'secret123')
    await user.click(screen.getByRole('button', { name: 'Zaloguj się' }))

    expect(await screen.findByText('Protected home')).toBeInTheDocument()
    expect(signInWithPassword).toHaveBeenCalledWith({
      email: 'user@example.com',
      password: 'secret123',
    })
  })

  it('shows a mapped error and stays on /login when credentials are invalid', async () => {
    const user = userEvent.setup()
    signInWithPassword.mockResolvedValue({
      data: { user: null, session: null },
      error: { code: 'invalid_credentials', message: 'Invalid login credentials' },
    } as unknown as Awaited<ReturnType<typeof supabase.auth.signInWithPassword>>)
    renderLogin()

    await user.type(screen.getByLabelText('E-mail'), 'user@example.com')
    await user.type(screen.getByLabelText('Hasło'), 'wrongpass')
    await user.click(screen.getByRole('button', { name: 'Zaloguj się' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Nieprawidłowy e-mail lub hasło.',
    )
    expect(screen.queryByText('Protected home')).not.toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Zaloguj się' })).toBeInTheDocument()
  })

  it('registers and navigates to the protected home when sign-up returns a session', async () => {
    const user = userEvent.setup()
    signUp.mockResolvedValue({
      data: { user: fakeUser, session: fakeSession },
      error: null,
    })
    renderLogin()

    // Toggle into registration mode.
    await user.click(screen.getByRole('button', { name: 'Załóż konto' }))
    await user.type(screen.getByLabelText('E-mail'), 'new@example.com')
    await user.type(screen.getByLabelText('Hasło'), 'secret123')
    await user.click(screen.getByRole('button', { name: 'Załóż konto' }))

    expect(await screen.findByText('Protected home')).toBeInTheDocument()
    expect(signUp).toHaveBeenCalledWith({
      email: 'new@example.com',
      password: 'secret123',
    })
  })

  it('redirects an already-authenticated visitor away from /login', () => {
    renderLogin({ session: fakeSession, isLoading: false })

    expect(screen.getByText('Protected home')).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Zaloguj się' })).not.toBeInTheDocument()
  })

  it('skips the network call and shows a validation error for a short password', async () => {
    const user = userEvent.setup()
    renderLogin()

    await user.type(screen.getByLabelText('E-mail'), 'user@example.com')
    await user.type(screen.getByLabelText('Hasło'), '123')
    await user.click(screen.getByRole('button', { name: 'Zaloguj się' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Hasło musi mieć co najmniej 6 znaków.',
    )
    expect(signInWithPassword).not.toHaveBeenCalled()
  })
})

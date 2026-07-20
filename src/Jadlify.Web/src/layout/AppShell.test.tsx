import { render, screen, within } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import type { Session } from '@supabase/supabase-js'
import { AppShell } from './AppShell'
import { navItems } from './navItems'
import { SessionContext } from '../auth/SessionContext'

// The shell mounts AccountMenu, which reads the session and (on sign-out) calls
// Supabase; stub the client so the import is inert under test.
vi.mock('../lib/supabase', () => ({
  supabase: { auth: { signOut: vi.fn() } },
}))

const fakeSession = {
  user: { email: 'user@example.com' },
} as unknown as Session

function renderShell(initialPath = '/') {
  return render(
    <SessionContext.Provider value={{ session: fakeSession, isLoading: false }}>
      <MemoryRouter initialEntries={[initialPath]}>
        <Routes>
          <Route element={<AppShell />}>
            <Route path="/" element={<div>Home content</div>} />
            {navItems
              .filter((item) => item.to !== '/')
              .map((item) => (
                <Route
                  key={item.to}
                  path={item.to}
                  element={<div>{item.label} content</div>}
                />
              ))}
          </Route>
        </Routes>
      </MemoryRouter>
    </SessionContext.Provider>,
  )
}

describe('AppShell', () => {
  it('renders the brand, the routed outlet, and the six Polish nav pills', () => {
    renderShell()

    expect(screen.getByText('Jadlify')).toBeInTheDocument()
    expect(screen.getByText('Home content')).toBeInTheDocument()

    const nav = screen.getByRole('navigation', { name: 'Główna nawigacja' })
    for (const item of navItems) {
      expect(within(nav).getByRole('link', { name: item.label })).toBeInTheDocument()
    }
  })

  it('marks the active route with aria-current="page"', () => {
    renderShell('/products')

    const nav = screen.getByRole('navigation', { name: 'Główna nawigacja' })
    expect(within(nav).getByRole('link', { name: 'Produkty' })).toHaveAttribute(
      'aria-current',
      'page',
    )
    expect(within(nav).getByRole('link', { name: 'Strona główna' })).not.toHaveAttribute(
      'aria-current',
    )
  })

  it('mounts the account control from the session', () => {
    renderShell()

    expect(
      screen.getByRole('button', { name: 'user@example.com' }),
    ).toBeInTheDocument()
  })
})

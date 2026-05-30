import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
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

function renderShell() {
  return render(
    <SessionContext.Provider value={{ session: fakeSession, isLoading: false }}>
      <MemoryRouter initialEntries={['/']}>
        <Routes>
          <Route element={<AppShell />}>
            <Route path="/" element={<div>Home content</div>} />
            {navItems
              .filter((item) => item.to !== '/')
              .map((item) => (
                <Route key={item.to} path={item.to} element={<div>{item.label} content</div>} />
              ))}
          </Route>
        </Routes>
      </MemoryRouter>
    </SessionContext.Provider>,
  )
}

describe('AppShell', () => {
  it('renders the brand, the routed outlet, and inline desktop nav', () => {
    renderShell()

    expect(screen.getByText('Jadlify')).toBeInTheDocument()
    expect(screen.getByText('Home content')).toBeInTheDocument()

    const mainNav = screen.getByRole('navigation', { name: 'Main' })
    for (const item of navItems) {
      expect(within(mainNav).getByRole('link', { name: item.label })).toBeInTheDocument()
    }
  })

  it('opens and closes the mobile drawer via the hamburger toggle', async () => {
    const user = userEvent.setup()
    renderShell()

    // Drawer starts closed (not mounted).
    expect(screen.queryByRole('navigation', { name: 'Mobile' })).not.toBeInTheDocument()

    const toggle = screen.getByRole('button', { name: /toggle navigation/i })
    expect(toggle).toHaveAttribute('aria-expanded', 'false')

    await user.click(toggle)

    const drawer = screen.getByRole('navigation', { name: 'Mobile' })
    expect(drawer).toBeInTheDocument()
    expect(toggle).toHaveAttribute('aria-expanded', 'true')
    expect(within(drawer).getByRole('link', { name: 'Products' })).toBeInTheDocument()

    await user.click(toggle)

    expect(screen.queryByRole('navigation', { name: 'Mobile' })).not.toBeInTheDocument()
    expect(toggle).toHaveAttribute('aria-expanded', 'false')
  })

  it('closes the drawer when a navigation link is selected', async () => {
    const user = userEvent.setup()
    renderShell()

    await user.click(screen.getByRole('button', { name: /toggle navigation/i }))

    const drawer = screen.getByRole('navigation', { name: 'Mobile' })
    await user.click(within(drawer).getByRole('link', { name: 'Recipes' }))

    expect(screen.queryByRole('navigation', { name: 'Mobile' })).not.toBeInTheDocument()
  })
})

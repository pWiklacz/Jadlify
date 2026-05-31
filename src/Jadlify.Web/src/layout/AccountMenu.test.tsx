import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { Session } from '@supabase/supabase-js'
import { AccountMenu } from './AccountMenu'
import { SessionContext } from '../auth/SessionContext'
import type { SessionState } from '../auth/SessionContext'
import { supabase } from '../lib/supabase'

vi.mock('../lib/supabase', () => ({
  supabase: { auth: { signOut: vi.fn() } },
}))

const signOut = vi.mocked(supabase.auth.signOut)

const fakeSession = {
  user: { email: 'user@example.com' },
} as unknown as Session

function renderMenu(state: SessionState = { session: fakeSession, isLoading: false }) {
  return render(
    <SessionContext.Provider value={state}>
      <AccountMenu />
      <button type="button">outside</button>
    </SessionContext.Provider>,
  )
}

afterEach(() => {
  vi.clearAllMocks()
})

describe('AccountMenu', () => {
  it('shows the signed-in user email on the trigger', () => {
    renderMenu()

    expect(
      screen.getByRole('button', { name: 'user@example.com' }),
    ).toBeInTheDocument()
    // Panel is not mounted until opened.
    expect(screen.queryByRole('menu')).not.toBeInTheDocument()
  })

  it('opens and closes via the trigger', async () => {
    const user = userEvent.setup()
    renderMenu()

    const trigger = screen.getByRole('button', { name: /user@example.com/ })
    expect(trigger).toHaveAttribute('aria-expanded', 'false')

    await user.click(trigger)
    expect(screen.getByRole('menu')).toBeInTheDocument()
    expect(trigger).toHaveAttribute('aria-expanded', 'true')

    await user.click(trigger)
    expect(screen.queryByRole('menu')).not.toBeInTheDocument()
    expect(trigger).toHaveAttribute('aria-expanded', 'false')
  })

  it('closes on Escape and returns focus to the trigger', async () => {
    const user = userEvent.setup()
    renderMenu()

    const trigger = screen.getByRole('button', { name: /user@example.com/ })
    await user.click(trigger)
    expect(screen.getByRole('menu')).toBeInTheDocument()

    await user.keyboard('{Escape}')

    expect(screen.queryByRole('menu')).not.toBeInTheDocument()
    expect(trigger).toHaveFocus()
  })

  it('closes when clicking outside the menu', async () => {
    const user = userEvent.setup()
    renderMenu()

    await user.click(screen.getByRole('button', { name: /user@example.com/ }))
    expect(screen.getByRole('menu')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'outside' }))

    expect(screen.queryByRole('menu')).not.toBeInTheDocument()
  })

  it('calls supabase signOut from Wyloguj without navigating', async () => {
    const user = userEvent.setup()
    signOut.mockResolvedValue({ error: null })
    renderMenu()

    await user.click(screen.getByRole('button', { name: /user@example.com/ }))
    await user.click(screen.getByRole('menuitem', { name: 'Wyloguj' }))

    expect(signOut).toHaveBeenCalledTimes(1)
  })

  it('falls back to a neutral label when the session has no email', () => {
    renderMenu({
      session: { user: {} } as unknown as Session,
      isLoading: false,
    })

    expect(screen.getByRole('button', { name: /Twoje konto/ })).toBeInTheDocument()
  })
})

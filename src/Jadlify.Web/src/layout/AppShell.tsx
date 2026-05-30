import { useState } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { navItems } from './navItems'

/**
 * Persistent responsive shell for the protected area: a top app bar with a
 * brand, inline navigation on desktop that collapses to a hamburger-toggled
 * drawer on mobile, and an account-area placeholder slot (sign-out wiring is
 * roadmap slice S-01). Renders the routed content in the main outlet.
 *
 * Tailwind breakpoints (`md:`) drive desktop vs. mobile presentation. The
 * mobile drawer is conditionally mounted from `open` state so its toggle is
 * observable in tests (jsdom does not evaluate CSS media queries).
 */
export function AppShell() {
  const [open, setOpen] = useState(false)

  const linkClass = ({ isActive }: { isActive: boolean }) =>
    [
      'rounded-md px-3 py-2 text-sm font-medium transition-colors',
      isActive
        ? 'bg-slate-900 text-white'
        : 'text-slate-700 hover:bg-slate-200',
    ].join(' ')

  return (
    <div className="flex min-h-screen flex-col bg-slate-50 text-slate-900">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex h-14 max-w-5xl items-center gap-3 px-4">
          <button
            type="button"
            aria-label="Toggle navigation"
            aria-expanded={open}
            aria-controls="mobile-nav"
            onClick={() => setOpen((value) => !value)}
            className="inline-flex h-9 w-9 items-center justify-center rounded-md text-slate-700 hover:bg-slate-200 md:hidden"
          >
            <span aria-hidden="true" className="text-xl leading-none">
              ☰
            </span>
          </button>

          <span className="text-lg font-bold tracking-tight">Jadlify</span>

          <nav
            aria-label="Main"
            className="ml-4 hidden items-center gap-1 md:flex"
          >
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.to === '/'}
                className={linkClass}
              >
                {item.label}
              </NavLink>
            ))}
          </nav>

          {/* Account-area placeholder; real account/sign-out UI is slice S-01. */}
          <span className="ml-auto text-sm text-slate-400">Account</span>
        </div>

        {open && (
          <nav
            id="mobile-nav"
            aria-label="Mobile"
            className="border-t border-slate-200 px-4 pb-3 md:hidden"
          >
            <ul className="flex flex-col gap-1 pt-2">
              {navItems.map((item) => (
                <li key={item.to}>
                  <NavLink
                    to={item.to}
                    end={item.to === '/'}
                    className={linkClass}
                    onClick={() => setOpen(false)}
                  >
                    {item.label}
                  </NavLink>
                </li>
              ))}
            </ul>
          </nav>
        )}
      </header>

      <main className="mx-auto w-full max-w-5xl flex-1 px-4 py-6">
        <Outlet />
      </main>
    </div>
  )
}

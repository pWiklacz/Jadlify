import { Link, NavLink, Outlet } from 'react-router-dom'
import { navItems } from './navItems'
import { AccountMenu } from './AccountMenu'
import { BrandMark } from './BrandMark'

/**
 * Persistent shell for the protected area, per the redesign: a warm dark header
 * with a left status-pill slot, a centered brand, and an account control on the
 * right, above a row of six Polish nav pills. The pill row wraps and centers on
 * desktop and scrolls horizontally on mobile.
 *
 * Responsiveness is CSS-only (the `design:` breakpoint) so there is a single DOM
 * tree — no `window.innerWidth`-driven duplication. The active route gets
 * `aria-current="page"` automatically from `NavLink`.
 */
export function AppShell() {
  const pillClass = ({ isActive }: { isActive: boolean }) =>
    [
      'flex-none rounded-pill px-3.5 py-2 text-[13.5px] transition-colors',
      isActive
        ? 'bg-parchment/10 font-semibold text-parchment'
        : 'font-medium text-parchment/60 hover:bg-parchment/[0.06] hover:text-parchment',
    ].join(' ')

  return (
    <div className="flex min-h-screen flex-col text-parchment">
      <header className="sticky top-0 z-30 border-b border-parchment/10 bg-ink/95 px-4 pt-3 backdrop-blur design:static design:bg-transparent design:px-[clamp(20px,4vw,52px)] design:pt-5 design:backdrop-blur-0">
        <div className="grid grid-cols-[1fr_auto_1fr] items-center gap-3 design:gap-4">
          {/* Left: contextual status pill (populated by pages in later phases). */}
          <div className="flex justify-start" />

          <Link
            to="/"
            aria-label="Jadlify — strona główna"
            className="flex justify-center rounded-field focus-visible:outline-none"
          >
            <BrandMark size={30} stacked withTagline />
          </Link>

          <div className="flex justify-end">
            <AccountMenu />
          </div>
        </div>

        <nav
          aria-label="Główna nawigacja"
          className="flex gap-1.5 overflow-x-auto px-1 pb-2 pt-2.5 design:flex-wrap design:justify-center design:gap-1 design:overflow-visible design:px-0 design:pb-4 design:pt-3.5"
        >
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/'}
              className={pillClass}
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
      </header>

      <main className="mx-auto w-full max-w-[1180px] flex-1 px-4 pb-24 pt-6 design:px-[clamp(16px,3.4vw,44px)] design:pt-8">
        <Outlet />
      </main>
    </div>
  )
}

import { useEffect, useRef, useState } from 'react'
import { supabase } from '../lib/supabase'
import { useSession } from '../auth/useSession'

/**
 * Account dropdown for the app bar: a trigger showing the signed-in user's
 * e-mail and a panel with a **Wyloguj** (sign-out) action.
 *
 * Identity is read client-side from the live Supabase session. Sign-out is
 * implicit: it only calls `supabase.auth.signOut()` and never navigates —
 * `SessionProvider.onAuthStateChange` clears the session and `RequireAuth`
 * redirects to `/login` (see the plan's Critical Implementation Details).
 *
 * The panel is conditionally mounted from `open` state (mirroring the mobile
 * drawer pattern in AppShell) so its open/close is observable in jsdom, which
 * does not evaluate CSS.
 */
export function AccountMenu() {
  const { session } = useSession()
  const [open, setOpen] = useState(false)
  const [isSigningOut, setIsSigningOut] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)
  const triggerRef = useRef<HTMLButtonElement>(null)

  const email = session?.user?.email ?? 'Twoje konto'

  // While open, Escape closes (returning focus to the trigger) and a pointer
  // press outside the menu closes it.
  useEffect(() => {
    if (!open) {
      return
    }

    function handleOutside(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setOpen(false)
      }
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setOpen(false)
        triggerRef.current?.focus()
      }
    }

    document.addEventListener('mousedown', handleOutside)
    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('mousedown', handleOutside)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [open])

  async function handleSignOut() {
    setIsSigningOut(true)
    try {
      await supabase.auth.signOut()
    } finally {
      setIsSigningOut(false)
    }
  }

  return (
    <div ref={containerRef} className="relative ml-auto">
      <button
        ref={triggerRef}
        type="button"
        aria-haspopup="menu"
        aria-expanded={open}
        onClick={() => setOpen((value) => !value)}
        className="flex items-center gap-1 rounded-md px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-200"
      >
        <span className="max-w-[12rem] truncate">{email}</span>
        <span aria-hidden="true" className="text-xs leading-none">
          ▾
        </span>
      </button>

      {open && (
        <div
          role="menu"
          aria-label="Konto"
          className="absolute right-0 z-10 mt-1 w-56 rounded-md border border-slate-200 bg-white py-1 shadow-lg"
        >
          <p className="truncate px-3 py-2 text-xs text-slate-500">{email}</p>
          <button
            type="button"
            role="menuitem"
            onClick={handleSignOut}
            disabled={isSigningOut}
            className="w-full px-3 py-2 text-left text-sm text-slate-700 hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSigningOut ? 'Wylogowywanie…' : 'Wyloguj'}
          </button>
        </div>
      )}
    </div>
  )
}

import { useEffect, useId, useRef, useState } from 'react'
import { supabase } from '../lib/supabase'
import { useSession } from '../auth/useSession'

/**
 * Account control in the shell header: a round trigger (labelled with the
 * signed-in e-mail) that discloses a small popover with the e-mail and a
 * **Wyloguj** (sign-out) action.
 *
 * This is a plain disclosure popover — not an ARIA menu — so it intentionally
 * avoids `role="menu"`/`menuitem` (which would promise arrow-key semantics we
 * don't implement). It wires `aria-expanded`/`aria-controls`, closes on Escape
 * (returning focus to the trigger) and on outside click.
 *
 * Sign-out is implicit: it only calls `supabase.auth.signOut()` and never
 * navigates — `SessionProvider.onAuthStateChange` clears the session and
 * `RequireAuth` redirects to `/login`.
 */
export function AccountMenu() {
  const { session } = useSession()
  const [open, setOpen] = useState(false)
  const [isSigningOut, setIsSigningOut] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)
  const triggerRef = useRef<HTMLButtonElement>(null)
  const panelId = useId()

  const email = session?.user?.email ?? 'Twoje konto'

  // While open, Escape closes (returning focus to the trigger) and a pointer
  // press outside the popover closes it.
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
    <div ref={containerRef} className="relative">
      <button
        ref={triggerRef}
        type="button"
        aria-label={email}
        aria-expanded={open}
        aria-controls={panelId}
        onClick={() => setOpen((value) => !value)}
        className="flex h-9 w-9 items-center justify-center rounded-full border border-parchment/15 text-parchment/75 transition-colors hover:bg-parchment/10 hover:text-parchment"
      >
        <svg
          width="18"
          height="18"
          viewBox="0 0 20 20"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.6"
          strokeLinecap="round"
          strokeLinejoin="round"
          aria-hidden="true"
        >
          <circle cx="10" cy="7" r="3" />
          <path d="M4.5 17c0-3 2.5-4.6 5.5-4.6s5.5 1.6 5.5 4.6" />
        </svg>
      </button>

      {open && (
        <div
          id={panelId}
          className="absolute right-0 z-[35] mt-2 w-56 rounded-panel border border-cream-border bg-cream p-1.5 text-espresso shadow-card"
        >
          <p className="truncate px-3 py-2 text-xs text-mocha">{email}</p>
          <button
            type="button"
            onClick={handleSignOut}
            disabled={isSigningOut}
            className="w-full rounded-field px-3 py-2 text-left text-sm font-medium text-espresso transition-colors hover:bg-cream-hover disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSigningOut ? 'Wylogowywanie…' : 'Wyloguj'}
          </button>
        </div>
      )}
    </div>
  )
}

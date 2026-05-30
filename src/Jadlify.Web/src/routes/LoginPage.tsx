import { useState } from 'react'
import type { FormEvent } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { supabase } from '../lib/supabase'
import { useSession } from '../auth/useSession'
import { toAuthMessage } from '../auth/authErrors'

type Mode = 'signin' | 'signup'

// Matches the local Supabase `minimum_password_length`. Kept in sync manually.
const MIN_PASSWORD_LENGTH = 6
// Pragmatic, intentionally loose shape check — catches obvious typos without
// rejecting valid addresses. Supabase remains the authority on deliverability.
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

/**
 * Sign-in / registration route. A single accessible form toggles between
 * "Zaloguj się" and "Załóż konto", validates lightly, maps Supabase errors to
 * friendly Polish messages, and on success navigates to the protected home.
 *
 * Auth-state propagation is implicit: the component only calls Supabase and
 * navigates. `SessionProvider.onAuthStateChange` is the single source that
 * updates the session, so authenticated visitors are bounced off `/login` here.
 */
export function LoginPage() {
  const { session, isLoading } = useSession()
  const navigate = useNavigate()

  const [mode, setMode] = useState<Mode>('signin')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  // Authed users never see the form — bounce them into the protected app.
  if (session) {
    return <Navigate to="/" replace />
  }

  // Mirror RequireAuth's loader idiom while the initial session resolves.
  if (isLoading) {
    return (
      <div
        role="status"
        aria-live="polite"
        className="flex min-h-screen items-center justify-center bg-slate-50 text-slate-600"
      >
        Ładowanie…
      </div>
    )
  }

  const isSignup = mode === 'signup'

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)

    const trimmedEmail = email.trim()
    if (!EMAIL_PATTERN.test(trimmedEmail)) {
      setError('Podaj prawidłowy adres e-mail.')
      return
    }
    if (password.length < MIN_PASSWORD_LENGTH) {
      setError('Hasło musi mieć co najmniej 6 znaków.')
      return
    }

    setIsSubmitting(true)
    try {
      if (isSignup) {
        const { data, error: signUpError } = await supabase.auth.signUp({
          email: trimmedEmail,
          password,
        })
        if (signUpError) {
          setError(toAuthMessage(signUpError))
          return
        }
        // Confirmations off → signUp returns a session and we proceed like
        // sign-in. If prod unexpectedly has confirmations on there's no session:
        // surface a neutral message rather than navigating (no confirmation
        // screen is built — see plan's Critical Implementation Details).
        if (!data.session) {
          setError(
            'Konto utworzone. Sprawdź skrzynkę e-mail, aby potwierdzić rejestrację.',
          )
          return
        }
      } else {
        const { error: signInError } = await supabase.auth.signInWithPassword({
          email: trimmedEmail,
          password,
        })
        if (signInError) {
          setError(toAuthMessage(signInError))
          return
        }
      }

      // Success: onAuthStateChange updates the session; the guard then allows /.
      navigate('/', { replace: true })
    } catch (caught) {
      setError(toAuthMessage(caught))
    } finally {
      setIsSubmitting(false)
    }
  }

  function switchMode() {
    setMode((current) => (current === 'signin' ? 'signup' : 'signin'))
    setError(null)
  }

  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-6 bg-slate-50 px-6 text-slate-900">
      <div className="w-full max-w-sm">
        <h1 className="mb-1 text-center text-3xl font-bold">
          {isSignup ? 'Załóż konto' : 'Zaloguj się'}
        </h1>
        <p className="mb-6 text-center text-slate-600">
          {isSignup
            ? 'Utwórz konto, aby zacząć planować posiłki.'
            : 'Zaloguj się, aby kontynuować.'}
        </p>

        <form onSubmit={handleSubmit} className="flex flex-col gap-4" noValidate>
          <div className="flex flex-col gap-1">
            <label htmlFor="email" className="text-sm font-medium">
              E-mail
            </label>
            <input
              id="email"
              name="email"
              type="email"
              autoComplete="email"
              required
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              disabled={isSubmitting}
              className="rounded-md border border-slate-300 px-3 py-2 focus:border-slate-500 focus:outline-none focus:ring-1 focus:ring-slate-500 disabled:opacity-60"
            />
          </div>

          <div className="flex flex-col gap-1">
            <label htmlFor="password" className="text-sm font-medium">
              Hasło
            </label>
            <input
              id="password"
              name="password"
              type="password"
              autoComplete={isSignup ? 'new-password' : 'current-password'}
              required
              minLength={MIN_PASSWORD_LENGTH}
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              disabled={isSubmitting}
              className="rounded-md border border-slate-300 px-3 py-2 focus:border-slate-500 focus:outline-none focus:ring-1 focus:ring-slate-500 disabled:opacity-60"
            />
          </div>

          {error && (
            <p role="alert" className="text-sm text-red-600">
              {error}
            </p>
          )}

          <button
            type="submit"
            disabled={isSubmitting}
            className="rounded-md bg-slate-900 px-4 py-2 font-medium text-white transition-colors hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSubmitting
              ? 'Proszę czekać…'
              : isSignup
                ? 'Załóż konto'
                : 'Zaloguj się'}
          </button>
        </form>

        <p className="mt-6 text-center text-sm text-slate-600">
          {isSignup ? 'Masz już konto?' : 'Nie masz jeszcze konta?'}{' '}
          <button
            type="button"
            onClick={switchMode}
            disabled={isSubmitting}
            className="font-medium text-slate-900 underline underline-offset-2 hover:text-slate-700 disabled:opacity-60"
          >
            {isSignup ? 'Zaloguj się' : 'Załóż konto'}
          </button>
        </p>
      </div>
    </main>
  )
}

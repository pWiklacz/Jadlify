import { useState } from 'react'
import type { FormEvent } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { supabase } from '../lib/supabase'
import { useSession } from '../auth/useSession'
import { toAuthMessage } from '../auth/authErrors'
import { BrandMark } from '../layout/BrandMark'
import { Button } from '../ui/Button'
import { TextField } from '../ui/Field'

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
 * Redesigned to the target auth surface: a warm dark backdrop with a brand
 * statement beside a cream card. Auth behaviour is unchanged — the component
 * only calls Supabase and navigates; `SessionProvider.onAuthStateChange` is the
 * single source that updates the session, so authenticated visitors are bounced
 * off `/login` here.
 */
export function LoginPage() {
  const { session, isLoading } = useSession()
  const navigate = useNavigate()

  const [mode, setMode] = useState<Mode>('signin')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
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
        className="flex min-h-screen items-center justify-center text-parchment/60"
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
    <main className="flex min-h-screen items-center justify-center px-5 py-10 text-parchment">
      <div className="flex w-full max-w-[920px] flex-col items-center gap-10 design:flex-row design:items-center design:gap-16">
        {/* Brand / value statement */}
        <aside className="flex flex-col items-center text-center design:flex-1 design:items-start design:text-left">
          <BrandMark size={34} withTagline />
          <p className="mt-6 max-w-[18ch] font-serif text-2xl leading-snug text-parchment design:text-3xl">
            Planuj posiłki, pilnuj makro i twórz listy zakupów na podstawie swojego
            planu.
          </p>
        </aside>

        {/* Auth card */}
        <div className="w-full max-w-[420px] rounded-card bg-cream p-7 text-espresso shadow-authcard animate-rise design:p-9">
          <h1 className="font-serif text-3xl font-normal leading-tight">
            {isSignup ? 'Załóż konto' : 'Zaloguj się'}
          </h1>
          <p className="mb-6 mt-1.5 text-sm text-mocha">
            {isSignup
              ? 'Utwórz prywatne konto — Twoje produkty i plany widzisz tylko Ty.'
              : 'Wróć do swojego planu posiłków i makro.'}
          </p>

          <form onSubmit={handleSubmit} className="flex flex-col gap-4" noValidate>
            <TextField
              id="email"
              name="email"
              type="email"
              label="E-mail"
              labelVariant="plain"
              autoComplete="email"
              inputMode="email"
              placeholder="ty@example.com"
              required
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              disabled={isSubmitting}
            />

            <TextField
              id="password"
              name="password"
              type={showPassword ? 'text' : 'password'}
              label="Hasło"
              labelVariant="plain"
              autoComplete={isSignup ? 'new-password' : 'current-password'}
              placeholder="Twoje hasło"
              required
              minLength={MIN_PASSWORD_LENGTH}
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              disabled={isSubmitting}
              trailing={
                <button
                  type="button"
                  onClick={() => setShowPassword((value) => !value)}
                  aria-label={showPassword ? 'Ukryj hasło' : 'Pokaż hasło'}
                  className="rounded-field px-2 py-1.5 text-xs font-bold uppercase tracking-wide text-mocha transition-colors hover:text-espresso"
                >
                  {showPassword ? 'Ukryj' : 'Pokaż'}
                </button>
              }
            />

            {error && (
              <p role="alert" className="flex items-start gap-2 text-sm text-danger">
                <span aria-hidden="true">▲</span>
                {error}
              </p>
            )}

            <Button type="submit" block isLoading={isSubmitting} className="mt-2">
              {isSubmitting
                ? 'Proszę czekać…'
                : isSignup
                  ? 'Załóż konto'
                  : 'Zaloguj się'}
            </Button>
          </form>

          <p className="mt-6 border-t border-dotted border-cream-line pt-5 text-center text-sm text-mocha">
            {isSignup ? 'Masz już konto?' : 'Nie masz jeszcze konta?'}{' '}
            <button
              type="button"
              onClick={switchMode}
              disabled={isSubmitting}
              className="font-bold text-terracotta-strong underline-offset-2 hover:underline disabled:opacity-60"
            >
              {isSignup ? 'Zaloguj się' : 'Załóż konto'}
            </button>
          </p>
        </div>
      </div>
    </main>
  )
}

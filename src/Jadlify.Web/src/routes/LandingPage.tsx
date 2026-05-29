import { useMe } from '../api/useMe'

/**
 * Post-login landing. Renders the `/api/me` result to prove the protected bearer
 * round-trip works end to end. Real feature pages arrive in later slices.
 */
export function LandingPage() {
  const { data, isPending, isError, error } = useMe()

  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-3 bg-slate-50 px-6 text-center text-slate-900">
      <h1 className="text-3xl font-bold">Jadlify</h1>

      {isPending && (
        <p role="status" aria-live="polite" className="text-slate-600">
          Loading your session…
        </p>
      )}

      {isError && (
        <p role="alert" className="text-red-600">
          Could not reach the API
          {error instanceof Error ? `: ${error.message}` : ''}.
        </p>
      )}

      {data && (
        <p className="text-slate-600">
          Signed in as{' '}
          <span className="font-mono font-semibold">{data.userId}</span>
        </p>
      )}
    </main>
  )
}

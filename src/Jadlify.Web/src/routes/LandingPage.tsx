import { useMe } from '../api/useMe'

/**
 * Post-login home. Renders the `/api/me` result to prove the protected bearer
 * round-trip works end to end. Lives inside the responsive app shell; real
 * dashboard content arrives in later slices.
 */
export function LandingPage() {
  const { data, isPending, isError, error } = useMe()

  return (
    <section className="flex flex-col gap-3">
      <h1 className="text-2xl font-bold">Home</h1>

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
    </section>
  )
}

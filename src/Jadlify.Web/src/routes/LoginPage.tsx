/**
 * Public placeholder login route. The real sign-in / registration / sign-out
 * flow is roadmap slice S-01; this exists so unauthenticated visitors (and the
 * route guard's redirect) have a destination.
 */
export function LoginPage() {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-3 bg-slate-50 px-6 text-center text-slate-900">
      <h1 className="text-3xl font-bold">Sign in</h1>
      <p className="max-w-md text-slate-600">
        The sign-in and registration flow is wired in a later slice (S-01). This
        is a placeholder so anonymous visitors have somewhere to land.
      </p>
    </main>
  )
}

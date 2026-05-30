import { Navigate, Outlet } from 'react-router-dom'
import { useSession } from './useSession'

/**
 * Route guard for protected areas: renders the routed outlet when a session is
 * present, redirects to the `/login` placeholder when not, and shows a loader
 * while the initial session is still resolving.
 */
export function RequireAuth() {
  const { session, isLoading } = useSession()

  if (isLoading) {
    return (
      <div
        role="status"
        aria-live="polite"
        className="flex min-h-screen items-center justify-center bg-slate-50 text-slate-600"
      >
        Loading…
      </div>
    )
  }

  if (!session) {
    return <Navigate to="/login" replace />
  }

  return <Outlet />
}

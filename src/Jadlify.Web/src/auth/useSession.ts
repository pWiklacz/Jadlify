import { useContext } from 'react'
import { SessionContext } from './SessionContext'

/** Reads the current Supabase session state; throws if used outside SessionProvider. */
export function useSession() {
  const context = useContext(SessionContext)
  if (context === undefined) {
    throw new Error('useSession must be used within a SessionProvider')
  }
  return context
}

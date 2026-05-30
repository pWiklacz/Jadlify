import { createContext } from 'react'
import type { Session } from '@supabase/supabase-js'

/** The current Supabase auth state exposed to the app. */
export interface SessionState {
  /** The active Supabase session, or null when signed out. */
  session: Session | null
  /** True until the initial session has been resolved. */
  isLoading: boolean
}

// undefined default lets useSession() detect usage outside the provider.
export const SessionContext = createContext<SessionState | undefined>(undefined)

import { supabase } from '../lib/supabase'
import { createApiClient } from './client'

// Read the token from the *current* Supabase session at call time (not cached at
// startup) so a refreshed token is used after Supabase rotates it.
async function getSupabaseAccessToken(): Promise<string | null> {
  const { data } = await supabase.auth.getSession()
  return data.session?.access_token ?? null
}

/** App-wide API client wired to the live Supabase session. */
export const apiClient = createApiClient(getSupabaseAccessToken)

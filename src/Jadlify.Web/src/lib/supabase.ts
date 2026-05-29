import { createClient } from '@supabase/supabase-js'

// The SPA uses Supabase ONLY for authentication/session management. All domain
// data flows through the ASP.NET Core API (see docs/reference/contract-surfaces.md).
const supabaseUrl = import.meta.env.VITE_SUPABASE_URL
const supabaseAnonKey = import.meta.env.VITE_SUPABASE_ANON_KEY

if (!supabaseUrl || !supabaseAnonKey) {
  throw new Error(
    'Missing Supabase configuration. Set VITE_SUPABASE_URL and ' +
      'VITE_SUPABASE_ANON_KEY (see .env.example).',
  )
}

// persistSession + autoRefreshToken (both on by default) keep the access token
// fresh in storage; the API client reads it per request so rotated tokens are used.
export const supabase = createClient(supabaseUrl, supabaseAnonKey)

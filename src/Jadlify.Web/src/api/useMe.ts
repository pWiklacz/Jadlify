import { useQuery } from '@tanstack/react-query'
import { apiClient } from './apiClient'
import { useSession } from '../auth/useSession'

/** Shape of `GET /api/me` (mirrors the API's MeResponse). */
export interface Me {
  userId: string
}

/**
 * Fetches the authenticated caller's identity from `GET /api/me`. Enabled only
 * when a session exists, proving the protected bearer round-trip end to end.
 */
export function useMe() {
  const { session } = useSession()

  return useQuery({
    queryKey: ['me'],
    queryFn: () => apiClient.get<Me>('/api/me'),
    enabled: Boolean(session),
  })
}

import { useMutation } from '@tanstack/react-query'
import { apiClient } from '../api/apiClient'
import type { BarcodeLookupResponse } from './types'

/**
 * On-demand barcode lookup via `GET /api/products/barcode/{barcode}`. Modelled
 * as a mutation because it fires on a user action (the "Look up" button), not on
 * render. The endpoint always returns HTTP 200 with an outcome (Found /
 * NotFound / AlreadyInCatalog), so a miss is a normal result, not a thrown error.
 */
export function useBarcodeLookup() {
  return useMutation({
    mutationFn: (barcode: string) =>
      apiClient.get<BarcodeLookupResponse>(
        `/api/products/barcode/${encodeURIComponent(barcode)}`,
      ),
  })
}

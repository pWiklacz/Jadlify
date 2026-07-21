import { useMutation, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../api/apiClient'
import { productsQueryKey } from './useProducts'
import type { CreateProductRequest, Product, UpdateProductRequest } from './types'

// Every mutation invalidates the `['products']` prefix, which covers the plain
// list, the paginated catalog (`['products','catalog',…]`), each product's
// detail (`['products','detail',id]`) and the recipe picker search — so a single
// invalidate keeps every product view in sync without hand-listing each key.

/** Creates a product, then refreshes the list. Returns the created product. */
export function useCreateProduct() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: CreateProductRequest) =>
      apiClient.post<Product>('/api/products', body),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: productsQueryKey })
    },
  })
}

/** Updates a product (204 No Content), then refreshes the list. */
export function useUpdateProduct() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: UpdateProductRequest }) =>
      apiClient.put<void>(`/api/products/${id}`, body),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: productsQueryKey })
    },
  })
}

/** Deletes a product (204 No Content), then refreshes the list. */
export function useDeleteProduct() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => apiClient.del(`/api/products/${id}`),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: productsQueryKey })
    },
  })
}

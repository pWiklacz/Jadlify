import { useEffect, useId, useRef } from 'react'
import { useDeleteProduct } from './useProductMutations'
import type { Product } from './types'

interface DeleteProductDialogProps {
  product: Product
  onClose: () => void
}

/**
 * Confirmation dialog guarding product deletion. Cancel dismisses; Delete fires
 * the mutation and closes on success. ESC also dismisses; focus lands on Cancel
 * so the safe action is the keyboard default.
 */
export function DeleteProductDialog({ product, onClose }: DeleteProductDialogProps) {
  const deleteProduct = useDeleteProduct()
  const cancelRef = useRef<HTMLButtonElement>(null)
  const baseId = useId()
  const titleId = `${baseId}-title`

  useEffect(() => {
    cancelRef.current?.focus()
  }, [])

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        event.preventDefault()
        onClose()
      }
    }
    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [onClose])

  function handleDelete() {
    deleteProduct.mutate(product.id, { onSuccess: onClose })
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) {
          onClose()
        }
      }}
    >
      <div
        role="alertdialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="w-full max-w-sm rounded-lg bg-white p-5 shadow-xl"
      >
        <h2 id={titleId} className="text-lg font-bold">
          Delete product
        </h2>
        <p className="mt-2 text-sm text-slate-600">
          Delete <span className="font-medium text-slate-900">{product.name}</span>? This cannot be
          undone.
        </p>

        {deleteProduct.isError && (
          <p role="alert" className="mt-2 text-sm text-red-600">
            Could not delete the product. Please try again.
          </p>
        )}

        <div className="mt-4 flex justify-end gap-2">
          <button
            ref={cancelRef}
            type="button"
            onClick={onClose}
            className="rounded-md px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={handleDelete}
            disabled={deleteProduct.isPending}
            className="rounded-md bg-red-600 px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
          >
            {deleteProduct.isPending ? 'Deleting…' : 'Delete'}
          </button>
        </div>
      </div>
    </div>
  )
}

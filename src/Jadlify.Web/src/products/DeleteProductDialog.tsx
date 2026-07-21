import { useRef } from 'react'
import { Button } from '../ui/Button'
import { Dialog } from '../ui/Dialog'
import { useDeleteProduct } from './useProductMutations'
import type { Product } from './types'

interface DeleteProductDialogProps {
  product: Product
  onClose: () => void
  /** Fires after a successful delete (before close), so the parent can toast/redirect. */
  onDeleted?: () => void
}

/**
 * Confirmation dialog guarding product deletion. Deletion is never blocked — the
 * catalog keeps recipe ingredients as snapshots, so removing a product cannot
 * break existing recipes or past shopping lists. Focus lands on Cancel so the
 * safe choice is the keyboard default.
 */
export function DeleteProductDialog({ product, onClose, onDeleted }: DeleteProductDialogProps) {
  const deleteProduct = useDeleteProduct()
  const cancelRef = useRef<HTMLButtonElement>(null)

  function handleDelete() {
    deleteProduct.mutate(product.id, {
      onSuccess: () => {
        onDeleted?.()
        onClose()
      },
    })
  }

  return (
    <Dialog
      open
      onClose={onClose}
      role="alertdialog"
      title="Usunąć produkt?"
      initialFocusRef={cancelRef}
      footer={
        <>
          <Button ref={cancelRef} variant="ghost" onClick={onClose} disabled={deleteProduct.isPending}>
            Anuluj
          </Button>
          <Button variant="danger" onClick={handleDelete} isLoading={deleteProduct.isPending}>
            Usuń produkt
          </Button>
        </>
      }
    >
      <p className="text-sm leading-relaxed text-espresso">
        <span className="font-semibold">„{product.name}”</span> zostanie trwale usunięty z Twojego
        katalogu. Tej operacji nie można cofnąć.
      </p>
      <p className="mt-2 text-[13px] leading-relaxed text-mocha">
        Przepisy zachowują wartości zapisane podczas dodawania składnika, a wcześniej wygenerowane
        listy zakupów pozostają bez zmian.
      </p>

      {deleteProduct.isError && (
        <p role="alert" className="mt-3 text-sm font-semibold text-danger">
          Nie udało się usunąć produktu. Spróbuj ponownie.
        </p>
      )}
    </Dialog>
  )
}

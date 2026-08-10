import { useRef } from 'react'
import type { ReactNode } from 'react'
import { ApiError } from '../api/client'
import { Button } from '../ui/Button'
import { Dialog } from '../ui/Dialog'
import { useDeleteRecipe } from './useRecipeMutations'

interface DeleteRecipeDialogProps {
  recipeId: string
  recipeName: string
  onClose: () => void
  onDeleted?: () => void
  /** Link to the planner, offered when the recipe is still used by an entry. */
  planLink?: ReactNode
}

/**
 * Confirmation dialog for deleting a recipe. A 409 means the recipe is still
 * referenced by a meal-plan entry: the dialog switches to an explanatory state
 * that points at the planner instead of offering a delete that cannot succeed.
 */
export function DeleteRecipeDialog({
  recipeId,
  recipeName,
  onClose,
  onDeleted,
  planLink,
}: DeleteRecipeDialogProps) {
  const deleteRecipe = useDeleteRecipe()
  const cancelRef = useRef<HTMLButtonElement>(null)
  const isConflict = deleteRecipe.error instanceof ApiError && deleteRecipe.error.status === 409

  function handleDelete() {
    deleteRecipe.mutate(recipeId, {
      onSuccess: () => {
        onDeleted?.()
        onClose()
      },
    })
  }

  if (isConflict) {
    return (
      <Dialog
        open
        role="alertdialog"
        onClose={onClose}
        title="Nie można jeszcze usunąć przepisu"
        initialFocusRef={cancelRef}
        footer={
          <Button ref={cancelRef} onClick={onClose}>
            Zamknij
          </Button>
        }
      >
        <p className="text-sm leading-relaxed text-mocha">
          Przepis <b className="text-espresso">{recipeName}</b> jest używany w zaplanowanych
          posiłkach. Usuń go najpierw z planu, aby móc usunąć przepis. Wygenerowane wcześniej listy
          zakupów są zapisem z chwili utworzenia i pozostaną bez zmian.
        </p>
        {planLink && <p className="mt-3 text-sm">{planLink}</p>}
      </Dialog>
    )
  }

  return (
    <Dialog
      open
      role="alertdialog"
      onClose={onClose}
      title="Usunąć przepis?"
      initialFocusRef={cancelRef}
      footer={
        <>
          <Button ref={cancelRef} variant="ghost" onClick={onClose} disabled={deleteRecipe.isPending}>
            Anuluj
          </Button>
          <Button variant="danger" onClick={handleDelete} isLoading={deleteRecipe.isPending}>
            Usuń przepis
          </Button>
        </>
      }
    >
      <p className="text-sm leading-relaxed text-mocha">
        Przepis <b className="text-espresso">{recipeName}</b> zostanie trwale usunięty. Tej operacji
        nie można cofnąć. Twoje produkty pozostaną bez zmian.
      </p>

      {deleteRecipe.isError && !isConflict && (
        <p role="alert" className="mt-3 text-[12.5px] font-semibold text-danger">
          Nie udało się usunąć przepisu. Spróbuj ponownie.
        </p>
      )}
    </Dialog>
  )
}

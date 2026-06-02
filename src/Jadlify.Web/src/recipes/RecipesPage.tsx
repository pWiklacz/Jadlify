import { useState } from 'react'
import { DeleteRecipeDialog } from './DeleteRecipeDialog'
import { RecipeFormModal } from './RecipeFormModal'
import { MacroPanel } from './RecipeMacroPreview'
import { useRecipes } from './useRecipes'
import type { Recipe } from './types'

type ModalState = { mode: 'create' } | { mode: 'edit'; recipe: Recipe } | null

/** Recipe list and builder entry point for the protected `/recipes` route. */
export function RecipesPage() {
  const { data: recipes, isLoading, isError } = useRecipes()
  const [modal, setModal] = useState<ModalState>(null)
  const [deleteTarget, setDeleteTarget] = useState<Recipe | null>(null)

  return (
    <section className="flex flex-col gap-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-bold">Recipes</h1>
        <button
          type="button"
          onClick={() => setModal({ mode: 'create' })}
          className="self-start rounded-md bg-slate-900 px-4 py-2 text-sm font-medium text-white sm:self-auto"
        >
          Create recipe
        </button>
      </div>

      {isLoading && <p className="text-slate-600">Loading recipes.</p>}

      {isError && (
        <p role="alert" className="text-red-600">
          Could not load recipes. Please refresh to try again.
        </p>
      )}

      {recipes && recipes.length === 0 && (
        <p className="text-slate-600">
          No recipes yet. Create your first recipe from products in your catalog.
        </p>
      )}

      {recipes && recipes.length > 0 && (
        <ul className="grid grid-cols-1 gap-3 lg:grid-cols-2">
          {recipes.map((recipe) => (
            <li
              key={recipe.id}
              className="flex flex-col gap-3 rounded-lg border border-slate-200 bg-white p-4"
            >
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <h2 className="truncate text-lg font-semibold">{recipe.name}</h2>
                  <p className="text-sm text-slate-500">
                    {recipe.portions} {recipe.portions === 1 ? 'portion' : 'portions'} -{' '}
                    {recipe.ingredients.length}{' '}
                    {recipe.ingredients.length === 1 ? 'ingredient' : 'ingredients'}
                  </p>
                </div>
              </div>

              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                <MacroPanel title="Whole recipe" macros={recipe.totalMacros} />
                <MacroPanel title="Per serving" macros={recipe.perServingMacros} />
              </div>

              <div className="flex flex-wrap gap-2">
                <button
                  type="button"
                  onClick={() => setModal({ mode: 'edit', recipe })}
                  className="rounded-md border border-slate-300 px-3 py-1 text-sm font-medium hover:bg-slate-100"
                >
                  Edit
                </button>
                <button
                  type="button"
                  onClick={() => setDeleteTarget(recipe)}
                  className="rounded-md border border-slate-300 px-3 py-1 text-sm font-medium text-red-600 hover:bg-red-50"
                >
                  Delete
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}

      {modal && (
        <RecipeFormModal
          key={modal.mode === 'edit' ? `edit-${modal.recipe.id}` : 'create'}
          mode={modal.mode}
          recipe={modal.mode === 'edit' ? modal.recipe : undefined}
          onClose={() => setModal(null)}
        />
      )}

      {deleteTarget && (
        <DeleteRecipeDialog recipe={deleteTarget} onClose={() => setDeleteTarget(null)} />
      )}
    </section>
  )
}

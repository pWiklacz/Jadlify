import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import type { Recipe } from '../recipes/types'
import { mealTypes, type AddMealPlanEntryRequest, type MealPlanEntry, type MealType } from './types'

type MealPlanEntryFormProps =
  | {
      mode: 'create'
      date: string
      recipes: Recipe[]
      /** Recipe preselected by an add-to-plan handoff; defaults to the first recipe. */
      initialRecipeId?: string
      isSubmitting: boolean
      onSubmit: (body: AddMealPlanEntryRequest) => Promise<void>
    }
  | {
      mode: 'edit'
      date: string
      entry: MealPlanEntry
      recipes: Recipe[]
      isSubmitting: boolean
      onCancel: () => void
      onSubmit: (body: { mealType: MealType; portions: number }) => Promise<void>
    }

export function MealPlanEntryForm(props: MealPlanEntryFormProps) {
  // This form is recipe-only; the product variant arrives with the Phase 7 planner rebuild.
  // Until then a product entry's null recipe/portions fall back to the empty create defaults.
  const initialRecipeId =
    props.mode === 'edit'
      ? (props.entry.recipeId ?? '')
      : (props.initialRecipeId ?? props.recipes[0]?.id ?? '')
  const initialMealType = props.mode === 'edit' ? props.entry.mealType : 'Breakfast'
  const initialPortions = props.mode === 'edit' ? String(props.entry.portions ?? 1) : '1'
  const [recipeId, setRecipeId] = useState(initialRecipeId)
  const [mealType, setMealType] = useState<MealType>(initialMealType)
  const [portions, setPortions] = useState(initialPortions)

  useEffect(() => {
    if (props.mode === 'edit') {
      setRecipeId(props.entry.recipeId ?? '')
      setMealType(props.entry.mealType)
      setPortions(String(props.entry.portions ?? 1))
      return
    }

    setRecipeId((current) => current || props.recipes[0]?.id || '')
  }, [props])

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const parsedPortions = Number(portions)

    if (props.mode === 'create') {
      await props.onSubmit({
        date: props.date,
        recipeId,
        mealType,
        portions: parsedPortions,
      })
      setMealType('Breakfast')
      setPortions('1')
      return
    }

    await props.onSubmit({ mealType, portions: parsedPortions })
  }

  const hasRecipes = props.recipes.length > 0
  const canSubmit = hasRecipes && recipeId && Number.isInteger(Number(portions)) && Number(portions) > 0

  if (props.mode === 'create' && !hasRecipes) {
    return (
      <div className="rounded-lg border border-slate-200 bg-white p-4 text-sm text-slate-600">
        <p>No recipes are available for meal planning.</p>
        <Link to="/recipes" className="font-medium text-slate-900 underline">
          Create a recipe
        </Link>
      </div>
    )
  }

  return (
    <form
      aria-label={props.mode === 'create' ? 'Add meal-plan entry' : 'Edit meal-plan entry'}
      onSubmit={submit}
      className="grid gap-3 rounded-lg border border-slate-200 bg-white p-4"
    >
      <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
        {props.mode === 'create' ? (
          <label className="flex flex-col gap-1 text-sm font-medium text-slate-700 md:col-span-1">
            Recipe
            <select
              value={recipeId}
              onChange={(event) => setRecipeId(event.currentTarget.value)}
              className="rounded-md border border-slate-300 px-3 py-2 text-base font-normal text-slate-900"
            >
              {props.recipes.map((recipe) => (
                <option key={recipe.id} value={recipe.id}>
                  {recipe.name}
                </option>
              ))}
            </select>
          </label>
        ) : (
          <div className="flex flex-col gap-1 text-sm text-slate-700 md:col-span-1">
            <span className="font-medium">Recipe</span>
            <span className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-base text-slate-900">
              {props.entry.recipeName}
            </span>
          </div>
        )}

        <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
          Meal type
          <select
            value={mealType}
            onChange={(event) => setMealType(event.currentTarget.value as MealType)}
            className="rounded-md border border-slate-300 px-3 py-2 text-base font-normal text-slate-900"
          >
            {mealTypes.map((type) => (
              <option key={type} value={type}>
                {type}
              </option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
          Portions
          <input
            type="number"
            min={1}
            step={1}
            value={portions}
            onChange={(event) => setPortions(event.currentTarget.value)}
            className="rounded-md border border-slate-300 px-3 py-2 text-base font-normal text-slate-900"
          />
        </label>
      </div>

      <div className="flex flex-wrap gap-2">
        <button
          type="submit"
          disabled={!canSubmit || props.isSubmitting}
          className="rounded-md bg-slate-900 px-4 py-2 text-sm font-medium text-white disabled:cursor-not-allowed disabled:bg-slate-300"
        >
          {props.isSubmitting
            ? 'Saving'
            : props.mode === 'create'
              ? 'Add entry'
              : 'Save entry'}
        </button>
        {props.mode === 'edit' && (
          <button
            type="button"
            onClick={props.onCancel}
            className="rounded-md border border-slate-300 px-4 py-2 text-sm font-medium hover:bg-slate-100"
          >
            Cancel
          </button>
        )}
      </div>
    </form>
  )
}

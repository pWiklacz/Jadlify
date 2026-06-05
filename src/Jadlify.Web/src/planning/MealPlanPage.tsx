import { useState } from 'react'
import { DailyMacroSummaryPanel } from './DailyMacroSummaryPanel'
import { MealPlanEntryForm } from './MealPlanEntryForm'
import { useDailyMacroSummary } from './useDailyMacroSummary'
import { useMealPlan } from './useMealPlan'
import { formatMacro } from '../recipes/macroMath'
import { useRecipes } from '../recipes/useRecipes'
import {
  useAddMealPlanEntry,
  useDeleteMealPlanEntry,
  useUpdateMealPlanEntry,
} from './useMealPlanMutations'
import {
  mealTypes,
  type AddMealPlanEntryRequest,
  type MacroSummary,
  type MealPlanEntry,
  type MealType,
} from './types'

export function MealPlanPage() {
  const [date, setDate] = useState(todayIsoDate())
  const [editing, setEditing] = useState<MealPlanEntry | null>(null)
  const { data: entries, isLoading, isError } = useMealPlan(date)
  const summaryQuery = useDailyMacroSummary(date)
  const recipesQuery = useRecipes()
  const addEntry = useAddMealPlanEntry()
  const updateEntry = useUpdateMealPlanEntry()
  const deleteEntry = useDeleteMealPlanEntry()

  const orderedEntries = [...(entries ?? [])].sort(compareEntries)
  const entryMacros = new Map(
    (summaryQuery.data?.entries ?? []).map((item) => [item.entryId, item.macros]),
  )
  const mutationFailed = addEntry.isError || updateEntry.isError || deleteEntry.isError

  async function addMealPlanEntry(body: AddMealPlanEntryRequest) {
    await addEntry.mutateAsync(body)
  }

  async function updateMealPlanEntry(entry: MealPlanEntry, body: { mealType: MealType; portions: number }) {
    await updateEntry.mutateAsync({
      id: entry.id,
      date: entry.date,
      body,
    })
    setEditing(null)
  }

  async function deleteMealPlanEntry(entry: MealPlanEntry) {
    await deleteEntry.mutateAsync({ id: entry.id, date: entry.date })
  }

  return (
    <section className="flex flex-col gap-5">
      <div className="flex flex-col gap-1">
        <h1 className="text-2xl font-bold">Meal plan</h1>
        <p className="max-w-2xl text-sm text-slate-600">
          Plan recipes for one day at a time. A daily goal is optional for this step.
        </p>
      </div>

      <label className="flex max-w-xs flex-col gap-1 text-sm font-medium text-slate-700">
        Date
        <input
          type="date"
          value={date}
          onChange={(event) => {
            setDate(event.currentTarget.value)
            setEditing(null)
          }}
          className="rounded-md border border-slate-300 px-3 py-2 text-base font-normal text-slate-900"
        />
      </label>

      {recipesQuery.isLoading && <p className="text-slate-600">Loading recipes.</p>}

      {recipesQuery.isError && (
        <p role="alert" className="text-red-600">
          Could not load recipes for meal planning. Please refresh to try again.
        </p>
      )}

      {recipesQuery.data && (
        <MealPlanEntryForm
          mode="create"
          date={date}
          recipes={recipesQuery.data}
          isSubmitting={addEntry.isPending}
          onSubmit={addMealPlanEntry}
        />
      )}

      {mutationFailed && (
        <p role="alert" className="text-sm font-medium text-red-600">
          The meal-plan change could not be saved. Please try again.
        </p>
      )}

      <DailyMacroSummaryPanel
        summary={summaryQuery.data}
        isLoading={summaryQuery.isLoading}
        isError={summaryQuery.isError}
      />

      <div className="flex flex-col gap-3">
        <h2 className="text-lg font-semibold">Entries for {date}</h2>

        {isLoading && <p className="text-slate-600">Loading meal plan.</p>}

        {isError && (
          <p role="alert" className="text-red-600">
            Could not load the meal plan. Please refresh to try again.
          </p>
        )}

        {entries && entries.length === 0 && (
          <p className="rounded-md border border-slate-200 bg-white p-4 text-sm text-slate-600">
            No entries for this date yet.
          </p>
        )}

        {orderedEntries.length > 0 && (
          <ul className="grid grid-cols-1 gap-3 lg:grid-cols-2">
            {orderedEntries.map((entry) => (
              <li
                key={entry.id}
                className="flex flex-col gap-3 rounded-lg border border-slate-200 bg-white p-4"
              >
                <div className="flex flex-col gap-1">
                  <p className="text-sm font-medium text-slate-500">{entry.mealType}</p>
                  <h3 className="text-lg font-semibold">{entry.recipeName}</h3>
                  <p className="text-sm text-slate-600">
                    {entry.portions} {entry.portions === 1 ? 'portion' : 'portions'}
                  </p>
                  <p className="text-sm font-medium text-slate-700">
                    {formatEntryMacros(entryMacros.get(entry.id))}
                  </p>
                </div>

                {editing?.id === entry.id ? (
                  <MealPlanEntryForm
                    mode="edit"
                    date={date}
                    entry={entry}
                    recipes={recipesQuery.data ?? []}
                    isSubmitting={updateEntry.isPending}
                    onCancel={() => setEditing(null)}
                    onSubmit={(body) => updateMealPlanEntry(entry, body)}
                  />
                ) : (
                  <div className="flex flex-wrap gap-2">
                    <button
                      type="button"
                      onClick={() => setEditing(entry)}
                      className="rounded-md border border-slate-300 px-3 py-1 text-sm font-medium hover:bg-slate-100"
                    >
                      Edit
                    </button>
                    <button
                      type="button"
                      onClick={() => deleteMealPlanEntry(entry)}
                      className="rounded-md border border-slate-300 px-3 py-1 text-sm font-medium text-red-600 hover:bg-red-50"
                    >
                      Delete
                    </button>
                  </div>
                )}
              </li>
            ))}
          </ul>
        )}
      </div>
    </section>
  )
}

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10)
}

function compareEntries(left: MealPlanEntry, right: MealPlanEntry): number {
  const mealTypeDifference = mealTypes.indexOf(left.mealType) - mealTypes.indexOf(right.mealType)
  if (mealTypeDifference !== 0) {
    return mealTypeDifference
  }

  return left.recipeName.localeCompare(right.recipeName)
}

function formatEntryMacros(macros: MacroSummary | undefined): string {
  if (!macros) {
    return '- kcal, - g protein, - g fat, - g carbs'
  }

  return [
    `${formatMacro(macros.calories)} kcal`,
    `${formatMacro(macros.protein)} g protein`,
    `${formatMacro(macros.fat)} g fat`,
    `${formatMacro(macros.carbohydrates)} g carbs`,
  ].join(', ')
}

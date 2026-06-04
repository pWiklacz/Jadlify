import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useDailyGoal, useUpsertDailyGoal } from './useDailyGoal'

type GoalFormState = {
  calories: string
  protein: string
  fat: string
  carbohydrates: string
}

const emptyGoalForm: GoalFormState = {
  calories: '',
  protein: '',
  fat: '',
  carbohydrates: '',
}

export function DailyGoalsPage() {
  const { data: goal, isLoading, isError } = useDailyGoal()
  const upsertGoal = useUpsertDailyGoal()
  const [form, setForm] = useState<GoalFormState>(emptyGoalForm)
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    if (goal) {
      setForm({
        calories: String(goal.calories),
        protein: String(goal.protein),
        fat: String(goal.fat),
        carbohydrates: String(goal.carbohydrates),
      })
    } else if (goal === null) {
      setForm(emptyGoalForm)
    }
  }, [goal])

  function updateField(field: keyof GoalFormState, value: string) {
    setSaved(false)
    setForm((current) => ({ ...current, [field]: value }))
  }

  async function saveGoal(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSaved(false)

    await upsertGoal.mutateAsync({
      calories: Number(form.calories),
      protein: Number(form.protein),
      fat: Number(form.fat),
      carbohydrates: Number(form.carbohydrates),
    })

    setSaved(true)
  }

  const isValid =
    Number(form.calories) > 0 &&
    Number(form.protein) >= 0 &&
    Number(form.fat) >= 0 &&
    Number(form.carbohydrates) >= 0 &&
    form.calories.trim() !== ''

  return (
    <section className="flex flex-col gap-5">
      <div className="flex flex-col gap-1">
        <h1 className="text-2xl font-bold">Daily goals</h1>
        <p className="max-w-2xl text-sm text-slate-600">
          Set one current daily macro target. Saving replaces the current goal.
        </p>
      </div>

      {isLoading && <p className="text-slate-600">Loading daily goal.</p>}

      {isError && (
        <p role="alert" className="text-red-600">
          Could not load your daily goal. Please refresh to try again.
        </p>
      )}

      {!isLoading && goal === null && (
        <p className="rounded-md border border-slate-200 bg-white p-4 text-sm text-slate-600">
          No daily goal is configured yet. Add your target macros to start using this page.
        </p>
      )}

      <form
        aria-label="Daily goal form"
        onSubmit={saveGoal}
        className="grid gap-4 rounded-lg border border-slate-200 bg-white p-4"
      >
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <NumberField
            label="Calories"
            value={form.calories}
            min={1}
            onChange={(value) => updateField('calories', value)}
          />
          <NumberField
            label="Protein (g)"
            value={form.protein}
            onChange={(value) => updateField('protein', value)}
          />
          <NumberField
            label="Fat (g)"
            value={form.fat}
            onChange={(value) => updateField('fat', value)}
          />
          <NumberField
            label="Carbohydrates (g)"
            value={form.carbohydrates}
            onChange={(value) => updateField('carbohydrates', value)}
          />
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <button
            type="submit"
            disabled={!isValid || upsertGoal.isPending}
            className="rounded-md bg-slate-900 px-4 py-2 text-sm font-medium text-white disabled:cursor-not-allowed disabled:bg-slate-300"
          >
            {upsertGoal.isPending ? 'Saving' : 'Save goal'}
          </button>

          {saved && (
            <p role="status" className="text-sm font-medium text-green-700">
              Saved.
            </p>
          )}

          {upsertGoal.isError && (
            <p role="alert" className="text-sm font-medium text-red-600">
              Could not save the goal. Check the values and try again.
            </p>
          )}
        </div>
      </form>
    </section>
  )
}

function NumberField({
  label,
  value,
  min = 0,
  onChange,
}: {
  label: string
  value: string
  min?: number
  onChange: (value: string) => void
}) {
  return (
    <label className="flex flex-col gap-1 text-sm font-medium text-slate-700">
      {label}
      <input
        type="number"
        min={min}
        step="0.1"
        value={value}
        onChange={(event) => onChange(event.currentTarget.value)}
        className="rounded-md border border-slate-300 px-3 py-2 text-base font-normal text-slate-900"
      />
    </label>
  )
}

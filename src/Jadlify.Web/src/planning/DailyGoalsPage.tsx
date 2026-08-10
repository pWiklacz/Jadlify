import { useEffect, useMemo, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { Button } from '../ui/Button'
import { Card } from '../ui/Card'
import { Dialog } from '../ui/Dialog'
import { Field, TextInput } from '../ui/Field'
import { MacroCompareRow } from '../ui/MacroCompareRow'
import { PageHeader } from '../ui/PageHeader'
import { QueryState } from '../ui/QueryState'
import { Toast } from '../ui/Toast'
import { formatKcal, formatMacro, parseDecimal } from '../ui/formatters'
import { useDailyGoal, useUpsertDailyGoal } from './useDailyGoal'
import { useDailyMacroSummary } from './useDailyMacroSummary'
import type { DailyGoal } from './types'

type GoalField = 'calories' | 'protein' | 'fat' | 'carbohydrates'

type GoalFormState = Record<GoalField, string>

const EMPTY_FORM: GoalFormState = { calories: '', protein: '', fat: '', carbohydrates: '' }

interface FieldMeta {
  key: GoalField
  label: string
  unit: string
  placeholder: string
  /** Macro colour used by the shared compare rows; kcal has its own accent. */
  kind: 'kcal' | 'protein' | 'fat' | 'carbs'
}

const MACRO_FIELDS: FieldMeta[] = [
  { key: 'protein', label: 'Białko', unit: 'g', placeholder: 'np. 150', kind: 'protein' },
  { key: 'fat', label: 'Tłuszcz', unit: 'g', placeholder: 'np. 80', kind: 'fat' },
  { key: 'carbohydrates', label: 'Węglowodany', unit: 'g', placeholder: 'np. 290', kind: 'carbs' },
]

/** Atwater factors: the macro→kcal conversion behind the consistency warning. */
const KCAL_PER_GRAM = { protein: 4, carbohydrates: 4, fat: 9 } as const

function toFormState(goal: DailyGoal): GoalFormState {
  return {
    calories: String(goal.calories),
    protein: String(goal.protein),
    fat: String(goal.fat),
    carbohydrates: String(goal.carbohydrates),
  }
}

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10)
}

/**
 * Daily goals screen (S-04). The goal stays a singleton GET/PUT with no id,
 * history, timestamp or delete — saving replaces the current target. Three
 * states share one route: a summary when a goal exists, an empty state when it
 * does not, and the edit form. The form previews the goal against the day's
 * planned meals and warns (never blocks) when the macros do not add up to the
 * calorie target.
 */
export function DailyGoalsPage() {
  const goalQuery = useDailyGoal()
  const upsertGoal = useUpsertDailyGoal()
  const [today] = useState(todayIsoDate)
  const summaryQuery = useDailyMacroSummary(today)

  const [editing, setEditing] = useState(false)
  const [form, setForm] = useState<GoalFormState>(EMPTY_FORM)
  const [showErrors, setShowErrors] = useState(false)
  const [dirty, setDirty] = useState(false)
  const [confirmDiscard, setConfirmDiscard] = useState(false)
  const [toast, setToast] = useState<string | null>(null)

  const caloriesRef = useRef<HTMLInputElement>(null)
  const fieldRefs = useRef<Partial<Record<GoalField, HTMLInputElement | null>>>({})

  const goal = goalQuery.data ?? null

  useEffect(() => {
    if (!toast) {
      return
    }
    const handle = setTimeout(() => setToast(null), 3200)
    return () => clearTimeout(handle)
  }, [toast])

  const parsed = useMemo(
    () => ({
      calories: parseDecimal(form.calories),
      protein: parseDecimal(form.protein),
      fat: parseDecimal(form.fat),
      carbohydrates: parseDecimal(form.carbohydrates),
    }),
    [form],
  )

  const errors: Partial<Record<GoalField, string>> = {}
  if (parsed.calories === null || parsed.calories <= 0) {
    errors.calories = 'Podaj dodatnią liczbę kalorii, np. 2600.'
  }
  for (const field of MACRO_FIELDS) {
    const value = parsed[field.key]
    if (value === null || value < 0) {
      errors[field.key] = `Podaj wartość 0 lub większą (${field.label.toLowerCase()}).`
    }
  }
  const hasErrors = Object.keys(errors).length > 0

  // Non-blocking consistency check: 4 kcal/g protein + 4 kcal/g carbs + 9 kcal/g fat.
  // Tolerance mirrors the mockup: the larger of 75 kcal or 5% of the calorie target.
  let mismatchText: string | null = null
  if (!hasErrors) {
    const computed =
      parsed.protein! * KCAL_PER_GRAM.protein +
      parsed.carbohydrates! * KCAL_PER_GRAM.carbohydrates +
      parsed.fat! * KCAL_PER_GRAM.fat
    const target = parsed.calories!
    if (Math.abs(computed - target) > Math.max(75, target * 0.05)) {
      mismatchText = `Podane makroskładniki odpowiadają około ${formatKcal(
        Math.round(computed / 10) * 10,
      )} kcal, a ustawiony cel to ${formatKcal(target)} kcal.`
    }
  }

  function startEditing(source: DailyGoal | null) {
    setForm(source ? toFormState(source) : EMPTY_FORM)
    setShowErrors(false)
    setDirty(false)
    setEditing(true)
  }

  function updateField(field: GoalField, value: string) {
    setDirty(true)
    setForm((current) => ({ ...current, [field]: value }))
  }

  function cancelEditing() {
    if (dirty) {
      setConfirmDiscard(true)
      return
    }
    setEditing(false)
  }

  function discardChanges() {
    setConfirmDiscard(false)
    setDirty(false)
    setEditing(false)
  }

  async function saveGoal(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (hasErrors) {
      setShowErrors(true)
      // Focus the first field in visual order that is actually invalid.
      const order: GoalField[] = ['calories', ...MACRO_FIELDS.map((field) => field.key)]
      const firstInvalid = order.find((field) => errors[field])
      if (firstInvalid) {
        fieldRefs.current[firstInvalid]?.focus()
      }
      return
    }

    await upsertGoal.mutateAsync({
      calories: parsed.calories!,
      protein: parsed.protein!,
      fat: parsed.fat!,
      carbohydrates: parsed.carbohydrates!,
    })

    setDirty(false)
    setEditing(false)
    setToast('Zapisano dzienne cele.')
  }

  const planTotal = summaryQuery.data?.total ?? null

  return (
    <section>
      <PageHeader
        title="Dzienne cele"
        subtitle="Jeden aktualny zestaw celów. Jadlify porównuje z nim wszystkie posiłki zaplanowane na wybrany dzień."
      />

      <QueryState
        isPending={goalQuery.isPending}
        isError={goalQuery.isError}
        onRetry={() => void goalQuery.refetch()}
        loadingLabel="Wczytujemy dzienne cele…"
        errorTitle="Nie udało się wczytać celów"
      >
        <div className="flex flex-col gap-4 design:flex-row design:items-start">
          <div className="flex flex-col gap-4 design:flex-[2]">
            {editing ? (
              <Card className="flex flex-col gap-4">
                <div>
                  <h2 className="font-serif text-2xl font-normal text-espresso">
                    {goal ? 'Edytuj dzienne cele' : 'Ustaw dzienne cele'}
                  </h2>
                  <p className="mt-1.5 text-[13.5px] leading-relaxed text-mocha">
                    Podaj cel kaloryczny i rozkład makroskładników. Wartości dotyczą jednego dnia.
                  </p>
                </div>

                <form
                  aria-label="Formularz dziennych celów"
                  className="flex flex-col gap-4"
                  onSubmit={saveGoal}
                  noValidate
                >
                  <Field
                    id="goal-calories"
                    label="Kalorie"
                    error={showErrors ? (errors.calories ?? null) : null}
                  >
                    <div className="flex items-center gap-2">
                      <TextInput
                        id="goal-calories"
                        ref={(node) => {
                          caloriesRef.current = node
                          fieldRefs.current.calories = node
                        }}
                        inputMode="decimal"
                        value={form.calories}
                        onChange={(event) => updateField('calories', event.target.value)}
                        placeholder="np. 2600"
                        invalid={showErrors && Boolean(errors.calories)}
                        aria-describedby={
                          showErrors && errors.calories ? 'goal-calories-error' : undefined
                        }
                        className="text-[22px] font-bold tabular-nums"
                      />
                      <span aria-hidden="true" className="text-sm font-bold text-mocha">
                        kcal
                      </span>
                    </div>
                  </Field>

                  <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                    {MACRO_FIELDS.map((field) => (
                      <Field
                        key={field.key}
                        id={`goal-${field.key}`}
                        label={`${field.label} (${field.unit})`}
                        error={showErrors ? (errors[field.key] ?? null) : null}
                      >
                        <TextInput
                          id={`goal-${field.key}`}
                          ref={(node) => {
                            fieldRefs.current[field.key] = node
                          }}
                          inputMode="decimal"
                          value={form[field.key]}
                          onChange={(event) => updateField(field.key, event.target.value)}
                          placeholder={field.placeholder}
                          invalid={showErrors && Boolean(errors[field.key])}
                          aria-describedby={
                            showErrors && errors[field.key]
                              ? `goal-${field.key}-error`
                              : undefined
                          }
                          className="font-bold tabular-nums"
                        />
                      </Field>
                    ))}
                  </div>

                  {mismatchText && (
                    <p
                      role="status"
                      className="rounded-panel border border-warning/50 bg-warning/10 px-3.5 py-3 text-[12.5px] leading-relaxed text-warning-ink"
                    >
                      <b>{mismatchText}</b> To tylko informacja — możesz zapisać cel z tymi
                      wartościami.
                    </p>
                  )}

                  <p className="text-[12.5px] leading-relaxed text-mocha">
                    Masz jeden aktualny zestaw celów. Zapisanie nowych wartości zastąpi obecne cele.
                  </p>

                  {upsertGoal.isError && (
                    <p role="alert" className="text-[12.5px] font-semibold text-danger">
                      Nie udało się zapisać celów. Sprawdź wartości i spróbuj ponownie.
                    </p>
                  )}

                  <div className="flex flex-wrap gap-2">
                    <Button
                      variant="ghost"
                      onClick={cancelEditing}
                      disabled={upsertGoal.isPending}
                    >
                      Anuluj
                    </Button>
                    <Button type="submit" isLoading={upsertGoal.isPending}>
                      {goal ? 'Zapisz zmiany' : 'Zapisz cele'}
                    </Button>
                  </div>
                </form>
              </Card>
            ) : goal ? (
              <Card className="flex flex-col gap-4">
                <h2 className="font-serif text-2xl font-normal text-espresso">
                  Aktualny cel dzienny
                </h2>

                <div className="rounded-panel border border-cream-border bg-cream-panel p-4">
                  <div className="flex items-baseline gap-2.5">
                    <span className="font-serif text-[44px] leading-none tabular-nums text-terracotta-strong">
                      {formatKcal(goal.calories)}
                    </span>
                    <span className="text-sm font-semibold text-mocha">kcal / dzień</span>
                  </div>
                </div>

                <dl className="grid grid-cols-1 gap-3 sm:grid-cols-3">
                  {MACRO_FIELDS.map((field) => (
                    <div
                      key={field.key}
                      className="rounded-panel border border-cream-border bg-cream-panel p-4"
                    >
                      <dt className="text-[11px] font-bold uppercase tracking-eyebrow text-mocha">
                        {field.label}
                      </dt>
                      <dd className="mt-2 flex items-baseline gap-1.5">
                        <span className="font-serif text-[30px] leading-none tabular-nums text-espresso">
                          {formatMacro(goal[field.key])}
                        </span>
                        <span className="text-[13px] font-semibold text-mocha">g</span>
                      </dd>
                    </div>
                  ))}
                </dl>

                <div>
                  <Button onClick={() => startEditing(goal)}>Edytuj cele</Button>
                </div>

                <p className="border-t border-dotted border-cream-line pt-3.5 text-[12.5px] leading-relaxed text-mocha">
                  Masz jeden aktualny zestaw celów. Zapisanie nowych wartości zastąpi obecne cele.
                </p>
              </Card>
            ) : (
              <Card className="flex flex-col gap-4">
                <h2 className="font-serif text-2xl font-normal text-espresso design:text-3xl">
                  Nie masz jeszcze dziennych celów
                </h2>
                <p className="max-w-prose text-[15px] leading-relaxed text-mocha">
                  Planowanie posiłków działa również bez celów, a Jadlify wciąż zlicza kalorie i
                  makro dla każdego dnia. Aby porównywać plan z założeniami, ustaw jeden dzienny cel.
                </p>
                <ul className="flex flex-col">
                  <FactRow>Plan posiłków i lista zakupów działają normalnie.</FactRow>
                  <FactRow>Sumy kalorii i makroskładników nadal są obliczane.</FactRow>
                  <FactRow muted>Porównanie z celem pojawi się dopiero po ustawieniu wartości.</FactRow>
                </ul>
                <div>
                  <Button onClick={() => startEditing(null)}>Ustaw dzienne cele</Button>
                </div>
              </Card>
            )}
          </div>

          <aside className="flex flex-col gap-4 design:flex-1">
            {editing && (
              <Card
                role="region"
                aria-label="Porównanie z planem dnia"
                className="flex flex-col gap-3"
              >
                <div className="text-[11px] font-bold uppercase tracking-eyebrow text-label">
                  Przy obecnym planie dnia
                </div>
                {summaryQuery.isError ? (
                  <p role="alert" className="text-[13px] text-mocha">
                    Nie udało się wczytać planu dnia. Cele możesz zapisać mimo to.
                  </p>
                ) : !planTotal ? (
                  <p className="text-[13px] leading-relaxed text-mocha">
                    Wczytujemy dzisiejszy plan…
                  </p>
                ) : (
                  <div className="flex flex-col gap-3.5">
                    <MacroCompareRow
                      label="Kalorie"
                      value={planTotal.calories}
                      goal={hasErrors ? null : parsed.calories}
                      kind="kcal"
                    />
                    {MACRO_FIELDS.map((field) => (
                      <MacroCompareRow
                        key={field.key}
                        label={field.label}
                        value={planTotal[field.key]}
                        goal={hasErrors ? null : parsed[field.key]}
                        kind={field.kind}
                      />
                    ))}
                  </div>
                )}
              </Card>
            )}

            <Card role="region" aria-label="Gdzie używamy celów" className="flex flex-col gap-2">
              <div className="text-[11px] font-bold uppercase tracking-eyebrow text-label">
                Gdzie używamy celów
              </div>
              <p className="text-[13px] leading-relaxed text-mocha">
                Jadlify porównuje z tym celem wszystkie posiłki zaplanowane na wybrany dzień:
              </p>
              <ul className="flex flex-col gap-1.5 text-[13px] text-label">
                <li>• Bilans dnia na stronie głównej</li>
                <li>• Podsumowanie makro dla wybranego dnia</li>
                <li>• Wartości pozostałe i przekroczone w planie posiłków</li>
              </ul>
              <Link
                to="/meal-plan"
                className="mt-1 text-[13px] font-semibold text-terracotta underline underline-offset-[3px]"
              >
                Przejdź do planu posiłków
              </Link>
            </Card>
          </aside>
        </div>
      </QueryState>

      {confirmDiscard && (
        <Dialog
          open
          role="alertdialog"
          onClose={() => setConfirmDiscard(false)}
          title="Masz niezapisane zmiany"
          description="Jeśli wyjdziesz teraz, wprowadzone wartości nie zostaną zapisane."
          footer={
            <>
              <Button variant="ghost" onClick={() => setConfirmDiscard(false)}>
                Wróć do edycji
              </Button>
              <Button variant="danger" onClick={discardChanges}>
                Odrzuć zmiany
              </Button>
            </>
          }
        >
          <p className="text-sm text-mocha">
            Obecne cele pozostaną bez zmian, dopóki nie zapiszesz nowych wartości.
          </p>
        </Dialog>
      )}

      {toast && <Toast tone="success" message={toast} />}
    </section>
  )
}

function FactRow({ children, muted = false }: { children: React.ReactNode; muted?: boolean }) {
  return (
    <li className="flex items-start gap-2.5 border-b border-dotted border-cream-line py-2.5 last:border-b-0">
      <span aria-hidden="true" className={muted ? 'text-mocha' : 'text-success'}>
        {muted ? 'ⓘ' : '✓'}
      </span>
      <span className={`text-sm leading-relaxed ${muted ? 'text-mocha' : 'text-label'}`}>
        {children}
      </span>
    </li>
  )
}

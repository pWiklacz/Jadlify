import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Button } from '../ui/Button'
import { Card } from '../ui/Card'
import { MacroCompareRow } from '../ui/MacroCompareRow'
import { PageHeader } from '../ui/PageHeader'
import { QueryState } from '../ui/QueryState'
import { StatusPill } from '../ui/StatusPill'
import { Toast, type ToastTone } from '../ui/Toast'
import { AddMealEntryDialog } from '../planning/AddMealEntryDialog'
import { MealGroup, type EntryWithMacros } from '../planning/MealGroup'
import { addDays, isIsoDate, todayIso } from '../planning/dateRange'
import { formatDayLong } from '../planning/plannerFormat'
import { ZERO_MACRO } from '../planning/plannerMacros'
import { useMealPlanRange } from '../planning/useMealPlanRange'
import {
  useAddMealPlanEntry,
  useDeleteMealPlanEntry,
  useUpdateMealPlanEntry,
} from '../planning/useMealPlanMutations'
import {
  mealTypes,
  type AddMealPlanEntryRequest,
  type MacroSummary,
  type MealPlanDay,
  type MealPlanEntry,
  type MealType,
} from '../planning/types'
import { useProductCatalog } from '../products/useProductCatalog'
import { useRecipeCatalog } from '../recipes/useRecipeCatalog'
import { useShoppingListIndex } from '../shopping/useShoppingLists'
import { ActiveShoppingCard } from '../dashboard/ActiveShoppingCard'
import { BalanceRing } from '../dashboard/BalanceRing'
import { DashboardDateNav } from '../dashboard/DashboardDateNav'
import { NextStepCard } from '../dashboard/NextStepCard'
import { resolveNextStep } from '../dashboard/nextStep'

function capitalize(value: string): string {
  return value.charAt(0).toUpperCase() + value.slice(1)
}

function emptyDay(date: string): MealPlanDay {
  return {
    date,
    entries: [],
    entryMacros: [],
    total: { calories: 0, protein: 0, fat: 0, carbohydrates: 0 },
    goal: null,
    remaining: null,
  }
}

/**
 * The interactive day dashboard (S-01), the post-login home.
 *
 * It is a *consumer* of the planner and shopping surfaces, not a third
 * implementation of them: the day comes from the same one-day range query the
 * planner uses, edits go through the same mutation hooks, meals render through the
 * same {@link MealGroup}, and the shopping slot reads the index summary the
 * shopping screen already fetches. That is what keeps the dashboard from drifting
 * out of agreement with the screens it summarises.
 *
 * Each panel degrades on its own — a failed shopping read must not blank the day —
 * and the single "Następny krok" is derived, never stored.
 */
export function LandingPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [dialogOpen, setDialogOpen] = useState<{ mealType?: MealType } | null>(null)
  const [toast, setToast] = useState<{ tone: ToastTone; message: string } | null>(null)

  const paramDate = searchParams.get('date')
  const date = paramDate && isIsoDate(paramDate) ? paramDate : todayIso()
  const today = todayIso()

  const range = useMealPlanRange(date, date)
  const shopping = useShoppingListIndex()
  const products = useProductCatalog({ search: '', category: 'all', sort: 'NameAsc' })
  const recipes = useRecipeCatalog({ search: '', sort: 'NameAsc' })

  const addEntry = useAddMealPlanEntry()
  const updateEntry = useUpdateMealPlanEntry()
  const deleteEntry = useDeleteMealPlanEntry()

  useEffect(() => {
    if (!toast) {
      return
    }
    const handle = setTimeout(() => setToast(null), 5000)
    return () => clearTimeout(handle)
  }, [toast])

  function setDate(next: string) {
    setSearchParams(
      (prev) => {
        const params = new URLSearchParams(prev)
        params.set('date', next)
        return params
      },
      { replace: true },
    )
  }

  const day = range.data?.days.find((candidate) => candidate.date === date) ?? emptyDay(date)
  const macrosByEntry = new Map<string, MacroSummary>(
    day.entryMacros.map((item) => [item.entryId, item.macros]),
  )
  const groups = mealTypes
    .map((mealType) => ({
      mealType,
      items: day.entries
        .filter((entry) => entry.mealType === mealType)
        .map<EntryWithMacros>((entry) => ({
          entry,
          macros: macrosByEntry.get(entry.id) ?? ZERO_MACRO,
        })),
    }))
    .filter((group) => group.items.length > 0)

  const busy = updateEntry.isPending || deleteEntry.isPending
  const activeList = shopping.data?.active ?? null

  function changeQuantity(entry: MealPlanEntry, value: number) {
    const body =
      entry.source === 'Recipe'
        ? { mealType: entry.mealType, portions: value }
        : { mealType: entry.mealType, grams: value }
    updateEntry.mutate(
      { id: entry.id, body },
      { onError: () => setToast({ tone: 'error', message: 'Nie udało się zapisać zmiany.' }) },
    )
  }

  function changeMealType(entry: MealPlanEntry, mealType: MealType) {
    const body =
      entry.source === 'Recipe'
        ? { mealType, portions: entry.portions ?? 1 }
        : { mealType, grams: entry.grams ?? 0 }
    updateEntry.mutate(
      { id: entry.id, body },
      {
        onError: () =>
          setToast({ tone: 'error', message: 'Nie udało się zmienić typu posiłku.' }),
      },
    )
  }

  function removeEntry(entry: MealPlanEntry) {
    deleteEntry.mutate(
      { id: entry.id },
      {
        onSuccess: () => setToast({ tone: 'info', message: 'Usunięto posiłek.' }),
        onError: () => setToast({ tone: 'error', message: 'Nie udało się usunąć posiłku.' }),
      },
    )
  }

  function submitAdd(body: AddMealPlanEntryRequest, targetDate: string) {
    addEntry.mutate(body, {
      onSuccess: () => {
        setDialogOpen(null)
        if (targetDate !== date) {
          setDate(targetDate)
        }
        setToast({ tone: 'success', message: 'Dodano posiłek.' })
      },
      onError: () => setToast({ tone: 'error', message: 'Nie udało się zapisać posiłku.' }),
    })
  }

  // Counts drive the contextual step, so it waits for them rather than briefly
  // telling a user with a full catalog to "add your first product".
  const countsReady = Boolean(products.data && recipes.data)
  const nextStep = countsReady
    ? resolveNextStep({
        productCount: products.data?.total ?? 0,
        recipeCount: recipes.data?.total ?? 0,
        hasGoal: day.goal !== null,
        plannedMealCount: day.entries.length,
        activeListId: activeList?.id ?? null,
      })
    : null

  return (
    <section>
      <PageHeader
        title="Strona główna"
        subtitle={capitalize(formatDayLong(date))}
        actions={
          <DashboardDateNav
            date={date}
            showBackToday={date !== today}
            onPrev={() => setDate(addDays(date, -1))}
            onNext={() => setDate(addDays(date, 1))}
            onDateChange={(next) => {
              if (isIsoDate(next)) {
                setDate(next)
              }
            }}
            onToday={() => setDate(today)}
          />
        }
      />

      <QueryState
        isPending={range.isPending}
        isError={range.isError}
        onRetry={() => void range.refetch()}
        loadingLabel="Wczytujemy Twój plan dnia…"
        errorTitle="Nie udało się pobrać planu dnia"
        errorDescription="Twoje zaplanowane posiłki są bezpieczne. Sprawdź połączenie i spróbuj ponownie."
      >
        <Card elevated className="flex flex-col gap-5">
          <div className="flex flex-wrap items-center gap-x-3.5 gap-y-2.5">
            <h2 className="font-serif text-2xl font-normal text-espresso design:text-[28px]">
              {capitalize(formatDayLong(date))}
            </h2>
            {date === today && <StatusPill label="DZIŚ" tone="info" />}
            <span className="flex-1" />
            <Button size="sm" onClick={() => setDialogOpen({})} disabled={busy}>
              + Dodaj posiłek
            </Button>
          </div>

          <div className="flex flex-wrap items-start gap-6">
            <div className="flex min-w-0 flex-[2_1_420px] flex-col gap-3.5">
              <h3 className="text-[11px] font-bold uppercase tracking-eyebrow text-mocha">
                Posiłki w planie
              </h3>

              {groups.length > 0 ? (
                groups.map((group) => (
                  <MealGroup
                    key={group.mealType}
                    mealType={group.mealType}
                    items={group.items}
                    busy={busy}
                    onQuantityChange={changeQuantity}
                    onMealTypeChange={changeMealType}
                    onDelete={removeEntry}
                    onAdd={(mealType) => setDialogOpen({ mealType })}
                  />
                ))
              ) : (
                <div className="rounded-panel border-[1.5px] border-dashed border-cream-line px-6 py-8 text-center">
                  <p className="font-serif text-xl text-espresso">
                    Zaplanuj swój pierwszy dzień
                  </p>
                  <p className="mx-auto mb-4 mt-1.5 max-w-sm text-[13.5px] leading-relaxed text-mocha">
                    Dodaj przepis w porcjach albo pojedynczy produkt w gramach, a policzymy
                    kalorie i makro tego dnia.
                  </p>
                  <Button onClick={() => setDialogOpen({})} disabled={busy}>
                    Dodaj posiłek
                  </Button>
                </div>
              )}
            </div>

            <aside
              aria-label="Bilans dnia"
              className="flex min-w-0 flex-[1_1_300px] flex-col gap-3.5"
            >
              <div className="rounded-panel border border-cream-border bg-cream-panel px-[18px] py-4">
                <h3 className="sr-only">Bilans dnia</h3>
                <BalanceRing calories={day.total.calories} goalCalories={day.goal?.calories ?? null} />
                <div className="mt-4 flex flex-col gap-3.5">
                  <MacroCompareRow
                    label="Białko"
                    value={day.total.protein}
                    goal={day.goal?.protein ?? null}
                    kind="protein"
                  />
                  <MacroCompareRow
                    label="Węglowodany"
                    value={day.total.carbohydrates}
                    goal={day.goal?.carbohydrates ?? null}
                    kind="carbs"
                  />
                  <MacroCompareRow
                    label="Tłuszcz"
                    value={day.total.fat}
                    goal={day.goal?.fat ?? null}
                    kind="fat"
                  />
                </div>
              </div>

              {nextStep && (
                <NextStepCard step={nextStep} onAction={() => setDialogOpen({})} />
              )}

              {shopping.isError ? (
                <section
                  aria-label="Lista zakupów"
                  className="rounded-panel border border-cream-border bg-cream-panel px-[18px] py-4"
                >
                  <h3 className="font-serif text-xl font-normal text-espresso">Lista zakupów</h3>
                  <p role="alert" className="mt-2 text-[13px] text-danger">
                    Nie udało się pobrać list zakupów. Reszta panelu działa normalnie.
                  </p>
                </section>
              ) : (
                !shopping.isPending && <ActiveShoppingCard list={activeList} />
              )}
            </aside>
          </div>
        </Card>
      </QueryState>

      {dialogOpen && (
        <AddMealEntryDialog
          date={date}
          initialMealType={dialogOpen.mealType}
          isSubmitting={addEntry.isPending}
          isError={addEntry.isError}
          onClose={() => setDialogOpen(null)}
          onSubmit={submitAdd}
        />
      )}

      {toast && <Toast tone={toast.tone} message={toast.message} />}
    </section>
  )
}

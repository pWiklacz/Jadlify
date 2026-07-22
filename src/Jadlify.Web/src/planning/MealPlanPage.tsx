import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { PageHeader } from '../ui/PageHeader'
import { QueryState } from '../ui/QueryState'
import { Toast, type ToastTone } from '../ui/Toast'
import { readAddToPlanParams, stripAddToPlanParams } from '../recipes/addToPlan'
import { AddMealEntryDialog } from './AddMealEntryDialog'
import { CopyDayDialog } from './CopyDayDialog'
import { CopyMealEntryDialog } from './CopyMealEntryDialog'
import { DayCard } from './DayCard'
import { DayPanel } from './DayPanel'
import { MonthGrid } from './MonthGrid'
import { MoveMealEntryDialog } from './MoveMealEntryDialog'
import { PlannerRangeNav } from './PlannerRangeNav'
import { WeekSummary } from './WeekSummary'
import { isIsoDate, rangeFor, shiftDate, todayIso, type PlannerView } from './dateRange'
import { formatRangeLabel } from './plannerFormat'
import { useMealPlanRange } from './useMealPlanRange'
import {
  useAddMealPlanEntry,
  useCopyMealPlanDay,
  useCopyMealPlanEntry,
  useDeleteMealPlanEntry,
  useMoveMealPlanEntry,
  useUpdateMealPlanEntry,
} from './useMealPlanMutations'
import type {
  AddMealPlanEntryRequest,
  CopyMealPlanDayRequest,
  CopyMealPlanEntryRequest,
  MealPlanDay,
  MealPlanEntry,
  MoveMealPlanEntryRequest,
  MealType,
} from './types'

type DialogState =
  | { kind: 'add'; date: string; mealType?: MealType; initialRecipeId?: string }
  | { kind: 'move'; entry: MealPlanEntry }
  | { kind: 'copy'; entry: MealPlanEntry }
  | { kind: 'copyDay'; date: string; entryCount: number }
  | null

interface ToastState {
  tone: ToastTone
  message: string
  action?: { label: string; onClick: () => void }
}

function normalizeView(value: string | null): PlannerView {
  return value === 'day' || value === 'month' ? value : 'week'
}

function normalizeDate(value: string | null): string {
  return value && isIsoDate(value) ? value : todayIso()
}

/** An empty stand-in when the selected day is not present in the range (defensive). */
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
 * The planner surface (S-05). The day / week / month views and every batch
 * operation read through one range request keyed by `[view, date]` in the URL, so
 * the view is shareable and survives reloads. Selecting a day inside the current
 * window re-highlights without refetching; stepping the period or switching the
 * view issues exactly one new range request. Every mutation invalidates the shared
 * `meal-plan` prefix, so the whole surface stays consistent from one place.
 */
export function MealPlanPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()

  const view = normalizeView(searchParams.get('view'))
  const date = normalizeDate(searchParams.get('date'))
  const { from, to } = rangeFor(view, date)

  const [dialog, setDialog] = useState<DialogState>(null)
  const [handoffReturnTo, setHandoffReturnTo] = useState<string | null>(null)
  const [toast, setToast] = useState<ToastState | null>(null)

  const range = useMealPlanRange(from, to)
  const addEntry = useAddMealPlanEntry()
  const updateEntry = useUpdateMealPlanEntry()
  const deleteEntry = useDeleteMealPlanEntry()
  const moveEntry = useMoveMealPlanEntry()
  const copyEntry = useCopyMealPlanEntry()
  const copyDay = useCopyMealPlanDay()

  const today = todayIso()
  const showBackToday = today < from || today > to

  // Consume an add-to-plan handoff from `/recipes/:id`: open the add dialog with
  // the recipe pre-selected on the requested day, then strip the params so a reload
  // does not re-open it. The URL — not localStorage — carries the intent.
  useEffect(() => {
    const handoff = readAddToPlanParams(searchParams)
    if (!handoff) {
      return
    }
    const handoffDate = handoff.date ?? date
    setDialog({ kind: 'add', date: handoffDate, initialRecipeId: handoff.recipeId })
    setHandoffReturnTo(handoff.returnTo ?? null)

    const next = stripAddToPlanParams(searchParams)
    if (handoff.date) {
      next.set('date', handoff.date)
    }
    setSearchParams(next, { replace: true })
    // `date` is intentionally omitted: the handoff carries its own target day.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchParams, setSearchParams])

  useEffect(() => {
    if (!toast) {
      return
    }
    const handle = setTimeout(() => setToast(null), 5000)
    return () => clearTimeout(handle)
  }, [toast])

  function patchUrl(next: { view?: PlannerView; date?: string }) {
    setSearchParams(
      (prev) => {
        const params = new URLSearchParams(prev)
        params.set('view', next.view ?? view)
        params.set('date', next.date ?? date)
        return params
      },
      { replace: true },
    )
  }

  const days = range.data?.days ?? []
  const selectedDay = days.find((day) => day.date === date) ?? emptyDay(date)
  const rangeLabel = formatRangeLabel(view, date)
  const entriesBusy = updateEntry.isPending || deleteEntry.isPending

  function openAdd(mealType?: MealType, targetDate: string = date) {
    setHandoffReturnTo(null)
    setDialog({ kind: 'add', date: targetDate, mealType })
  }

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
      { onError: () => setToast({ tone: 'error', message: 'Nie udało się zmienić typu posiłku.' }) },
    )
  }

  function undoDelete(entry: MealPlanEntry) {
    const body: AddMealPlanEntryRequest =
      entry.source === 'Recipe'
        ? {
            date: entry.date,
            mealType: entry.mealType,
            recipeId: entry.recipeId ?? '',
            portions: entry.portions ?? 1,
          }
        : {
            date: entry.date,
            mealType: entry.mealType,
            productId: entry.productId ?? '',
            grams: entry.grams ?? 0,
          }
    addEntry.mutate(body, {
      onSuccess: () => setToast({ tone: 'success', message: 'Przywrócono posiłek.' }),
      onError: () =>
        setToast({ tone: 'error', message: 'Nie udało się przywrócić posiłku — spróbuj ponownie.' }),
    })
  }

  function deleteWithUndo(entry: MealPlanEntry) {
    deleteEntry.mutate(
      { id: entry.id },
      {
        onSuccess: () =>
          setToast({
            tone: 'info',
            message: 'Usunięto posiłek.',
            action: { label: 'Cofnij', onClick: () => undoDelete(entry) },
          }),
        onError: () => setToast({ tone: 'error', message: 'Nie udało się usunąć posiłku.' }),
      },
    )
  }

  function submitAdd(body: AddMealPlanEntryRequest, targetDate: string) {
    addEntry.mutate(body, {
      onSuccess: () => {
        const returnTo = handoffReturnTo
        setDialog(null)
        if (targetDate !== date) {
          patchUrl({ date: targetDate })
        }
        setToast({
          tone: 'success',
          message: 'Dodano posiłek.',
          action: returnTo
            ? { label: 'Wróć do przepisu', onClick: () => navigate(returnTo) }
            : undefined,
        })
        setHandoffReturnTo(null)
      },
    })
  }

  function submitMove(entry: MealPlanEntry, body: MoveMealPlanEntryRequest) {
    moveEntry.mutate(
      { id: entry.id, body },
      {
        onSuccess: () => {
          setDialog(null)
          setToast({ tone: 'success', message: 'Przeniesiono posiłek.' })
        },
      },
    )
  }

  function submitCopy(entry: MealPlanEntry, body: CopyMealPlanEntryRequest) {
    copyEntry.mutate(
      { id: entry.id, body },
      {
        onSuccess: (result) => {
          setDialog(null)
          setToast({
            tone: 'success',
            message: `Skopiowano posiłek do ${result.entries.length} dni.`,
          })
        },
      },
    )
  }

  function submitCopyDay(sourceDate: string, body: CopyMealPlanDayRequest) {
    copyDay.mutate(
      { date: sourceDate, body },
      {
        onSuccess: (result) => {
          setDialog(null)
          setToast({
            tone: 'success',
            message: `Skopiowano dzień — dodano ${result.entries.length} posiłków.`,
          })
        },
      },
    )
  }

  const backgroundRefreshing = range.isFetching && !range.isPending

  return (
    <section>
      <PageHeader title="Plan posiłków" subtitle={rangeLabel} />

      <div className="mb-5">
        <PlannerRangeNav
          view={view}
          date={date}
          showBackToday={showBackToday}
          onViewChange={(next) => patchUrl({ view: next })}
          onPrev={() => patchUrl({ date: shiftDate(view, date, -1) })}
          onNext={() => patchUrl({ date: shiftDate(view, date, 1) })}
          onDateChange={(next) => {
            if (isIsoDate(next)) {
              patchUrl({ date: next })
            }
          }}
          onToday={() => patchUrl({ date: today })}
          onAdd={() => openAdd()}
        />
      </div>

      {backgroundRefreshing && (
        <div
          role="status"
          className="mb-4 flex items-center gap-2.5 rounded-panel border border-parchment/12 bg-parchment/[0.06] px-3.5 py-2.5 text-[13px] text-parchment/65"
        >
          <span
            aria-hidden="true"
            className="h-3.5 w-3.5 animate-spin-slow rounded-full border-2 border-parchment/25 border-t-terracotta"
          />
          Odświeżamy plan w tle — Twoje zaplanowane posiłki pozostają widoczne.
        </div>
      )}

      <QueryState
        isPending={range.isPending}
        isError={range.isError}
        onRetry={() => void range.refetch()}
        loadingLabel="Wczytujemy Twój plan posiłków…"
        errorTitle="Nie udało się pobrać planu"
        errorDescription="Wystąpił problem z połączeniem. Twoje przepisy i wcześniejsze plany są bezpieczne — spróbuj ponownie."
      >
        {view === 'week' && (
          <>
            <WeekSummary days={days} />
            <section aria-label="Przegląd tygodnia" className="mb-5 overflow-x-auto pb-1">
              <div className="grid min-w-[620px] grid-cols-7 gap-2.5 design:min-w-0">
                {days.map((day) => (
                  <DayCard
                    key={day.date}
                    day={day}
                    isSelected={day.date === date}
                    isToday={day.date === today}
                    onSelect={() => patchUrl({ date: day.date })}
                    onAddFirst={() => openAdd(undefined, day.date)}
                  />
                ))}
              </div>
            </section>
          </>
        )}

        {view === 'month' && (
          <MonthGrid
            days={days}
            selectedDate={date}
            todayDate={today}
            focusMonth={date}
            onSelect={(next) => patchUrl({ date: next })}
          />
        )}

        <DayPanel
          day={selectedDay}
          isToday={selectedDay.date === today}
          busy={entriesBusy}
          onQuantityChange={changeQuantity}
          onMealTypeChange={changeMealType}
          onMove={(entry) => setDialog({ kind: 'move', entry })}
          onCopy={(entry) => setDialog({ kind: 'copy', entry })}
          onDelete={deleteWithUndo}
          onAdd={(mealType) => openAdd(mealType)}
          onCopyDay={() =>
            setDialog({ kind: 'copyDay', date: selectedDay.date, entryCount: selectedDay.entries.length })
          }
        />
      </QueryState>

      {/* Mobile floating action button — the desktop add button lives in the nav. */}
      <div className="pointer-events-none fixed inset-x-4 bottom-4 z-30 flex justify-center design:hidden">
        <button
          type="button"
          onClick={() => openAdd()}
          className="pointer-events-auto inline-flex min-h-[52px] items-center gap-2 rounded-pill bg-terracotta-strong px-7 text-[15px] font-semibold text-paper shadow-fab transition-colors hover:bg-terracotta-hover"
        >
          + Dodaj posiłek
        </button>
      </div>

      {dialog?.kind === 'add' && (
        <AddMealEntryDialog
          key={dialog.initialRecipeId ?? `add-${dialog.date}`}
          date={dialog.date}
          initialMealType={dialog.mealType}
          initialRecipeId={dialog.initialRecipeId}
          isSubmitting={addEntry.isPending}
          isError={addEntry.isError}
          onClose={() => {
            setDialog(null)
            setHandoffReturnTo(null)
          }}
          onSubmit={submitAdd}
        />
      )}

      {dialog?.kind === 'move' && (
        <MoveMealEntryDialog
          entry={dialog.entry}
          isSubmitting={moveEntry.isPending}
          isError={moveEntry.isError}
          onClose={() => setDialog(null)}
          onSubmit={(body) => submitMove(dialog.entry, body)}
        />
      )}

      {dialog?.kind === 'copy' && (
        <CopyMealEntryDialog
          entry={dialog.entry}
          isSubmitting={copyEntry.isPending}
          isError={copyEntry.isError}
          onClose={() => setDialog(null)}
          onSubmit={(body) => submitCopy(dialog.entry, body)}
        />
      )}

      {dialog?.kind === 'copyDay' && (
        <CopyDayDialog
          date={dialog.date}
          entryCount={dialog.entryCount}
          isSubmitting={copyDay.isPending}
          isError={copyDay.isError}
          onClose={() => setDialog(null)}
          onSubmit={(body) => submitCopyDay(dialog.date, body)}
        />
      )}

      {toast && (
        <Toast
          tone={toast.tone}
          message={toast.message}
          action={
            toast.action
              ? {
                  label: toast.action.label,
                  onClick: () => {
                    const run = toast.action?.onClick
                    setToast(null)
                    run?.()
                  },
                }
              : undefined
          }
        />
      )}
    </section>
  )
}

import { useMemo, useRef, useState } from 'react'
import { Button } from '../ui/Button'
import { Dialog } from '../ui/Dialog'
import { TextField } from '../ui/Field'
import { formatCount } from '../ui/formatters'
import { addDays, todayIso, weekdayIndex } from '../planning/dateRange'
import { WEEKDAY_SHORT, formatDayShort } from '../planning/plannerFormat'
import { useMealPlanRange } from '../planning/useMealPlanRange'
import { defaultListName, sourceDaysLabel } from './shoppingFormat'

/** The window the picker offers, matching the API's 42-day span bound for a list's source days. */
const WINDOW_DAYS = 42

interface CreateShoppingListDialogProps {
  isSubmitting: boolean
  onClose: () => void
  onSubmit: (name: string, days: string[]) => void
}

/**
 * The two-step list creator: pick the days, then review and generate.
 *
 * Days are an arbitrary selection — not a fixed "this week" — so meal prep across
 * a long weekend or a single shopping trip both work. The candidate window is the
 * next 42 days, which is also the API's span bound, so an in-range selection can
 * never be rejected for spanning too far. Each candidate shows how many meals it
 * holds, read from the same planner range query the planner itself uses.
 */
export function CreateShoppingListDialog({
  isSubmitting,
  onClose,
  onSubmit,
}: CreateShoppingListDialogProps) {
  const [step, setStep] = useState<1 | 2>(1)
  const [selected, setSelected] = useState<ReadonlySet<string>>(new Set())
  const [name, setName] = useState('')
  const [nameEdited, setNameEdited] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const nameRef = useRef<HTMLInputElement>(null)

  const from = todayIso()
  const to = addDays(from, WINDOW_DAYS - 1)
  const range = useMealPlanRange(from, to)

  const days = useMemo(
    () =>
      (range.data?.days ?? []).map((day) => ({
        date: day.date,
        entryCount: day.entries.length,
      })),
    [range.data],
  )

  const selectedDays = useMemo(() => [...selected].sort(), [selected])
  const suggestedName = defaultListName(selectedDays)
  const effectiveName = nameEdited ? name : suggestedName

  function toggle(date: string) {
    setError(null)
    setSelected((prev) => {
      const next = new Set(prev)
      if (next.has(date)) {
        next.delete(date)
      } else {
        next.add(date)
      }
      return next
    })
  }

  function selectNextSeven() {
    setError(null)
    setSelected(new Set(Array.from({ length: 7 }, (_, index) => addDays(from, index))))
  }

  function selectPlanned() {
    setError(null)
    setSelected(new Set(days.filter((day) => day.entryCount > 0).map((day) => day.date)))
  }

  function goToReview() {
    if (selected.size === 0) {
      setError('Zaznacz co najmniej jeden dzień, aby wygenerować listę.')
      return
    }
    setError(null)
    setStep(2)
  }

  function submit() {
    const trimmed = effectiveName.trim()
    if (trimmed === '') {
      setError('Podaj nazwę listy.')
      nameRef.current?.focus()
      return
    }
    onSubmit(trimmed, selectedDays)
  }

  const plannedCount = days.filter((day) => day.entryCount > 0).length
  const selectedMealCount = days
    .filter((day) => selected.has(day.date))
    .reduce((sum, day) => sum + day.entryCount, 0)
  const plannedSelectedCount = days.filter(
    (day) => selected.has(day.date) && day.entryCount > 0,
  ).length

  /** The review step's at-a-glance recap of what the generated list will cover. */
  const summaryChips = [
    sourceDaysLabel(selectedDays),
    `${formatCount(plannedSelectedCount, 'dzień', 'dni', 'dni')} z posiłkami`,
    formatCount(selectedMealCount, 'posiłek', 'posiłki', 'posiłków'),
  ]

  return (
    <Dialog
      open
      onClose={onClose}
      size="lg"
      title="Nowa lista zakupów"
      description={step === 1 ? '1 · Wybierz dni' : '2 · Sprawdź i wygeneruj'}
      footer={
        step === 1 ? (
          <>
            <Button variant="ghost" onClick={onClose}>
              Anuluj
            </Button>
            <Button onClick={goToReview}>Sprawdź i wygeneruj</Button>
          </>
        ) : (
          <>
            <Button variant="ghost" onClick={() => setStep(1)} disabled={isSubmitting}>
              Wróć do wyboru dni
            </Button>
            <Button onClick={submit} isLoading={isSubmitting}>
              Wygeneruj listę zakupów
            </Button>
          </>
        )
      }
    >
      {step === 1 ? (
        <div className="flex flex-col gap-4">
          <div className="flex flex-wrap gap-2">
            <Button variant="ghost" size="sm" onClick={selectNextSeven}>
              Najbliższe 7 dni
            </Button>
            <Button
              variant="ghost"
              size="sm"
              onClick={selectPlanned}
              disabled={plannedCount === 0}
            >
              Wszystkie dni z posiłkami
            </Button>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setSelected(new Set())}
              disabled={selected.size === 0}
            >
              Wyczyść wybór
            </Button>
          </div>

          {range.isPending && (
            <p role="status" className="text-[13px] text-mocha">
              Wczytujemy Twój plan posiłków…
            </p>
          )}

          {range.isError && (
            <p role="alert" className="text-[13px] text-danger">
              Nie udało się wczytać planu. Możesz nadal wybrać dni — liczba posiłków będzie
              nieznana.
            </p>
          )}

          {error && (
            <p role="alert" className="text-[13px] text-danger">
              {error}
            </p>
          )}

          {/*
            A calendar-like grid rather than a list: the choice is "which of the next
            few weeks' days do I shop for", and seeing the days side by side with
            their meal counts is what makes that answerable at a glance.
          */}
          <div
            role="group"
            aria-label="Dni listy zakupów"
            className="grid grid-cols-[repeat(auto-fill,minmax(146px,1fr))] gap-2.5"
          >
            {(days.length > 0 ? days : fallbackDays(from)).map((day) => {
              const checked = selected.has(day.date)
              const planned = day.entryCount > 0
              return (
                <button
                  key={day.date}
                  type="button"
                  aria-pressed={checked}
                  onClick={() => toggle(day.date)}
                  className={[
                    'flex min-h-20 flex-col items-start gap-[3px] rounded-panel border-[1.5px] p-3 text-left transition-colors',
                    checked
                      ? 'border-terracotta-strong bg-terracotta-strong/[0.08]'
                      : planned
                        ? 'border-cream-border bg-cream-panel hover:border-terracotta/50'
                        : 'border-dashed border-cream-line bg-transparent hover:border-terracotta/50',
                  ].join(' ')}
                >
                  <span className="flex w-full items-center gap-1.5">
                    <span
                      className={[
                        'text-[10px] font-bold uppercase tracking-[0.14em]',
                        checked ? 'text-terracotta-strong' : 'text-mocha',
                      ].join(' ')}
                    >
                      {day.date === from ? 'Dziś · ' : ''}
                      {WEEKDAY_SHORT[weekdayIndex(day.date)]}
                    </span>
                    <span className="flex-1" />
                    <span
                      aria-hidden="true"
                      className={[
                        'flex h-[19px] w-[19px] items-center justify-center rounded-full border-[1.5px] text-[11px] font-extrabold leading-none text-paper',
                        checked
                          ? 'border-terracotta-strong bg-terracotta-strong'
                          : 'border-cream-mark',
                      ].join(' ')}
                    >
                      {checked ? '✓' : ''}
                    </span>
                  </span>
                  <span
                    className={[
                      'text-[15.5px] font-bold',
                      planned || checked ? 'text-espresso' : 'text-muted',
                    ].join(' ')}
                  >
                    {formatDayShort(day.date)}
                  </span>
                  <span
                    className={[
                      'text-xs tabular-nums',
                      planned
                        ? checked
                          ? 'font-semibold text-terracotta-strong'
                          : 'font-semibold text-label'
                        : 'font-medium text-faint',
                    ].join(' ')}
                  >
                    {planned
                      ? formatCount(day.entryCount, 'posiłek', 'posiłki', 'posiłków')
                      : 'brak posiłków'}
                  </span>
                </button>
              )
            })}
          </div>

          <p className="border-t border-dotted border-cream-line pt-3.5 text-sm font-bold tabular-nums text-espresso">
            Wybrano: {formatCount(selected.size, 'dzień', 'dni', 'dni')} ·{' '}
            {formatCount(selectedMealCount, 'posiłek', 'posiłki', 'posiłków')}
          </p>
        </div>
      ) : (
        <div className="flex flex-col gap-4">
          <TextField
            ref={nameRef}
            id="shopping-list-name"
            label="Nazwa listy"
            value={effectiveName}
            onChange={(event) => {
              setNameEdited(true)
              setName(event.currentTarget.value)
              setError(null)
            }}
            error={error}
          />

          <ul aria-label="Podsumowanie wyboru" className="flex flex-wrap gap-[7px]">
            {summaryChips.map((chip) => (
              <li
                key={chip}
                className="rounded-pill border border-cream-border bg-cream-panel px-3.5 py-[7px] text-[13px] font-semibold tabular-nums text-label"
              >
                {chip}
              </li>
            ))}
          </ul>

          <div className="rounded-panel border border-cream-border bg-cream-panel px-4 py-3.5">
            <h3 className="mb-2.5 text-[11px] font-bold uppercase tracking-eyebrow text-label">
              Wybrane dni
            </h3>
            <ul className="flex flex-col gap-1">
              {selectedDays.map((date) => {
                const day = days.find((candidate) => candidate.date === date)
                return (
                  <li
                    key={date}
                    className="flex items-baseline gap-2 text-[13.5px] text-espresso"
                  >
                    <span>
                      {WEEKDAY_SHORT[weekdayIndex(date)]} · {formatDayShort(date)}
                    </span>
                    <span
                      aria-hidden="true"
                      className="min-w-3 flex-1 self-center border-b-2 border-dotted border-cream-line"
                    />
                    <span className="text-[12.5px] tabular-nums text-mocha">
                      {day && day.entryCount > 0
                        ? formatCount(day.entryCount, 'posiłek', 'posiłki', 'posiłków')
                        : 'brak posiłków'}
                    </span>
                  </li>
                )
              })}
            </ul>
          </div>

          {selectedMealCount === 0 && (
            <p role="status" className="text-[13px] text-mocha">
              W wybranych dniach nie ma jeszcze posiłków — lista powstanie pusta i uzupełni się,
              gdy zaktualizujesz ją po zaplanowaniu posiłków.
            </p>
          )}
        </div>
      )}
    </Dialog>
  )
}

/** Candidate days when the planner range is unavailable, so day selection still works offline of it. */
function fallbackDays(from: string): { date: string; entryCount: number }[] {
  return Array.from({ length: WINDOW_DAYS }, (_, index) => ({
    date: addDays(from, index),
    entryCount: 0,
  }))
}

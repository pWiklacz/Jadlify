import type { PlannerView } from './dateRange'

interface PlannerRangeNavProps {
  view: PlannerView
  date: string
  /** True when the range is not showing the period that contains today. */
  showBackToday: boolean
  onViewChange: (view: PlannerView) => void
  onPrev: () => void
  onNext: () => void
  onDateChange: (date: string) => void
  onToday: () => void
  /** Opens the add-meal dialog (desktop; mobile uses the floating action button). */
  onAdd: () => void
}

const VIEW_LABELS: Record<PlannerView, string> = {
  day: 'Dzień',
  week: 'Tydzień',
  month: 'Miesiąc',
}

const PERIOD_NOUN: Record<PlannerView, string> = {
  day: 'dzień',
  week: 'tydzień',
  month: 'miesiąc',
}

/**
 * The planner's control row: the day/week/month toggle, prev/next period stepping,
 * a native date picker, a "back to today" shortcut, and the desktop add button. A
 * period step changes the range (one new request); switching the date picker jumps
 * the selected day, which only refetches when it lands outside the current window.
 */
export function PlannerRangeNav({
  view,
  date,
  showBackToday,
  onViewChange,
  onPrev,
  onNext,
  onDateChange,
  onToday,
  onAdd,
}: PlannerRangeNavProps) {
  const noun = PERIOD_NOUN[view]

  return (
    <div className="flex flex-wrap items-center gap-2">
      <div
        role="group"
        aria-label="Przełącz widok"
        className="flex gap-0.5 rounded-pill border border-parchment/16 p-[3px]"
      >
        {(Object.keys(VIEW_LABELS) as PlannerView[]).map((option) => {
          const active = option === view
          return (
            <button
              key={option}
              type="button"
              aria-pressed={active}
              onClick={() => onViewChange(option)}
              className={[
                'rounded-pill px-3.5 py-1.5 text-[13px] font-semibold transition-colors',
                active
                  ? 'bg-parchment/12 text-parchment'
                  : 'text-parchment/60 hover:text-parchment',
              ].join(' ')}
            >
              {VIEW_LABELS[option]}
            </button>
          )
        })}
      </div>

      {showBackToday && (
        <button
          type="button"
          onClick={onToday}
          className="rounded-pill border border-terracotta/55 px-4 py-2 text-[13px] font-semibold text-terracotta transition-colors hover:bg-terracotta/10"
        >
          Wróć do dzisiaj
        </button>
      )}

      <div className="flex items-center gap-1 rounded-pill border border-parchment/16 px-1.5 py-1">
        <button
          type="button"
          aria-label={`Poprzedni ${noun}`}
          onClick={onPrev}
          className="flex h-8 w-8 items-center justify-center rounded-full text-parchment transition-colors hover:bg-parchment/10"
        >
          <span aria-hidden="true">‹</span>
        </button>
        <input
          type="date"
          aria-label="Wybierz datę"
          value={date}
          onChange={(event) => onDateChange(event.currentTarget.value)}
          className="rounded-pill bg-transparent px-2 py-1 text-[13.5px] font-semibold tabular-nums text-parchment [color-scheme:dark]"
        />
        <button
          type="button"
          aria-label={`Następny ${noun}`}
          onClick={onNext}
          className="flex h-8 w-8 items-center justify-center rounded-full text-parchment transition-colors hover:bg-parchment/10"
        >
          <span aria-hidden="true">›</span>
        </button>
      </div>

      <span className="flex-1" />

      <button
        type="button"
        onClick={onAdd}
        className="hidden items-center gap-2 rounded-pill bg-terracotta-strong px-[18px] py-2.5 text-[13.5px] font-semibold text-paper transition-colors hover:bg-terracotta-hover design:inline-flex"
      >
        + Dodaj posiłek
      </button>
    </div>
  )
}

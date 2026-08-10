interface DashboardDateNavProps {
  date: string
  /** True when the selected day is not today. */
  showBackToday: boolean
  onPrev: () => void
  onNext: () => void
  onDateChange: (date: string) => void
  onToday: () => void
}

/**
 * The dashboard's day stepper. Deliberately narrower than the planner's control
 * row — there is no view toggle here, because the dashboard is always exactly one
 * day — but it steps and jumps the same way, so the two surfaces feel like one app.
 */
export function DashboardDateNav({
  date,
  showBackToday,
  onPrev,
  onNext,
  onDateChange,
  onToday,
}: DashboardDateNavProps) {
  return (
    <div className="flex flex-wrap items-center gap-2">
      <div className="flex items-center gap-1 rounded-pill border border-parchment/16 px-1.5 py-1">
        <button
          type="button"
          aria-label="Poprzedni dzień"
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
          aria-label="Następny dzień"
          onClick={onNext}
          className="flex h-8 w-8 items-center justify-center rounded-full text-parchment transition-colors hover:bg-parchment/10"
        >
          <span aria-hidden="true">›</span>
        </button>
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
    </div>
  )
}

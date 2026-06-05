import { Link } from 'react-router-dom'
import { formatMacro } from '../recipes/macroMath'
import type { DailyMacroSummary } from './types'

interface DailyMacroSummaryPanelProps {
  summary: DailyMacroSummary | undefined
  isLoading: boolean
  isError: boolean
}

const MACRO_ROWS = [
  { key: 'calories', label: 'kcal', unit: '' },
  { key: 'protein', label: 'Protein', unit: ' g' },
  { key: 'fat', label: 'Fat', unit: ' g' },
  { key: 'carbohydrates', label: 'Carbs', unit: ' g' },
] as const

export function DailyMacroSummaryPanel({
  summary,
  isLoading,
  isError,
}: DailyMacroSummaryPanelProps) {
  if (isLoading) {
    return (
      <section
        aria-label="Daily macro summary"
        className="rounded-lg border border-slate-200 bg-white p-4"
      >
        <h2 className="text-lg font-semibold">Daily macro summary</h2>
        <p className="mt-2 text-sm text-slate-600">Loading daily macro summary.</p>
      </section>
    )
  }

  if (isError) {
    return (
      <section
        aria-label="Daily macro summary"
        className="rounded-lg border border-red-200 bg-white p-4"
      >
        <h2 className="text-lg font-semibold">Daily macro summary</h2>
        <p role="alert" className="mt-2 text-sm text-red-600">
          Could not load the daily macro summary. Please refresh to try again.
        </p>
      </section>
    )
  }

  return (
    <section
      aria-label="Daily macro summary"
      className="rounded-lg border border-slate-200 bg-white p-4"
    >
      <div className="flex flex-col gap-1">
        <h2 className="text-lg font-semibold">Daily macro summary</h2>
        <p className="text-sm text-slate-600">Totals for the selected day.</p>
      </div>

      <dl className="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {MACRO_ROWS.map((row) => (
          <div key={row.key} className="rounded-md border border-slate-200 bg-slate-50 p-3">
            <dt className="text-sm font-medium text-slate-500">{row.label}</dt>
            <dd className="mt-1 text-xl font-semibold text-slate-900">
              {formatMacro(summary?.total[row.key] ?? 0)}
              {row.unit}
            </dd>
            {summary?.remaining && (
              <dd
                className={
                  summary.remaining[row.key] < 0
                    ? 'mt-1 text-sm font-medium text-red-600'
                    : 'mt-1 text-sm font-medium text-emerald-700'
                }
              >
                {formatMacro(summary.remaining[row.key])}
                {row.unit} remaining
              </dd>
            )}
          </div>
        ))}
      </dl>

      {summary && !summary.goal && (
        <p className="mt-4 text-sm text-slate-600">
          Set a daily goal to see remaining macros.{' '}
          <Link to="/goals" className="font-medium text-slate-900 underline">
            Set a goal
          </Link>
        </p>
      )}
    </section>
  )
}

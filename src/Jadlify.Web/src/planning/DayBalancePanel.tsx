import { Link } from 'react-router-dom'
import { MacroCompareRow } from '../ui/MacroCompareRow'
import { entryMacroLine } from './plannerMacros'
import type { MacroSummary } from './types'

interface DayBalancePanelProps {
  total: MacroSummary
  goal: MacroSummary | null
}

/**
 * The selected day's balance: one {@link MacroCompareRow} per macro against the
 * goal (each with a words-not-colour status), or, when no goal is set, the plain
 * planned totals and a prompt to configure a daily goal. Planning works either
 * way — the goal only unlocks the "how much is left" comparison.
 */
export function DayBalancePanel({ total, goal }: DayBalancePanelProps) {
  if (!goal) {
    return (
      <div className="rounded-panel border border-cream-border bg-cream-panel px-[18px] py-4">
        <h3 className="font-serif text-xl font-normal text-espresso">Brak dziennych celów</h3>
        <p className="mt-2 text-[13px] leading-relaxed text-mocha">
          Zaplanowany dzień to:{' '}
          <b className="tabular-nums text-espresso">{entryMacroLine(total)}</b>
        </p>
        <p className="mt-2.5 text-[13px] leading-relaxed text-mocha">
          Ustaw cel kalorii i makro, aby zobaczyć, ile zostało do celu w każdym dniu. Planowanie
          działa też bez celu.
        </p>
        <Link
          to="/goals"
          className="mt-3.5 inline-block rounded-pill bg-terracotta-strong px-[18px] py-2.5 text-[13px] font-semibold text-paper hover:bg-terracotta-hover"
        >
          Ustaw dzienne cele
        </Link>
      </div>
    )
  }

  return (
    <div className="rounded-panel border border-cream-border bg-cream-panel px-[18px] py-4">
      <h3 className="mb-3.5 font-serif text-xl font-normal text-espresso">Bilans dnia</h3>
      <div className="flex flex-col gap-3.5">
        <MacroCompareRow label="Kalorie" value={total.calories} goal={goal.calories} kind="kcal" />
        <MacroCompareRow label="Białko" value={total.protein} goal={goal.protein} kind="protein" />
        <MacroCompareRow
          label="Węglowodany"
          value={total.carbohydrates}
          goal={goal.carbohydrates}
          kind="carbs"
        />
        <MacroCompareRow label="Tłuszcz" value={total.fat} goal={goal.fat} kind="fat" />
      </div>
    </div>
  )
}

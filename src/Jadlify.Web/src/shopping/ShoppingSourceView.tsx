import { formatDayShort } from '../planning/plannerFormat'
import { CheckIcon } from '../ui/icons'
import { formatCount, formatGrams, mealTypeLabel } from '../ui/formatters'
import {
  formatSourceDayTitle,
  groupByDay,
  groupByRecipe,
  type SourceIngredient,
} from './shoppingFormat'
import type { ShoppingListItem } from './types'

interface ShoppingSourceViewProps {
  items: readonly ShoppingListItem[]
  /** `meal` folds every day's portions into one card per recipe; `day` nests meals under each day. */
  mode: 'meal' | 'day'
}

/**
 * The "Wg posiłków" and "Wg dni" regroupings.
 *
 * Both read the same already-loaded lines — a list carries the meal contributions
 * that produced each amount — so switching views costs no request and can never
 * disagree with the shopping view about a total. These views are explanatory, not
 * actionable: ticking happens in "Zakupy", against the aggregated product line.
 */
export function ShoppingSourceView({ items, mode }: ShoppingSourceViewProps) {
  if (mode === 'meal') {
    return (
      <>
        {groupByRecipe(items).map((group) => (
          <section
            key={group.sourceLabel}
            aria-label={group.sourceLabel}
            className="animate-rise rounded-card bg-cream px-5 py-4 text-espresso"
          >
            <div className="mb-1 flex flex-wrap items-baseline gap-x-3 gap-y-1.5">
              <h3 className="min-w-0 break-name font-serif text-[21px] font-normal">
                {group.sourceLabel}
              </h3>
              <span
                aria-hidden="true"
                className="min-w-[20px] flex-1 self-center border-b-2 border-dotted border-cream-line"
              />
              <span className="text-[12.5px] tabular-nums text-mocha">
                {formatCount(group.mealCount, 'posiłek', 'posiłki', 'posiłków')} ·{' '}
                {formatCount(group.dates.length, 'dzień', 'dni', 'dni')}
              </span>
            </div>
            <p className="mb-2.5 text-xs text-faint">
              {group.dates.map((date) => formatDayShort(date)).join(' · ')}
            </p>
            <IngredientList ingredients={group.ingredients} />
          </section>
        ))}

        <p className="px-3 py-0.5 text-center text-[12.5px] leading-relaxed text-parchment/45">
          Porcje tego samego przepisu ze wszystkich dni są zsumowane — składniki pokazują
          łączną ilość do kupienia.
        </p>
      </>
    )
  }

  return (
    <>
      {groupByDay(items).map((day) => (
        <section
          key={day.date}
          aria-label={formatSourceDayTitle(day.date)}
          className="animate-rise rounded-card bg-cream px-5 py-4 text-espresso"
        >
          <div className="mb-0.5 flex items-baseline gap-2.5">
            <h3 className="font-serif text-[21px] font-normal">
              {formatSourceDayTitle(day.date)}
            </h3>
            <span
              aria-hidden="true"
              className="min-w-[20px] flex-1 self-center border-b-2 border-dotted border-cream-line"
            />
            <span className="flex-none text-xs tabular-nums text-mocha">
              {formatCount(day.meals.length, 'posiłek', 'posiłki', 'posiłków')}
            </span>
          </div>

          <div className="flex flex-col">
            {day.meals.map((meal) => (
              <div key={meal.key} className="border-t border-dotted border-cream-line py-2.5">
                <div className="mb-1.5 flex flex-wrap items-baseline gap-x-2.5 gap-y-1.5">
                  <span className="rounded-pill bg-cream-hover px-2.5 py-[3px] text-[10px] font-bold uppercase tracking-[0.12em] text-label">
                    {mealTypeLabel(meal.mealType)}
                  </span>
                  <span className="min-w-0 break-name text-sm font-bold">
                    {meal.sourceLabel}
                  </span>
                </div>
                <IngredientList ingredients={meal.ingredients} />
              </div>
            ))}
          </div>
        </section>
      ))}

      <p className="px-3 py-0.5 text-center text-[12.5px] leading-relaxed text-parchment/45">
        Ten sam produkt może pojawiać się w kilku posiłkach — na liście „Zakupy” widnieje raz,
        z zsumowaną ilością.
      </p>
    </>
  )
}

/** The shared "product … dotted leader … amount" rows both source views render. */
function IngredientList({ ingredients }: { ingredients: readonly SourceIngredient[] }) {
  return (
    <ul className="flex flex-col gap-[3px]">
      {ingredients.map((ingredient) => (
        <li key={ingredient.key} className="flex items-baseline gap-2 py-[3px] text-[13px]">
          <span
            aria-hidden="true"
            className={[
              'flex h-[15px] w-[15px] flex-none items-center justify-center self-center rounded-[5px] border',
              ingredient.isBought
                ? 'border-success bg-success text-paper'
                : 'border-cream-mark bg-cream-panel',
            ].join(' ')}
          >
            {ingredient.isBought && <CheckIcon size={9} strokeWidth={3} />}
          </span>

          <span
            className={[
              'min-w-0 shrink break-name',
              ingredient.isBought ? 'text-muted line-through decoration-faint' : '',
            ]
              .filter(Boolean)
              .join(' ')}
          >
            {ingredient.productName}
            {ingredient.isBought && <span className="sr-only"> (kupione)</span>}
          </span>

          <span
            aria-hidden="true"
            className="min-w-[12px] flex-1 self-center border-b-2 border-dotted border-cream-border"
          />

          <span
            className={[
              'flex-none font-bold tabular-nums',
              ingredient.isBought ? 'text-muted' : '',
            ]
              .filter(Boolean)
              .join(' ')}
          >
            {formatGrams(ingredient.grams)}
          </span>
        </li>
      ))}
    </ul>
  )
}

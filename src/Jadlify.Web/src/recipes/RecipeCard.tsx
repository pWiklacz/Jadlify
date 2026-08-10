import { Link } from 'react-router-dom'
import { formatCount, formatKcal, formatMacro } from '../ui/formatters'
import type { RecipeSummary } from './types'

interface RecipeCardProps {
  recipe: RecipeSummary
  /** Link target for the detail route (filters are carried in `state`). */
  to: string
  /** Search string of the originating index, preserved for the return trip. */
  fromSearch: string
  onEdit: () => void
  onDelete: () => void
}

/**
 * One recipe in the catalog grid: a cream card whose body links to the detail
 * route, with an edit/delete action row in the footer. Recipes in the meal plan
 * carry a "W PLANIE" badge — text, not colour alone. Long names wrap rather than
 * truncate, and macros are shown as words plus numbers.
 */
export function RecipeCard({ recipe, to, fromSearch, onEdit, onDelete }: RecipeCardProps) {
  const perServing = recipe.perServingMacros
  const macroLine = `B ${formatMacro(perServing.protein)} · T ${formatMacro(perServing.fat)} · W ${formatMacro(perServing.carbohydrates)}`
  const metaLine = [
    formatCount(recipe.portions, 'porcja', 'porcje', 'porcji'),
    formatCount(recipe.ingredientCount, 'składnik', 'składniki', 'składników'),
  ].join(' · ')

  return (
    <div className="flex flex-col rounded-card border border-black/5 bg-cream text-espresso shadow-sm transition-shadow hover:shadow-card">
      <Link
        to={to}
        state={{ fromSearch }}
        className="flex flex-1 flex-col gap-2 rounded-t-card p-[18px] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-terracotta"
      >
        <div className="flex items-start gap-2">
          <span className="min-w-0 flex-1 break-words font-serif text-[21px] leading-tight text-espresso">
            {recipe.name}
          </span>
          {recipe.isInPlan && (
            <span
              title="Używany w zaplanowanych posiłkach"
              className="flex-none rounded-pill border border-success/30 bg-success/10 px-2.5 py-1 text-[9.5px] font-bold uppercase tracking-eyebrow text-success-ink"
            >
              W planie
            </span>
          )}
        </div>

        <div className="text-[12.5px] text-mocha">{metaLine}</div>

        <div className="mt-0.5 flex flex-wrap items-baseline gap-2">
          <span className="font-serif text-[30px] leading-none tabular-nums">
            {formatKcal(perServing.calories)}
          </span>
          <span className="text-[11px] font-bold uppercase tracking-eyebrow text-mocha">
            kcal / porcję
          </span>
        </div>
        <div className="text-[13px] tabular-nums text-label">
          {macroLine} <span className="text-mocha">/ porcję</span>
        </div>
      </Link>

      <div className="mt-1 flex items-center justify-end gap-1 border-t border-dotted border-cream-line px-[18px] py-2">
        <button
          type="button"
          onClick={onEdit}
          aria-label={`Edytuj przepis ${recipe.name}`}
          title="Edytuj"
          className="flex h-10 w-10 items-center justify-center rounded-full text-mocha transition-colors hover:bg-cream-hover hover:text-espresso"
        >
          <EditGlyph />
        </button>
        <button
          type="button"
          onClick={onDelete}
          aria-label={`Usuń przepis ${recipe.name}`}
          title="Usuń"
          className="flex h-10 w-10 items-center justify-center rounded-full text-mocha transition-colors hover:bg-danger/10 hover:text-danger"
        >
          <TrashGlyph />
        </button>
      </div>
    </div>
  )
}

function EditGlyph() {
  return (
    <svg width="15" height="15" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M4 13.7 13 4.7l2.3 2.3-9 9L3 17z" />
      <path d="M11.5 6.2l2.3 2.3" />
    </svg>
  )
}

function TrashGlyph() {
  return (
    <svg width="15" height="15" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M4 6h12" />
      <path d="M8 6V4.5h4V6" />
      <path d="M6 6l1 10.5h6L14 6" />
      <path d="M9 9v4.5M11 9v4.5" />
    </svg>
  )
}

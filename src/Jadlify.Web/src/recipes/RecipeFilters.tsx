import { RECIPE_SORTS, type RecipeCatalogSort } from './types'

interface RecipeFiltersProps {
  /** Live search-box value (debounced by the parent before it hits the query). */
  search: string
  onSearchChange: (value: string) => void
  onClearSearch: () => void
  sort: RecipeCatalogSort
  onSortChange: (value: RecipeCatalogSort) => void
  /** Ready-formatted "N przepisów" count for the current result set. */
  countLabel: string
}

/**
 * Index toolbar: a debounced name search box, a sort control and the result
 * count. Changing either input resets paging upstream (the parent drops `skip`).
 */
export function RecipeFilters({
  search,
  onSearchChange,
  onClearSearch,
  sort,
  onSortChange,
  countLabel,
}: RecipeFiltersProps) {
  return (
    <div className="mb-4 flex flex-wrap items-center gap-2.5">
      <div className="relative min-w-[220px] flex-1">
        <span
          aria-hidden="true"
          className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-parchment/45"
        >
          <SearchGlyph />
        </span>
        <input
          type="text"
          value={search}
          onChange={(event) => onSearchChange(event.target.value)}
          placeholder="Szukaj przepisu po nazwie…"
          aria-label="Szukaj przepisu po nazwie"
          className="min-h-[46px] w-full rounded-pill border border-parchment/15 bg-parchment/[0.06] py-2.5 pl-10 pr-11 text-sm text-parchment outline-none placeholder:text-parchment/45 focus:border-terracotta"
        />
        {search && (
          <button
            type="button"
            onClick={onClearSearch}
            aria-label="Wyczyść wyszukiwanie"
            title="Wyczyść wyszukiwanie"
            className="absolute right-1.5 top-1/2 flex h-9 w-9 -translate-y-1/2 items-center justify-center rounded-full text-parchment/60 transition-colors hover:bg-parchment/10 hover:text-parchment"
          >
            <CloseGlyph />
          </button>
        )}
      </div>

      <label className="flex items-center gap-2 text-[12.5px] text-parchment/55">
        Sortuj:
        <select
          value={sort}
          onChange={(event) => onSortChange(event.target.value as RecipeCatalogSort)}
          aria-label="Sortowanie przepisów"
          className="min-h-[44px] cursor-pointer rounded-pill border border-parchment/15 bg-parchment/[0.06] px-3.5 py-2 text-[13.5px] text-parchment outline-none focus:border-terracotta"
        >
          {RECIPE_SORTS.map((option) => (
            <option key={option.value} value={option.value} className="text-espresso">
              {option.label}
            </option>
          ))}
        </select>
      </label>

      <span className="text-[13px] tabular-nums text-parchment/55">{countLabel}</span>
    </div>
  )
}

function SearchGlyph() {
  return (
    <svg width="16" height="16" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" aria-hidden="true">
      <circle cx="9" cy="9" r="5.5" />
      <path d="m13.5 13.5 3.5 3.5" />
    </svg>
  )
}

function CloseGlyph() {
  return (
    <svg width="13" height="13" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" aria-hidden="true">
      <path d="M5 5l10 10M15 5 5 15" />
    </svg>
  )
}

import { categoryLabel, PRODUCT_CATEGORIES, PRODUCT_SORTS } from './types'
import type { ProductCatalogSort } from './types'
import type { CatalogCategoryFilter } from './useProductCatalog'

interface ProductFiltersProps {
  /** Live search-box value (debounced by the parent before it hits the query). */
  search: string
  onSearchChange: (value: string) => void
  onClearSearch: () => void
  category: CatalogCategoryFilter
  onCategoryChange: (value: CatalogCategoryFilter) => void
  sort: ProductCatalogSort
  onSortChange: (value: ProductCatalogSort) => void
  /** Ready-formatted "N produktów" count for the current result set. */
  countLabel: string
  /** The applied search term (drives the chip label), or empty when none. */
  appliedSearch: string
  onClearAll: () => void
}

const CONTROL_CLASSES =
  'min-h-[44px] rounded-pill border border-parchment/15 bg-parchment/[0.06] px-3.5 text-[13.5px] text-parchment outline-none focus:border-terracotta'

/** Human label for a category chip, including the "Bez kategorii" sentinel. */
function filterCategoryLabel(category: CatalogCategoryFilter): string {
  if (category === 'None') {
    return 'Bez kategorii'
  }
  return categoryLabel(category as never)
}

/**
 * Index toolbar: a debounced search box, a category filter, a sort control, the
 * result count, and a removable chip for each active filter. Changing any filter
 * resets paging upstream (the parent drops `skip` to 0).
 */
export function ProductFilters({
  search,
  onSearchChange,
  onClearSearch,
  category,
  onCategoryChange,
  sort,
  onSortChange,
  countLabel,
  appliedSearch,
  onClearAll,
}: ProductFiltersProps) {
  const filtersActive = appliedSearch.trim() !== '' || category !== 'all'

  return (
    <div className="mb-3 flex flex-col gap-3">
      <div className="flex flex-wrap items-center gap-2.5">
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
            placeholder="Szukaj po nazwie lub kodzie kreskowym…"
            aria-label="Szukaj produktu po nazwie lub kodzie kreskowym"
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
          Kategoria:
          <select
            value={category}
            onChange={(event) => onCategoryChange(event.target.value)}
            aria-label="Filtruj według kategorii"
            className={`max-w-[210px] cursor-pointer py-2 ${CONTROL_CLASSES}`}
          >
            <option value="all" className="text-espresso">
              Wszystkie
            </option>
            {PRODUCT_CATEGORIES.map((c) => (
              <option key={c.value} value={c.value} className="text-espresso">
                {c.label}
              </option>
            ))}
            <option value="None" className="text-espresso">
              Bez kategorii
            </option>
          </select>
        </label>

        <label className="flex items-center gap-2 text-[12.5px] text-parchment/55">
          Sortuj:
          <select
            value={sort}
            onChange={(event) => onSortChange(event.target.value as ProductCatalogSort)}
            aria-label="Sortowanie produktów"
            className={`cursor-pointer py-2 ${CONTROL_CLASSES}`}
          >
            {PRODUCT_SORTS.map((s) => (
              <option key={s.value} value={s.value} className="text-espresso">
                {s.label}
              </option>
            ))}
          </select>
        </label>

        <span className="text-[13px] tabular-nums text-parchment/55">{countLabel}</span>
      </div>

      {filtersActive && (
        <div className="flex flex-wrap items-center gap-2">
          <span className="text-xs font-bold uppercase tracking-eyebrow text-parchment/45">
            Filtry:
          </span>
          {appliedSearch.trim() !== '' && (
            <FilterChip label={`„${appliedSearch.trim()}”`} onRemove={onClearSearch} removeLabel="Usuń filtr wyszukiwania" />
          )}
          {category !== 'all' && (
            <FilterChip
              label={filterCategoryLabel(category)}
              onRemove={() => onCategoryChange('all')}
              removeLabel="Usuń filtr kategorii"
            />
          )}
          <button
            type="button"
            onClick={onClearAll}
            className="min-h-[36px] px-2 text-[12.5px] font-semibold text-parchment/55 underline underline-offset-[3px] transition-colors hover:text-parchment"
          >
            Wyczyść wszystko
          </button>
        </div>
      )}
    </div>
  )
}

function FilterChip({
  label,
  onRemove,
  removeLabel,
}: {
  label: string
  onRemove: () => void
  removeLabel: string
}) {
  return (
    <button
      type="button"
      onClick={onRemove}
      title={removeLabel}
      aria-label={removeLabel}
      className="inline-flex min-h-[36px] items-center gap-2 rounded-pill border border-terracotta/50 bg-terracotta/10 px-3 py-1.5 text-[12.5px] font-semibold text-terracotta transition-colors hover:bg-terracotta/20"
    >
      {label}
      <CloseGlyph small />
    </button>
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

function CloseGlyph({ small = false }: { small?: boolean }) {
  const size = small ? 11 : 13
  return (
    <svg width={size} height={size} viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" aria-hidden="true">
      <path d="M5 5l10 10M15 5 5 15" />
    </svg>
  )
}

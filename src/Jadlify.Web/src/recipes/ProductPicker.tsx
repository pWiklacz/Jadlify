import { useEffect, useRef, useState } from 'react'
import { formatKcal } from '../ui/formatters'
import { useProductSearch } from './useProductSearch'
import type { RecipeProductSelection } from './types'

interface ProductPickerProps {
  /** Ties the picker's trigger to its `Field` label. */
  id: string
  selected: RecipeProductSelection | null
  /** Products already used by other rows; selecting them again is blocked. */
  excludedProductIds: string[]
  onSelect: (product: RecipeProductSelection) => void
  onAddMissing: () => void
  invalid?: boolean
  describedBy?: string
}

/**
 * Search/select control for one ingredient row: a button showing the current
 * choice that opens an inline listbox with a name/barcode search. A product used
 * by another row is listed but disabled ("już w przepisie"), so the duplicate
 * rule is visible rather than silently swallowed on save.
 */
export function ProductPicker({
  id,
  selected,
  excludedProductIds,
  onSelect,
  onAddMissing,
  invalid = false,
  describedBy,
}: ProductPickerProps) {
  const [open, setOpen] = useState(false)
  const [search, setSearch] = useState('')
  const containerRef = useRef<HTMLDivElement>(null)
  const searchRef = useRef<HTMLInputElement>(null)
  const { data: products, isPending, isError } = useProductSearch(search)

  useEffect(() => {
    if (open) {
      searchRef.current?.focus()
    }
  }, [open])

  // Clicking outside closes the list; Escape is handled on the container so it
  // does not bubble up and close the surrounding dialog as well.
  useEffect(() => {
    if (!open) {
      return
    }
    function onPointerDown(event: MouseEvent) {
      if (!containerRef.current?.contains(event.target as Node)) {
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', onPointerDown)
    return () => document.removeEventListener('mousedown', onPointerDown)
  }, [open])

  return (
    <div
      ref={containerRef}
      className="relative"
      onKeyDown={(event) => {
        if (event.key === 'Escape' && open) {
          event.stopPropagation()
          setOpen(false)
        }
      }}
    >
      <button
        type="button"
        id={id}
        onClick={() => setOpen((current) => !current)}
        aria-expanded={open}
        aria-invalid={invalid || undefined}
        aria-describedby={describedBy}
        className={[
          'flex min-h-[46px] w-full items-center gap-2 rounded-field border-[1.5px] bg-cream-input px-3.5 py-2.5 text-left text-espresso transition-colors focus:border-terracotta',
          invalid ? 'border-danger' : 'border-cream-border',
        ].join(' ')}
      >
        <span className={`min-w-0 flex-1 break-words ${selected ? '' : 'text-mocha/80'}`}>
          {selected ? selected.name : 'Wybierz produkt…'}
        </span>
        <span aria-hidden="true" className="flex-none text-mocha">
          <ChevronGlyph />
        </span>
      </button>

      {open && (
        <div className="absolute left-0 right-0 z-10 mt-1 overflow-hidden rounded-panel border border-cream-border bg-cream shadow-modal">
          <div className="p-2">
            <input
              ref={searchRef}
              type="search"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Szukaj produktu…"
              aria-label="Szukaj produktu"
              className="w-full rounded-field border-[1.5px] border-cream-border bg-cream-input px-3 py-2 text-sm text-espresso outline-none placeholder:text-mocha/70 focus:border-terracotta"
            />
          </div>

          <ul className="max-h-56 overflow-y-auto" aria-label="Wybierz produkt">
            {isPending && (
              <li className="px-3.5 py-2.5 text-sm text-mocha">Szukamy produktów…</li>
            )}
            {isError && (
              <li role="alert" className="px-3.5 py-2.5 text-sm text-danger">
                Nie udało się wyszukać produktów.
              </li>
            )}
            {products?.length === 0 && (
              <li className="px-3.5 py-2.5 text-sm text-mocha">Brak pasujących produktów.</li>
            )}
            {products?.map((product) => {
              const alreadyUsed =
                excludedProductIds.includes(product.id) && selected?.id !== product.id
              return (
                <li key={product.id}>
                  <button
                    type="button"
                    disabled={alreadyUsed}
                    onClick={() => {
                      onSelect(product)
                      setOpen(false)
                    }}
                    className="flex w-full items-center gap-3 border-b border-dotted border-cream-line px-3.5 py-2.5 text-left last:border-b-0 hover:bg-cream-hover disabled:cursor-not-allowed disabled:opacity-55 disabled:hover:bg-transparent"
                  >
                    <span className="min-w-0 flex-1">
                      <span className="block break-words text-[13.5px] font-semibold text-espresso">
                        {product.name}
                      </span>
                      <span className="text-xs tabular-nums text-mocha">
                        {formatKcal(product.calories)} kcal / 100 g
                      </span>
                    </span>
                    {alreadyUsed && (
                      <span className="flex-none rounded-pill bg-cream-hover px-2 py-0.5 text-[9.5px] font-bold uppercase tracking-eyebrow text-mocha">
                        Już w przepisie
                      </span>
                    )}
                  </button>
                </li>
              )
            })}
          </ul>

          <div className="border-t border-cream-border p-2">
            <button
              type="button"
              onClick={() => {
                setOpen(false)
                onAddMissing()
              }}
              className="min-h-[40px] w-full rounded-pill px-3 text-[13px] font-bold text-terracotta underline underline-offset-[3px] hover:bg-terracotta/10"
            >
              Dodaj nowy produkt
            </button>
          </div>
        </div>
      )}
    </div>
  )
}

function ChevronGlyph() {
  return (
    <svg width="14" height="14" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.9" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="m5 8 5 5 5-5" />
    </svg>
  )
}

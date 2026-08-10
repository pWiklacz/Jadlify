import { useId, useState } from 'react'
import { formatDayShort } from '../planning/plannerFormat'
import { CheckIcon, ChevronDownIcon } from '../ui/icons'
import { formatGrams, mealTypeLabel } from '../ui/formatters'
import { itemCategoryLabel, type ItemsGroup } from './shoppingFormat'
import type { ShoppingListItem } from './types'

interface ShoppingItemsViewProps {
  /** Already filtered and ordered by the toolbar; `label` is empty for flat orders. */
  groups: readonly ItemsGroup[]
  /** Completed lists are frozen snapshots — their checkboxes are read-only. */
  readOnly: boolean
  /** True while a toggle is in flight; keeps the optimistic tick visible but blocks new writes. */
  busy: boolean
  /** Shows each line's category when the view is not already grouped into aisles. */
  showCategoryTag: boolean
  onToggle: (item: ShoppingListItem, isBought: boolean) => void
}

/**
 * The default "Zakupy" view: aisle-style category cards of tickable product lines.
 *
 * Each line's accessible name is the product name and its amount is wired through
 * `aria-describedby`, so a screen-reader user hears both without the grams being
 * folded into the label. Expanding a line names the meals the amount came from,
 * which is what makes an aggregated total explainable while standing in a shop.
 */
export function ShoppingItemsView({
  groups,
  readOnly,
  busy,
  showCategoryTag,
  onToggle,
}: ShoppingItemsViewProps) {
  return (
    <>
      {groups.map((group) => (
        <section
          key={group.key}
          aria-label={group.label || 'Produkty'}
          className="animate-rise rounded-card bg-cream px-[18px] pb-2.5 pt-1.5 text-espresso"
        >
          {group.label !== '' && (
            <div className="flex items-baseline gap-2.5 pb-1 pt-3">
              <h3 className="text-[11px] font-bold uppercase tracking-eyebrow text-mocha">
                {group.label}
              </h3>
              <span className="text-[11px] font-semibold tabular-nums text-faint">
                {group.items.filter((item) => item.isBought).length}/{group.items.length}
              </span>
            </div>
          )}

          <ul className="flex flex-col">
            {group.items.map((item) => (
              <ShoppingItemRow
                key={item.id}
                item={item}
                readOnly={readOnly}
                busy={busy}
                showCategoryTag={showCategoryTag}
                onToggle={onToggle}
              />
            ))}
          </ul>
        </section>
      ))}
    </>
  )
}

interface ShoppingItemRowProps {
  item: ShoppingListItem
  readOnly: boolean
  busy: boolean
  showCategoryTag: boolean
  onToggle: (item: ShoppingListItem, isBought: boolean) => void
}

function ShoppingItemRow({
  item,
  readOnly,
  busy,
  showCategoryTag,
  onToggle,
}: ShoppingItemRowProps) {
  const [open, setOpen] = useState(false)
  const nameId = useId()
  const amountId = useId()
  const disabled = readOnly || busy

  return (
    <li className="border-t border-dotted border-cream-line first:border-t-0">
      <div className="flex min-h-[56px] items-center">
        {/*
          The tick target is a 48px label holding a transparent native checkbox
          stretched over the whole area, with the visible box painted underneath
          and click-through. Keyboard, screen-reader and `disabled` semantics stay
          exactly those of a real checkbox, and the input itself is the hit target
          — nothing overlays it.
        */}
        <label className="relative -ml-2 flex h-12 w-12 flex-none items-center justify-center">
          <input
            type="checkbox"
            checked={item.isBought}
            disabled={disabled}
            aria-labelledby={nameId}
            aria-describedby={amountId}
            onChange={(event) => onToggle(item, event.currentTarget.checked)}
            className="peer absolute inset-0 h-full w-full cursor-pointer appearance-none opacity-0 disabled:cursor-not-allowed"
          />
          <span
            aria-hidden="true"
            className={[
              'pointer-events-none flex h-[27px] w-[27px] items-center justify-center rounded-[9px] border-[1.5px] transition-colors',
              'peer-focus-visible:outline peer-focus-visible:outline-2 peer-focus-visible:outline-offset-2 peer-focus-visible:outline-terracotta',
              item.isBought
                ? 'border-success bg-success text-paper'
                : 'border-cream-mark bg-cream-panel',
              disabled ? 'opacity-60' : '',
            ]
              .filter(Boolean)
              .join(' ')}
          >
            {item.isBought && <CheckIcon size={14} />}
          </span>
        </label>

        <button
          type="button"
          onClick={() => setOpen((previous) => !previous)}
          aria-expanded={open}
          title="Pokaż, w których posiłkach użyto produktu"
          className="flex min-w-0 flex-1 items-baseline gap-2.5 py-3 text-left"
        >
          <span
            id={nameId}
            className={[
              'min-w-0 shrink break-name text-[15px] font-semibold leading-snug',
              item.isBought ? 'text-muted line-through decoration-faint' : 'text-espresso',
            ].join(' ')}
          >
            {item.productName}
          </span>

          {/* An unset category is a valid state, not a label worth spending a chip on. */}
          {showCategoryTag && item.category !== null && (
            <span className="flex-none rounded-pill border border-cream-border px-2 py-0.5 text-[10px] font-bold uppercase tracking-[0.1em] text-faint">
              {itemCategoryLabel(item.category)}
            </span>
          )}

          <span
            aria-hidden="true"
            className="min-w-[14px] flex-1 self-center border-b-2 border-dotted border-cream-line"
          />

          <span
            id={amountId}
            className={[
              'flex-none text-[15px] font-bold tabular-nums',
              item.isBought ? 'text-muted' : 'text-espresso',
            ].join(' ')}
          >
            {formatGrams(item.grams)}
          </span>

          <ChevronDownIcon
            size={13}
            className={[
              'flex-none self-center text-faint transition-transform',
              open ? 'rotate-180' : '',
            ]
              .filter(Boolean)
              .join(' ')}
          />
        </button>
      </div>

      {open && (
        <div className="mb-3 ml-[42px] border-l-2 border-cream-border pl-3.5">
          <h4 className="mb-1.5 text-[10.5px] font-bold uppercase tracking-eyebrow text-mocha">
            Użyto w
          </h4>
          <ul className="flex flex-col gap-1.5">
            {item.sources.map((source, index) => (
              <li
                key={`${source.date}-${source.mealType}-${source.sourceLabel}-${index}`}
                className="flex flex-wrap items-baseline gap-x-2 gap-y-1 text-[12.5px] text-label"
              >
                <span className="font-semibold">{formatDayShort(source.date)}</span>
                <span aria-hidden="true" className="text-faint">
                  ·
                </span>
                <span className="text-[11px] font-bold uppercase tracking-[0.08em] text-mocha">
                  {mealTypeLabel(source.mealType)}
                </span>
                <span aria-hidden="true" className="text-faint">
                  ·
                </span>
                <span className="min-w-0 flex-1 break-name">{source.sourceLabel}</span>
                <span className="font-bold tabular-nums">{formatGrams(source.grams)}</span>
              </li>
            ))}
          </ul>
          <p className="mt-2 text-[11.5px] leading-relaxed text-faint">
            Odhaczenie produktu nie zmienia przepisów ani planu posiłków.
          </p>
        </div>
      )}
    </li>
  )
}

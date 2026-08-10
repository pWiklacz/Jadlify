import {
  progressCaption,
  progressLabel,
  progressPercent,
  progressRemainingLabel,
} from './shoppingFormat'

interface ShoppingProgressProps {
  bought: number
  total: number
  /** Distinguishes this bar from others on the page for assistive tech. */
  label?: string
  /**
   * `inline` is the compact readout used on cards; `hero` is the detail screen's
   * headline block, where the count is the largest thing on the page.
   */
  variant?: 'inline' | 'hero'
}

/**
 * The "X z Y kupione" progress readout. The bar is decorative — the same figure is
 * always present as text and on the `progressbar` role — so progress never depends
 * on seeing a coloured fill.
 */
export function ShoppingProgress({
  bought,
  total,
  label = 'Postęp zakupów',
  variant = 'inline',
}: ShoppingProgressProps) {
  const percent = progressPercent(bought, total)
  const text = progressLabel(bought, total)

  const bar = (
    <div
      role="progressbar"
      aria-label={label}
      aria-valuenow={percent}
      aria-valuemin={0}
      aria-valuemax={100}
      aria-valuetext={text}
      className="h-2 overflow-hidden rounded-pill bg-cream-track"
    >
      <div
        aria-hidden="true"
        className="h-full rounded-pill bg-success transition-[width] duration-300"
        style={{ width: `${percent}%` }}
      />
    </div>
  )

  if (variant === 'hero') {
    return (
      <div className="flex flex-wrap items-center gap-x-7 gap-y-4">
        <p className="flex flex-none items-baseline gap-[7px]">
          <span className="font-serif text-[34px] leading-none tabular-nums text-espresso">
            {bought} / {total}
          </span>
          <span className="text-xs font-bold uppercase tracking-[0.12em] text-mocha">
            kupione
          </span>
        </p>
        <div className="min-w-0 flex-1 basis-60">
          {bar}
          <p className="mt-1.5 text-[12.5px] tabular-nums text-mocha">
            {progressCaption(bought, total)}
          </p>
        </div>
      </div>
    )
  }

  return (
    <div>
      <div className="flex items-baseline justify-between gap-2.5 text-[13px] tabular-nums text-mocha">
        <span>
          <b className="text-[15px] text-espresso">
            {bought} z {total}
          </b>{' '}
          kupione
        </span>
        <span>{progressRemainingLabel(bought, total)}</span>
      </div>
      <div className="mt-[7px]">{bar}</div>
    </div>
  )
}

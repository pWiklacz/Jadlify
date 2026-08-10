interface BrandMarkProps {
  /** Diameter of the leaf mark in px; the wordmark scales with it. */
  size?: number
  /** Shows the "PLANER POSIŁKÓW I MAKRO" tagline under the wordmark. */
  withTagline?: boolean
  /** Stack the mark above the wordmark (centered header) vs. inline. */
  stacked?: boolean
}

/**
 * The Jadlify brand mark: a terracotta leaf glyph plus the serif "Jadlify"
 * wordmark and an optional tagline. Shared by the app shell header and the auth
 * screen so the logo stays identical everywhere.
 */
export function BrandMark({ size = 34, withTagline = false, stacked = false }: BrandMarkProps) {
  return (
    <span
      className={[
        'inline-flex text-parchment',
        stacked ? 'flex-col items-center gap-1.5' : 'items-center gap-2.5',
      ].join(' ')}
    >
      <svg
        width={size}
        height={size}
        viewBox="0 0 32 32"
        fill="none"
        strokeWidth="1.7"
        strokeLinecap="round"
        strokeLinejoin="round"
        aria-hidden="true"
        className="stroke-terracotta"
      >
        <circle cx="16" cy="16" r="8.2" />
        <circle cx="16" cy="16" r="3.4" />
        <path d="M4.4 10.5v4a1.6 1.6 0 0 0 3.2 0v-4" />
        <path d="M6 10.5V25" />
        <path d="M26.6 10.5c-1.7 1.9-1.7 5 0 6.6V25" />
      </svg>
      <span className={stacked ? 'flex flex-col items-center gap-1' : 'flex flex-col'}>
        <span
          className="font-serif leading-none"
          style={{ fontSize: `${Math.round(size * 0.76)}px` }}
        >
          Jadlify
        </span>
        {withTagline && (
          <span className="text-[9px] font-bold tracking-tagline text-parchment/45">
            PLANER POSIŁKÓW I MAKRO
          </span>
        )}
      </span>
    </span>
  )
}

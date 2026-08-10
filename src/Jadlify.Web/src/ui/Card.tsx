import type { HTMLAttributes, ReactNode } from 'react'

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  /** Cream surface used for lighter sub-panels inside a card. */
  tone?: 'cream' | 'panel'
  /** Adds the standard card drop shadow (top-level cards on the dark shell). */
  elevated?: boolean
  children: ReactNode
}

const TONE_CLASSES: Record<NonNullable<CardProps['tone']>, string> = {
  cream: 'bg-cream',
  panel: 'bg-cream-panel border border-cream-border',
}

/**
 * The cream surface primitive. Dark-shell content sits on these rounded cream
 * cards (`espresso` text). Replaces repeated `rounded-… bg-… p-…` strings.
 */
export function Card({
  tone = 'cream',
  elevated = false,
  className = '',
  children,
  ...rest
}: CardProps) {
  return (
    <div
      className={[
        'rounded-card p-5 text-espresso design:p-6',
        TONE_CLASSES[tone],
        elevated ? 'shadow-card' : '',
        className,
      ]
        .filter(Boolean)
        .join(' ')}
      {...rest}
    >
      {children}
    </div>
  )
}

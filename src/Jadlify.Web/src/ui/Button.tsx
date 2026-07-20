import { forwardRef } from 'react'
import type { ButtonHTMLAttributes, ReactNode } from 'react'

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger'
export type ButtonSize = 'md' | 'sm'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant
  size?: ButtonSize
  /** Stretch to the container width (used for form submit buttons). */
  block?: boolean
  /** Shows a spinner and disables the button while an action is in flight. */
  isLoading?: boolean
  children: ReactNode
}

const VARIANT_CLASSES: Record<ButtonVariant, string> = {
  primary: 'bg-terracotta-strong text-paper hover:bg-terracotta-hover',
  secondary: 'bg-ink-800 text-parchment hover:bg-ink-700',
  ghost:
    'border border-terracotta/55 bg-transparent text-terracotta hover:bg-terracotta/10',
  danger: 'bg-danger text-paper hover:bg-danger-ink',
}

const SIZE_CLASSES: Record<ButtonSize, string> = {
  md: 'px-5 py-3 text-[15px]',
  sm: 'px-4 py-2 text-sm',
}

/**
 * The single button primitive for the redesign. Consolidates the four action
 * intents (primary / secondary / ghost / danger) that were previously copied as
 * ad-hoc class strings across features. Renders a pill, respects `disabled`,
 * and optionally shows an inline spinner while `isLoading`.
 */
export const Button = forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  {
    variant = 'primary',
    size = 'md',
    block = false,
    isLoading = false,
    disabled,
    type = 'button',
    className = '',
    children,
    ...rest
  },
  ref,
) {
  return (
    <button
      ref={ref}
      type={type}
      disabled={disabled || isLoading}
      className={[
        'inline-flex items-center justify-center gap-2 rounded-pill font-semibold transition-colors disabled:cursor-not-allowed disabled:opacity-60',
        VARIANT_CLASSES[variant],
        SIZE_CLASSES[size],
        block ? 'w-full' : '',
        className,
      ]
        .filter(Boolean)
        .join(' ')}
      {...rest}
    >
      {isLoading && (
        <span
          aria-hidden="true"
          className="h-4 w-4 animate-spin-slow rounded-full border-2 border-current border-t-transparent"
        />
      )}
      {children}
    </button>
  )
})

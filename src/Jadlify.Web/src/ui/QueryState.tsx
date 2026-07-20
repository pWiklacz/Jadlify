import type { ReactNode } from 'react'
import { Button } from './Button'
import { Card } from './Card'

interface LoadingStateProps {
  /** Reassuring, specific copy, e.g. "Wczytujemy Twoje produkty…". */
  label?: string
  /** Number of skeleton lines to show. */
  lines?: number
}

/** Skeleton placeholder announced politely while data loads. */
export function LoadingState({ label = 'Wczytujemy dane…', lines = 3 }: LoadingStateProps) {
  return (
    <Card role="status" aria-live="polite" className="flex flex-col gap-3">
      <p className="text-sm font-medium text-mocha">{label}</p>
      <div className="flex flex-col gap-2.5" aria-hidden="true">
        {Array.from({ length: lines }).map((_, index) => (
          <div
            key={index}
            className="h-3.5 animate-pulse-soft rounded-pill bg-cream-track"
            style={{ width: `${90 - index * 12}%` }}
          />
        ))}
      </div>
    </Card>
  )
}

interface ErrorStateProps {
  title?: string
  description?: ReactNode
  onRetry?: () => void
}

/** Fetch-error card with reassuring copy and a retry affordance. */
export function ErrorState({
  title = 'Nie udało się wczytać danych',
  description = 'Twoje dane są bezpieczne. Spróbuj wczytać je ponownie za chwilę.',
  onRetry,
}: ErrorStateProps) {
  return (
    <Card role="alert" className="flex flex-col items-start gap-3">
      <h2 className="font-serif text-2xl font-normal text-espresso">{title}</h2>
      <p className="text-sm text-mocha">{description}</p>
      {onRetry && (
        <Button variant="ghost" size="sm" onClick={onRetry}>
          Wczytaj ponownie
        </Button>
      )}
    </Card>
  )
}

interface EmptyStateProps {
  title: string
  description?: ReactNode
  /** Decorative glyph shown above the title. */
  icon?: ReactNode
  /** Primary action (e.g. a "Dodaj" button or link). */
  action?: ReactNode
}

/** Empty-collection placeholder: icon, serif title, description and a CTA. */
export function EmptyState({ title, description, icon, action }: EmptyStateProps) {
  return (
    <Card className="flex flex-col items-center gap-3 text-center">
      {icon && (
        <span aria-hidden="true" className="text-3xl text-terracotta">
          {icon}
        </span>
      )}
      <h2 className="font-serif text-2xl font-normal text-espresso">{title}</h2>
      {description && <p className="max-w-prose text-sm text-mocha">{description}</p>}
      {action && <div className="mt-1">{action}</div>}
    </Card>
  )
}

interface QueryStateProps {
  isPending: boolean
  isError: boolean
  /** Renders the empty state instead of children when true and not loading. */
  isEmpty?: boolean
  onRetry?: () => void
  loadingLabel?: string
  errorTitle?: string
  errorDescription?: ReactNode
  empty?: EmptyStateProps
  children: ReactNode
}

/**
 * Renders the standard loading / error / empty / content branches for a
 * TanStack Query-backed section, so every screen handles these states the same
 * way. Data-fetching hooks stay untouched — only their flags flow in here.
 */
export function QueryState({
  isPending,
  isError,
  isEmpty = false,
  onRetry,
  loadingLabel,
  errorTitle,
  errorDescription,
  empty,
  children,
}: QueryStateProps) {
  if (isPending) {
    return <LoadingState label={loadingLabel} />
  }
  if (isError) {
    return (
      <ErrorState title={errorTitle} description={errorDescription} onRetry={onRetry} />
    )
  }
  if (isEmpty && empty) {
    return <EmptyState {...empty} />
  }
  return <>{children}</>
}

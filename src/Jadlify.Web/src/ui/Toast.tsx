import type { ReactNode } from 'react'

export type ToastTone = 'pending' | 'success' | 'info' | 'error'

interface ToastAction {
  label: string
  onClick: () => void
}

interface ToastProps {
  tone?: ToastTone
  message: ReactNode
  /** Optional inline action, e.g. "Cofnij" (undo) or "Spróbuj ponownie". */
  action?: ToastAction
}

const TONE_CLASSES: Record<ToastTone, string> = {
  pending: 'bg-ink-800 text-parchment',
  info: 'bg-ink-800 text-parchment',
  success: 'bg-success text-paper',
  error: 'bg-danger text-paper',
}

/**
 * Bottom-anchored feedback pill. Progress and success/info states use
 * `role="status"` (polite); errors use `role="alert"` (assertive) so failures
 * interrupt. Pending shows a spinner; an optional action (undo / retry) is a
 * single inline button.
 */
export function Toast({ tone = 'info', message, action }: ToastProps) {
  const isError = tone === 'error'
  return (
    <div className="pointer-events-none fixed inset-x-0 bottom-5 z-[60] flex justify-center px-4">
      <div
        role={isError ? 'alert' : 'status'}
        aria-live={isError ? 'assertive' : 'polite'}
        className={[
          'pointer-events-auto flex items-center gap-3 rounded-pill px-5 py-3 text-sm font-medium shadow-toast animate-toast',
          TONE_CLASSES[tone],
        ].join(' ')}
      >
        {tone === 'pending' && (
          <span
            aria-hidden="true"
            className="h-4 w-4 animate-spin-slow rounded-full border-2 border-current border-t-transparent"
          />
        )}
        <span>{message}</span>
        {action && (
          <button
            type="button"
            onClick={action.onClick}
            className="font-semibold underline underline-offset-2"
          >
            {action.label}
          </button>
        )}
      </div>
    </div>
  )
}

import { useEffect, useId, useRef } from 'react'
import { createPortal } from 'react-dom'
import type { KeyboardEvent as ReactKeyboardEvent, ReactNode, RefObject } from 'react'

interface DialogProps {
  open: boolean
  onClose: () => void
  /** Accessible name; wired via `aria-labelledby`. */
  title: ReactNode
  /** Optional supporting text; wired via `aria-describedby`. */
  description?: ReactNode
  /** `dialog` for routine modals, `alertdialog` for destructive confirmations. */
  role?: 'dialog' | 'alertdialog'
  /** Element focused on open; defaults to the first focusable in the panel. */
  initialFocusRef?: RefObject<HTMLElement | null>
  /** Footer actions row (buttons). */
  footer?: ReactNode
  size?: 'md' | 'lg'
  children: ReactNode
}

const FOCUSABLE_SELECTOR =
  'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])'

function getFocusable(container: HTMLElement | null): HTMLElement[] {
  if (!container) {
    return []
  }
  return Array.from(container.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR))
}

/**
 * The single modal contract for the redesign: accessible name, initial focus, a
 * full focus trap, Escape-to-close, an explicit backdrop policy, body scroll
 * lock and focus return to the trigger. Desktop renders a centered card;
 * mobile renders the same dialog as a bottom sheet (CSS only — one DOM tree).
 *
 * `alertdialog` does not dismiss on a backdrop click (destructive confirmations
 * require an explicit choice); `dialog` does.
 */
export function Dialog(props: DialogProps) {
  if (!props.open) {
    return null
  }
  return <DialogPanel {...props} />
}

function DialogPanel({
  onClose,
  title,
  description,
  role = 'dialog',
  initialFocusRef,
  footer,
  size = 'md',
  children,
}: DialogProps) {
  const panelRef = useRef<HTMLDivElement>(null)
  const titleId = useId()
  const descriptionId = useId()
  const dismissOnBackdrop = role === 'dialog'

  // Mounted only while open: capture focus + lock scroll on mount, restore both
  // on unmount (which is how the parent closes the dialog).
  useEffect(() => {
    const previouslyFocused = document.activeElement as HTMLElement | null
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'

    const target = initialFocusRef?.current ?? getFocusable(panelRef.current)[0] ?? panelRef.current
    target?.focus()

    return () => {
      document.body.style.overflow = previousOverflow
      previouslyFocused?.focus()
    }
    // Intentionally run once for the lifetime of the open dialog.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  function handleKeyDown(event: ReactKeyboardEvent<HTMLDivElement>) {
    if (event.key === 'Escape') {
      event.stopPropagation()
      onClose()
      return
    }
    if (event.key !== 'Tab') {
      return
    }
    const focusables = getFocusable(panelRef.current)
    if (focusables.length === 0) {
      event.preventDefault()
      return
    }
    const first = focusables[0]
    const last = focusables[focusables.length - 1]
    const active = document.activeElement
    if (event.shiftKey && active === first) {
      event.preventDefault()
      last.focus()
    } else if (!event.shiftKey && active === last) {
      event.preventDefault()
      first.focus()
    }
  }

  return createPortal(
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-overlay design:items-center design:p-6"
      onMouseDown={(event) => {
        if (dismissOnBackdrop && event.target === event.currentTarget) {
          onClose()
        }
      }}
    >
      <div
        ref={panelRef}
        role={role}
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={description ? descriptionId : undefined}
        tabIndex={-1}
        onKeyDown={handleKeyDown}
        className={[
          'flex max-h-[92vh] w-full flex-col overflow-hidden rounded-t-card bg-cream text-espresso shadow-modal outline-none animate-rise',
          'design:max-h-[85vh] design:rounded-card',
          size === 'lg' ? 'design:max-w-[640px]' : 'design:max-w-[560px]',
        ].join(' ')}
      >
        <div className="flex items-start justify-between gap-4 px-6 pt-6">
          <h2 id={titleId} className="font-serif text-2xl font-normal leading-tight text-espresso">
            {title}
          </h2>
          <button
            type="button"
            aria-label="Zamknij"
            onClick={onClose}
            className="-mr-1.5 -mt-1.5 flex h-11 w-11 flex-none items-center justify-center rounded-full text-xl text-mocha transition-colors hover:bg-cream-hover"
          >
            <span aria-hidden="true">×</span>
          </button>
        </div>

        <div className="overflow-y-auto px-6 py-4">
          {description && (
            <p id={descriptionId} className="mb-4 text-sm text-mocha">
              {description}
            </p>
          )}
          {children}
        </div>

        {footer && (
          <div className="flex flex-wrap justify-end gap-2 border-t border-cream-border px-6 py-4">
            {footer}
          </div>
        )}
      </div>
    </div>,
    document.body,
  )
}

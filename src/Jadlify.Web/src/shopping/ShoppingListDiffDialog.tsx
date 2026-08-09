import type { ReactNode } from 'react'
import { Button } from '../ui/Button'
import { Dialog } from '../ui/Dialog'
import { formatGrams } from '../ui/formatters'
import { ArrowRightIcon } from '../ui/icons'
import type { ShoppingListDiff, ShoppingListDiffLine } from './types'

interface ShoppingListDiffDialogProps {
  diff: ShoppingListDiff
  isSubmitting: boolean
  /**
   * Set after a 409: the plan moved again between this preview and the confirmation,
   * so the shown diff was re-read and needs a fresh confirmation.
   */
  isStale: boolean
  isRefreshingPreview: boolean
  onClose: () => void
  onConfirm: () => void
}

/** Per-section tint: the change's direction is carried by tone *and* by a sign glyph. */
const TONES = {
  added: {
    heading: 'text-success-ink',
    row: 'border-success/25 bg-success/[0.07]',
    sign: 'text-success-ink',
  },
  removed: {
    heading: 'text-danger-ink',
    row: 'border-danger/25 bg-danger/[0.06]',
    sign: 'text-danger-ink',
  },
  changed: {
    heading: 'text-warning-ink',
    row: 'border-warning/30 bg-warning/[0.07]',
    sign: 'text-warning-ink',
  },
  neutral: {
    heading: 'text-mocha',
    row: 'border-cream-border bg-cream-panel',
    sign: 'text-mocha',
  },
} as const

type DiffTone = keyof typeof TONES

/**
 * The refresh confirmation (FR-030). A plan change never rewrites a list behind the
 * user's back: this dialog shows exactly what would be added, removed and re-sized,
 * and only an explicit confirmation applies it — carrying the version and
 * fingerprint of *this* preview, so a diff the user never read cannot be applied.
 *
 * Cancelling writes nothing. Sections with no lines are omitted rather than shown
 * empty, so the dialog reads as a change list, not a form.
 */
export function ShoppingListDiffDialog({
  diff,
  isSubmitting,
  isStale,
  isRefreshingPreview,
  onClose,
  onConfirm,
}: ShoppingListDiffDialogProps) {
  return (
    <Dialog
      open
      onClose={onClose}
      size="lg"
      title="Plan posiłków się zmienił"
      description="Porównanie obecnej listy z aktualnym planem posiłków — nic nie zmieni się bez Twojej zgody."
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={isSubmitting}>
            Zostaw bez zmian
          </Button>
          <Button onClick={onConfirm} isLoading={isSubmitting} disabled={isRefreshingPreview}>
            Zaktualizuj listę
          </Button>
        </>
      }
    >
      <div className="flex flex-col gap-4">
        {isStale && (
          <p
            role="alert"
            className="rounded-field border border-danger/40 bg-danger/10 px-3.5 py-2.5 text-[13px] text-danger"
          >
            Twój plan posiłków zmienił się ponownie od wygenerowania tej listy zmian. Poniżej są
            już nowe zmiany — potwierdź je jeszcze raz.
          </p>
        )}

        {isRefreshingPreview && (
          <p role="status" className="text-[13px] text-mocha">
            Wczytujemy aktualne zmiany…
          </p>
        )}

        <DiffSection
          heading="Dojdą nowe produkty"
          tone="added"
          sign="+"
          lines={diff.added}
          renderAmount={(line) => (
            <Amount value={formatGrams(line.newGrams ?? 0)} emphasis />
          )}
        />

        <DiffSection
          heading="Znikną z listy"
          tone="removed"
          sign="−"
          strikeName
          lines={diff.removed}
          renderAmount={(line) => <Amount value={formatGrams(line.previousGrams ?? 0)} muted />}
        />

        <DiffSection
          heading="Zmieni się ilość"
          tone="changed"
          lines={diff.changed}
          renderAmount={(line) => {
            const from = formatGrams(line.previousGrams ?? 0)
            const to = formatGrams(line.newGrams ?? 0)
            return (
              <span className="flex flex-none items-baseline gap-2">
                <span className="sr-only">
                  z {from} na {to}
                </span>
                <span
                  aria-hidden="true"
                  className="tabular-nums text-mocha line-through decoration-faint"
                >
                  {from}
                </span>
                <ArrowRightIcon size={12} className="self-center text-warning-ink" />
                <span aria-hidden="true" className="font-bold tabular-nums">
                  {to}
                </span>
              </span>
            )
          }}
        />

        <DiffSection
          heading="Bez zmiany ilości, inne posiłki"
          tone="neutral"
          lines={diff.sourceOnly}
          note="Ilość zostaje taka sama — zmieniły się tylko posiłki, z których pochodzi. Odhaczenia zostaną zachowane."
          renderAmount={(line) => (
            <Amount value={formatGrams(line.newGrams ?? line.previousGrams ?? 0)} emphasis />
          )}
        />

        <p className="rounded-field border border-cream-border bg-cream-panel px-3.5 py-[11px] text-[13px] leading-relaxed text-label">
          <b className="text-espresso">Odhaczenia zostaną zachowane</b> wszędzie tam, gdzie
          ilość się nie zmienia. Produkty o zmienionej ilości i nowe produkty zostaną
          odznaczone, żeby nic Ci nie umknęło.
        </p>
      </div>
    </Dialog>
  )
}

function Amount({ value, emphasis, muted }: { value: string; emphasis?: boolean; muted?: boolean }) {
  return (
    <span
      className={[
        'flex-none tabular-nums',
        emphasis ? 'font-bold' : '',
        muted ? 'text-mocha' : '',
      ]
        .filter(Boolean)
        .join(' ')}
    >
      {value}
    </span>
  )
}

interface DiffSectionProps {
  heading: string
  tone: DiffTone
  /** Optional leading glyph (`+` / `−`) so direction is not carried by colour alone. */
  sign?: string
  strikeName?: boolean
  lines: ShoppingListDiffLine[]
  renderAmount: (line: ShoppingListDiffLine) => ReactNode
  note?: string
}

function DiffSection({
  heading,
  tone,
  sign,
  strikeName = false,
  lines,
  renderAmount,
  note,
}: DiffSectionProps) {
  if (lines.length === 0) {
    return null
  }
  const classes = TONES[tone]

  return (
    <section aria-label={heading}>
      <h3
        className={`mb-2 text-[11px] font-bold uppercase tracking-eyebrow ${classes.heading}`}
      >
        {heading}
      </h3>
      {note && <p className="mb-2 text-[12.5px] leading-relaxed text-mocha">{note}</p>}
      <ul className="flex flex-col gap-1">
        {lines.map((line) => (
          <li
            key={line.productId}
            className={`flex flex-wrap items-baseline gap-2 rounded-field border px-3.5 py-2.5 text-[13.5px] ${classes.row}`}
          >
            {sign && (
              <span aria-hidden="true" className={`font-extrabold ${classes.sign}`}>
                {sign}
              </span>
            )}
            <span
              className={[
                'min-w-0 break-name font-semibold',
                strikeName ? 'line-through decoration-faint' : '',
              ]
                .filter(Boolean)
                .join(' ')}
            >
              {line.productName}
            </span>
            <span
              aria-hidden="true"
              className="min-w-[14px] flex-1 self-center border-b-2 border-dotted border-cream-line"
            />
            {renderAmount(line)}
          </li>
        ))}
      </ul>
    </section>
  )
}

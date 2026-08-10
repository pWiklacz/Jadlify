export type StatusTone = 'neutral' | 'success' | 'warning' | 'danger' | 'info'

interface StatusPillProps {
  /** Short, typically uppercase status text (e.g. "ZAKUPY W TOKU"). */
  label: string
  tone?: StatusTone
}

const TONE_CLASSES: Record<StatusTone, { text: string; dot: string }> = {
  neutral: { text: 'text-parchment/70', dot: 'bg-parchment/40' },
  success: { text: 'text-success-onDark', dot: 'bg-success-dot' },
  warning: { text: 'text-warning-onDark', dot: 'bg-warning' },
  danger: { text: 'text-terracotta', dot: 'bg-terracotta-strong' },
  info: { text: 'text-terracotta', dot: 'bg-terracotta' },
}

/**
 * The contextual status pill shown in the app shell header (and reused on
 * pages). A colored dot plus a short uppercase label communicates state with
 * both text and color — never color alone.
 */
export function StatusPill({ label, tone = 'neutral' }: StatusPillProps) {
  const classes = TONE_CLASSES[tone]
  return (
    <span
      className={[
        'inline-flex items-center gap-2 rounded-pill border border-parchment/15 px-3 py-1.5 text-[11px] font-bold tracking-eyebrow',
        classes.text,
      ].join(' ')}
    >
      <span aria-hidden="true" className={`h-[7px] w-[7px] rounded-full ${classes.dot}`} />
      {label}
    </span>
  )
}

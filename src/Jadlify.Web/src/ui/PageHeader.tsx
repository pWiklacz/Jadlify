import type { ReactNode } from 'react'

interface PageHeaderProps {
  /** Serif page title. Rendered as the page's single `<h1>`. */
  title: ReactNode
  /** Optional supporting line under the title. */
  subtitle?: ReactNode
  /** Optional trailing actions (buttons), right-aligned on desktop. */
  actions?: ReactNode
}

/**
 * Standard page heading on the dark shell: a serif `<h1>` with an optional
 * subtitle and trailing actions. Gives every screen a consistent title block.
 */
export function PageHeader({ title, subtitle, actions }: PageHeaderProps) {
  return (
    <header className="mb-6 flex flex-col gap-3 design:flex-row design:items-end design:justify-between">
      <div className="flex flex-col gap-1">
        <h1 className="font-serif text-3xl font-normal leading-tight text-parchment design:text-4xl">
          {title}
        </h1>
        {subtitle && <p className="text-sm text-parchment/55">{subtitle}</p>}
      </div>
      {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
    </header>
  )
}

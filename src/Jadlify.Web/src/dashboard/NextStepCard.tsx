import { Link } from 'react-router-dom'
import { Button } from '../ui/Button'
import type { NextStep } from './nextStep'

interface NextStepCardProps {
  step: NextStep
  /** Runs when the step is handled in place (`step.to === null`), e.g. adding a meal. */
  onAction: () => void
}

/**
 * The single contextual next step. One card, never a checklist: the step already
 * accounts for everything the user has done, so a list of ticked boxes would add
 * noise without adding a decision.
 */
export function NextStepCard({ step, onAction }: NextStepCardProps) {
  return (
    <section
      aria-label="Następny krok"
      className="rounded-panel border border-cream-border bg-cream-panel px-[18px] py-4"
    >
      <p className="text-[11px] font-bold uppercase tracking-eyebrow text-mocha">Następny krok</p>
      <h3 className="mt-1 font-serif text-xl font-normal text-espresso">{step.title}</h3>
      <p className="mt-1.5 text-[13px] leading-relaxed text-mocha">{step.description}</p>
      <div className="mt-3.5">
        {step.to === null ? (
          <Button size="sm" onClick={onAction}>
            {step.actionLabel}
          </Button>
        ) : (
          <Link
            to={step.to}
            className="inline-flex min-h-[44px] items-center rounded-pill bg-terracotta-strong px-[18px] text-[13px] font-semibold text-paper transition-colors hover:bg-terracotta-hover"
          >
            {step.actionLabel}
          </Link>
        )}
      </div>
    </section>
  )
}

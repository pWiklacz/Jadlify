import { useMe } from '../api/useMe'
import { PageHeader } from '../ui/PageHeader'
import { Card } from '../ui/Card'

/**
 * Post-login home. A light placeholder within the redesigned shell — the full
 * interactive day dashboard arrives in a later phase. For now it still proves
 * the protected `/api/me` bearer round-trip works end to end.
 */
export function LandingPage() {
  const { data, isPending, isError, error } = useMe()

  return (
    <section>
      <PageHeader
        title="Strona główna"
        subtitle="Twój panel dnia pojawi się tutaj wkrótce."
      />

      <Card className="flex flex-col gap-3">
        <p className="text-espresso">
          Zacznij od dodania produktów i przepisów, ustaw dzienne cele, a potem
          zaplanuj posiłki i utwórz listę zakupów.
        </p>

        {isPending && (
          <p role="status" aria-live="polite" className="text-sm text-mocha">
            Sprawdzamy Twoją sesję…
          </p>
        )}

        {isError && (
          <p role="alert" className="text-sm text-danger">
            Nie udało się połączyć z API
            {error instanceof Error ? `: ${error.message}` : ''}.
          </p>
        )}

        {data && (
          <p className="text-sm text-mocha">
            Zalogowano jako{' '}
            <span className="font-mono font-semibold text-espresso">{data.userId}</span>
          </p>
        )}
      </Card>
    </section>
  )
}

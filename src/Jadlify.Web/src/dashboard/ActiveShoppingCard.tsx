import { Link } from 'react-router-dom'
import { formatCount } from '../ui/formatters'
import { ShoppingProgress } from '../shopping/ShoppingProgress'
import { sourceDaysLabel } from '../shopping/shoppingFormat'
import type { ShoppingListSummary } from '../shopping/types'

interface ActiveShoppingCardProps {
  /** The single active list, or null when nothing is in progress. */
  list: ShoppingListSummary | null
}

/**
 * The dashboard's shopping slot. Reads the same index summary the shopping screen
 * does — the counts are already on the wire, so showing progress here costs no
 * extra request and can never disagree with the list itself.
 */
export function ActiveShoppingCard({ list }: ActiveShoppingCardProps) {
  return (
    <section
      aria-label="Lista zakupów"
      className="rounded-panel border border-cream-border bg-cream-panel px-[18px] py-4"
    >
      <h3 className="font-serif text-xl font-normal text-espresso">Lista zakupów</h3>

      {list ? (
        <div className="mt-2.5 flex flex-col gap-3">
          <p className="text-[13px] text-mocha">
            <b className="text-espresso">{list.name}</b> ·{' '}
            {sourceDaysLabel(list.sourceDays)} ·{' '}
            {formatCount(list.itemCount, 'produkt', 'produkty', 'produktów')}
          </p>
          <ShoppingProgress
            bought={list.boughtCount}
            total={list.itemCount}
            label={`Postęp listy „${list.name}”`}
          />
          <Link
            to={`/shopping-list/${list.id}`}
            className="inline-flex min-h-[44px] w-fit items-center rounded-pill border border-terracotta/55 px-[18px] text-[13px] font-semibold text-terracotta transition-colors hover:bg-terracotta/10"
          >
            Pokaż listę
          </Link>
        </div>
      ) : (
        <div className="mt-2.5 flex flex-col gap-3">
          <p className="text-[13px] leading-relaxed text-mocha">
            Nie masz aktywnej listy zakupów. Wybierz dni z planu, a złożymy z nich listę
            produktów.
          </p>
          <Link
            to="/shopping-list"
            className="inline-flex min-h-[44px] w-fit items-center rounded-pill border border-terracotta/55 px-[18px] text-[13px] font-semibold text-terracotta transition-colors hover:bg-terracotta/10"
          >
            Utwórz listę zakupów
          </Link>
        </div>
      )}
    </section>
  )
}

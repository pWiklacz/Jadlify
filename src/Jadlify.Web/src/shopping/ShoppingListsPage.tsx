import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Button } from '../ui/Button'
import { Card } from '../ui/Card'
import { EmptyState, QueryState } from '../ui/QueryState'
import { Toast, type ToastTone } from '../ui/Toast'
import { formatCount } from '../ui/formatters'
import { ArrowRightIcon, CartIcon, PlusIcon } from '../ui/icons'
import { CreateShoppingListDialog } from './CreateShoppingListDialog'
import { ShoppingProgress } from './ShoppingProgress'
import {
  activeListCtaLabel,
  formatTimestamp,
  sourceDaysLabel,
  statusLabel,
} from './shoppingFormat'
import { isConflict, useCreateShoppingList } from './useShoppingListMutations'
import { useShoppingListIndex } from './useShoppingLists'
import type { ShoppingListSummary } from './types'

/**
 * The shopping index (S-06): the one active list plus the completed history.
 *
 * Lists are persistent aggregates, so this screen is a directory rather than a
 * generated view — the active card is the way back into shopping in progress, and
 * history keeps finished lists readable as frozen snapshots. Creating a list is
 * gated to one active at a time, which the API enforces with a 409 the dialog
 * surfaces.
 *
 * The active list is the one cream card on the page; history stays on the dark
 * shell as quiet rows, so the thing to act on is never competing with the archive.
 */
export function ShoppingListsPage() {
  const [creating, setCreating] = useState(false)
  const [toast, setToast] = useState<{ tone: ToastTone; message: string } | null>(null)

  const index = useShoppingListIndex()
  const createList = useCreateShoppingList()

  const active = index.data?.active ?? null
  const history = index.data?.history ?? []
  const isEmpty = !active && history.length === 0

  function submitCreate(name: string, days: string[]) {
    createList.mutate(
      { name, days },
      {
        onSuccess: () => {
          setCreating(false)
          setToast({ tone: 'success', message: 'Utworzono listę zakupów.' })
        },
        onError: (error) => {
          setCreating(false)
          setToast({
            tone: 'error',
            message: isConflict(error)
              ? 'Masz już aktywną listę zakupów. Ukończ ją, zanim utworzysz nową.'
              : 'Nie udało się utworzyć listy. Spróbuj ponownie.',
          })
        },
      },
    )
  }

  return (
    <section>
      <header className="mb-6 flex flex-wrap items-end gap-x-5 gap-y-3.5">
        <div className="min-w-[230px]">
          <h1 className="font-serif text-[clamp(30px,3.4vw,40px)] font-normal leading-[1.05] text-parchment">
            Listy zakupów
          </h1>
          <p className="mt-1.5 max-w-[520px] text-sm leading-relaxed text-parchment/55">
            Trwałe listy tworzone z zaplanowanych posiłków — na zakupy raz na tydzień lub
            dwa.
          </p>
        </div>
        <div className="flex-1" />
        {/*
          Creating is offered here and from the empty state only: one list can be
          active at a time, so a single "Utwórz nową listę" control keeps the
          accessible name unambiguous. The empty state owns its own CTA, so the
          header action steps aside there.
        */}
        {!isEmpty && !index.isPending && !index.isError && (
          <Button onClick={() => setCreating(true)}>
            <PlusIcon size={14} />
            Utwórz nową listę
          </Button>
        )}
      </header>

      <QueryState
        isPending={index.isPending}
        isError={index.isError}
        onRetry={() => void index.refetch()}
        loadingLabel="Wczytujemy Twoje listy zakupów…"
        errorTitle="Nie udało się pobrać list zakupów"
        errorDescription="Twoje listy są bezpieczne. Sprawdź połączenie i spróbuj ponownie."
      >
        {isEmpty ? (
          <div className="mx-auto mt-7 max-w-[560px]">
            <EmptyState
              title="Nie masz jeszcze listy zakupów"
              description="Lista zakupów powstaje z Twoich zaplanowanych posiłków: wybierasz dni z planu, a Jadlify sumuje składniki w jedną listę do odhaczania w sklepie."
              icon={<CartIcon size={38} className="text-terracotta-strong" />}
              action={
                <div className="flex flex-col items-center gap-3.5">
                  <Button onClick={() => setCreating(true)}>Utwórz pierwszą listę</Button>
                  <p className="text-[12.5px] text-mocha">
                    Najpierw dodaj posiłki w{' '}
                    <Link to="/meal-plan" className="font-semibold text-terracotta-strong">
                      planie posiłków
                    </Link>
                    , jeśli plan jest pusty.
                  </p>
                </div>
              }
            />
          </div>
        ) : (
          <div className="flex flex-col">
            <section aria-label="Aktywna lista">
              {active ? (
                <ActiveListCard list={active} />
              ) : (
                <div className="flex flex-wrap items-center gap-x-5 gap-y-3 rounded-panel border border-parchment/10 bg-parchment/[0.06] px-6 py-5">
                  <div className="flex-1 basis-[280px]">
                    <h2 className="text-[15.5px] font-bold text-parchment">
                      Brak aktywnej listy
                    </h2>
                    <p className="mt-1 text-[13.5px] leading-relaxed text-parchment/55">
                      Zaplanuj posiłki na najbliższe dni i utwórz z nich nową listę zakupów.
                    </p>
                  </div>
                  <Button onClick={() => setCreating(true)}>Utwórz nową listę</Button>
                </div>
              )}
            </section>

            {history.length > 0 && (
              <section aria-label="Poprzednie listy">
                <h2 className="mb-3 mt-[30px] font-serif text-[23px] font-normal text-parchment">
                  Poprzednie listy
                </h2>
                <div className="flex flex-col gap-2.5">
                  {history.map((list) => (
                    <HistoryRow key={list.id} list={list} />
                  ))}
                </div>
              </section>
            )}
          </div>
        )}
      </QueryState>

      {creating && (
        <CreateShoppingListDialog
          isSubmitting={createList.isPending}
          onClose={() => setCreating(false)}
          onSubmit={submitCreate}
        />
      )}

      {toast && <Toast tone={toast.tone} message={toast.message} />}
    </section>
  )
}

/** The active list as the page's headline card: status, progress and the way back in. */
function ActiveListCard({ list }: { list: ShoppingListSummary }) {
  return (
    <Card elevated className="border border-black/5">
      <div className="flex flex-wrap items-center gap-x-3 gap-y-2">
        <span className="rounded-pill border border-terracotta-strong/35 bg-terracotta/15 px-3 py-[5px] text-[10.5px] font-bold tracking-eyebrow text-terracotta-strong">
          {statusLabel(list.status)}
        </span>
        <span className="flex-1" />
        <span className="text-[12.5px] text-mocha">
          Utworzona {formatTimestamp(list.createdAt)}
        </span>
      </div>

      <h2 className="mb-1 mt-3 break-name font-serif text-[clamp(25px,3vw,32px)] font-normal leading-[1.1] text-espresso">
        {list.name}
      </h2>
      <p className="text-[13.5px] text-mocha">
        {sourceDaysLabel(list.sourceDays)} ·{' '}
        {formatCount(list.itemCount, 'produkt', 'produkty', 'produktów')}
      </p>

      <div className="mt-[18px] flex flex-wrap items-center gap-x-7 gap-y-4">
        <div className="min-w-0 flex-1 basis-[260px]">
          <ShoppingProgress
            bought={list.boughtCount}
            total={list.itemCount}
            label={`Postęp listy „${list.name}”`}
          />
        </div>
        <Link
          to={`/shopping-list/${list.id}`}
          className="inline-flex min-h-12 flex-none items-center gap-2.5 rounded-pill bg-terracotta-strong px-6 text-[14.5px] font-semibold text-paper transition-colors hover:bg-terracotta-hover"
        >
          {activeListCtaLabel(list.boughtCount, list.itemCount)}
          <ArrowRightIcon size={14} />
        </Link>
      </div>
    </Card>
  )
}

/** One archived list: a quiet dark row, readable at a glance and still openable. */
function HistoryRow({ list }: { list: ShoppingListSummary }) {
  return (
    <Link
      to={`/shopping-list/${list.id}`}
      className="flex flex-wrap items-center gap-x-4 gap-y-2.5 rounded-panel border border-parchment/10 bg-parchment/[0.06] px-5 py-4 transition-colors hover:bg-parchment/10"
    >
      <span className="min-w-0 flex-1 basis-[240px]">
        <span className="block break-name text-[15px] font-bold text-parchment">
          {list.name}
        </span>
        <span className="mt-[3px] block text-[12.5px] tabular-nums text-parchment/55">
          {sourceDaysLabel(list.sourceDays)} ·{' '}
          {formatCount(list.itemCount, 'produkt', 'produkty', 'produktów')} ·{' '}
          {list.boughtCount} z {list.itemCount} kupione
        </span>
      </span>
      <span className="flex-none rounded-pill border border-success-dot/30 bg-success-dot/10 px-[11px] py-1 text-[10px] font-bold tracking-[0.14em] text-success-onDark">
        {statusLabel(list.status)}
      </span>
      {list.completedAt && (
        <span className="flex-none text-[12.5px] tabular-nums text-parchment/45">
          {formatTimestamp(list.completedAt)}
        </span>
      )}
    </Link>
  )
}

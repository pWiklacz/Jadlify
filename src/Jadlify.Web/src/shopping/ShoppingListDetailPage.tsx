import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { Button } from '../ui/Button'
import { Card } from '../ui/Card'
import { EmptyState, QueryState } from '../ui/QueryState'
import { Toast, type ToastTone } from '../ui/Toast'
import { formatCount } from '../ui/formatters'
import {
  CheckIcon,
  ChevronLeftIcon,
  CloseIcon,
  SearchEmptyIcon,
  SearchIcon,
  WarningIcon,
} from '../ui/icons'
import { ShoppingItemsView } from './ShoppingItemsView'
import { ShoppingListDiffDialog } from './ShoppingListDiffDialog'
import { ShoppingProgress } from './ShoppingProgress'
import { ShoppingSourceView } from './ShoppingSourceView'
import {
  ITEMS_ORDERS,
  defaultItemsOrder,
  formatTimestamp,
  orderItems,
  progressPercent,
  searchItems,
  sourceDaysLabel,
  statusLabel,
  type ItemsOrder,
} from './shoppingFormat'
import {
  isConflict,
  useCompleteShoppingList,
  useRefreshShoppingList,
  useToggleShoppingListItem,
} from './useShoppingListMutations'
import { useShoppingListDetail, useShoppingListDiff } from './useShoppingLists'
import type { ShoppingListItem } from './types'

type ListView = 'items' | 'meal' | 'day'

const VIEWS: readonly { value: ListView; label: string }[] = [
  { value: 'items', label: 'Zakupy' },
  { value: 'meal', label: 'Wg posiłków' },
  { value: 'day', label: 'Wg dni' },
] as const

/**
 * One shopping list (S-06 detail). The same payload drives three groupings, so
 * switching views never refetches; only ticking, refreshing and completing write.
 *
 * A completed list is deliberately inert — it is the frozen record of a shop that
 * already happened, so its checkboxes are read-only and it is never offered a
 * refresh. An active list watches for plan drift through a single diff preview and
 * surfaces it as a banner the user must act on explicitly.
 *
 * Layout follows the mockup's "shopping mode": the summary card scrolls away while
 * the view switcher, search and progress stay pinned, because the controls are what
 * a user reaches for repeatedly with a trolley in the other hand.
 */
export function ShoppingListDetailPage() {
  const { id } = useParams<{ id: string }>()
  const [view, setView] = useState<ListView>('items')
  const [search, setSearch] = useState('')
  const [order, setOrder] = useState<ItemsOrder | null>(null)
  const [hidePurchased, setHidePurchased] = useState(false)
  const [diffOpen, setDiffOpen] = useState(false)
  const [diffStale, setDiffStale] = useState(false)
  const [toast, setToast] = useState<{ tone: ToastTone; message: string } | null>(null)

  const detail = useShoppingListDetail(id)
  const list = detail.data
  const isActive = list?.status === 'Active'

  const diff = useShoppingListDiff(id, Boolean(isActive))
  const toggleItem = useToggleShoppingListItem(id ?? '')
  const refreshList = useRefreshShoppingList(id ?? '')
  const completeList = useCompleteShoppingList(id ?? '')

  function handleToggle(item: ShoppingListItem, isBought: boolean) {
    if (!list) {
      return
    }
    toggleItem.mutate(
      { itemId: item.id, isBought, expectedVersion: list.version },
      {
        onError: (error) =>
          setToast({
            tone: 'error',
            message: isConflict(error)
              ? 'Lista zmieniła się w międzyczasie. Odświeżyliśmy ją — zaznacz produkt ponownie.'
              : 'Nie udało się zapisać zaznaczenia. Spróbuj ponownie.',
          }),
        onSettled: (_data, error) => {
          if (error && isConflict(error)) {
            void detail.refetch()
          }
        },
      },
    )
  }

  function confirmRefresh() {
    if (!diff.data) {
      return
    }
    refreshList.mutate(
      {
        expectedVersion: diff.data.listVersion,
        expectedSourceFingerprint: diff.data.sourceFingerprint,
      },
      {
        onSuccess: () => {
          setDiffOpen(false)
          setDiffStale(false)
          setToast({ tone: 'success', message: 'Zaktualizowano listę zakupów.' })
        },
        onError: (error) => {
          if (isConflict(error)) {
            // The plan moved again between preview and confirmation: re-read the
            // diff and keep the dialog open so the user confirms what is true now.
            setDiffStale(true)
            void diff.refetch()
            return
          }
          setToast({ tone: 'error', message: 'Nie udało się zaktualizować listy.' })
        },
      },
    )
  }

  function handleComplete() {
    if (!list) {
      return
    }
    completeList.mutate(
      { expectedVersion: list.version },
      {
        onSuccess: () => setToast({ tone: 'success', message: 'Lista została ukończona.' }),
        onError: (error) =>
          setToast({
            tone: 'error',
            message: isConflict(error)
              ? 'Lista zmieniła się w międzyczasie. Odśwież ją i spróbuj ponownie.'
              : 'Nie udało się ukończyć listy.',
          }),
      },
    )
  }

  const items = list?.items ?? []
  const boughtCount = items.filter((item) => item.isBought).length
  const allBought = items.length > 0 && boughtCount === items.length
  const showDiffBanner = Boolean(isActive && diff.data?.hasChanges)

  const effectiveOrder = order ?? defaultItemsOrder(items)
  const matching = searchItems(items, search)
  const hiddenCount = hidePurchased ? matching.filter((item) => item.isBought).length : 0
  const visible = hidePurchased ? matching.filter((item) => !item.isBought) : matching
  const groups = orderItems(visible, effectiveOrder)
  const noResults = search.trim() !== '' && groups.length === 0

  return (
    <section>
      <Link
        to="/shopping-list"
        className="mb-2 inline-flex items-center gap-[7px] py-2 text-[13px] font-semibold text-parchment/60 transition-colors hover:text-parchment"
      >
        <ChevronLeftIcon size={14} />
        Wszystkie listy
      </Link>

      <QueryState
        isPending={detail.isPending}
        isError={detail.isError}
        onRetry={() => void detail.refetch()}
        loadingLabel="Wczytujemy Twoją listę zakupów…"
        errorTitle="Nie udało się pobrać listy zakupów"
        errorDescription="Twoje odhaczenia są bezpieczne. Sprawdź połączenie i spróbuj ponownie."
      >
        {list && (
          <>
            <Card elevated>
              <div className="flex flex-wrap items-center gap-x-3 gap-y-2">
                <span
                  className={[
                    'rounded-pill border px-3 py-[5px] text-[10.5px] font-bold tracking-eyebrow',
                    isActive && !allBought
                      ? 'border-terracotta-strong/35 bg-terracotta/15 text-terracotta-strong'
                      : 'border-success/35 bg-success/[0.12] text-success-ink',
                  ].join(' ')}
                >
                  {isActive && allBought ? 'GOTOWA DO ZAMKNIĘCIA' : statusLabel(list.status)}
                </span>
                <span className="flex-1" />
                <span className="text-[12.5px] text-mocha">
                  Utworzona {formatTimestamp(list.createdAt)}
                  {list.completedAt && ` · ukończona ${formatTimestamp(list.completedAt)}`}
                </span>
                {/*
                  The mockup closes a list from the "everything bought" banner, but a
                  shop that ended early still has to be closeable — so the same action
                  stays available here as a quiet secondary until that banner appears.
                */}
                {isActive && !allBought && items.length > 0 && (
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={handleComplete}
                    isLoading={completeList.isPending}
                  >
                    Oznacz listę jako ukończoną
                  </Button>
                )}
              </div>

              <h1 className="mb-1 mt-2.5 break-name font-serif text-[clamp(26px,3vw,34px)] font-normal leading-[1.1] text-espresso">
                {list.name}
              </h1>
              <p className="text-[13.5px] text-mocha">
                {sourceDaysLabel(list.sourceDays)} ·{' '}
                {formatCount(items.length, 'produkt', 'produkty', 'produktów')}
              </p>

              {showDiffBanner && (
                <div
                  role="status"
                  className="mt-3.5 flex flex-wrap items-center gap-2.5 rounded-panel border border-warning/40 bg-warning/[0.12] px-3.5 py-[11px] text-[13px] text-warning-ink"
                >
                  <WarningIcon size={15} className="flex-none" />
                  <p className="min-w-0 flex-1 basis-[220px] leading-relaxed">
                    <b>Twój plan posiłków zmienił się</b> od wygenerowania tej listy. Lista
                    pozostaje bez zmian, dopóki jej nie zaktualizujesz.
                  </p>
                  <button
                    type="button"
                    onClick={() => setDiffOpen(true)}
                    className="min-h-10 flex-none rounded-pill border border-warning-ink/50 px-4 text-[12.5px] font-semibold text-warning-ink transition-colors hover:bg-warning/15"
                  >
                    Przejrzyj zmiany
                  </button>
                </div>
              )}

              <div className="mt-4">
                <ShoppingProgress
                  variant="hero"
                  bought={boughtCount}
                  total={items.length}
                  label={`Postęp listy „${list.name}”`}
                />
              </div>

              {isActive && allBought && (
                <div className="mt-4 flex flex-wrap items-center gap-2.5 rounded-panel border border-success/35 bg-success/10 px-4 py-3.5">
                  <CheckIcon size={18} className="flex-none text-success" />
                  <p className="flex-1 basis-[200px] text-sm font-bold text-success-ink">
                    Wszystko kupione — świetna robota!
                  </p>
                  <Button
                    variant="success"
                    size="sm"
                    onClick={handleComplete}
                    isLoading={completeList.isPending}
                    className="flex-none"
                  >
                    Oznacz listę jako ukończoną
                  </Button>
                </div>
              )}
            </Card>

            {items.length === 0 ? (
              <div className="mt-4">
                <EmptyState
                  title="Ta lista jest pusta"
                  description="W wybranych dniach nie ma jeszcze posiłków z produktami. Zaplanuj posiłki, a potem zaktualizuj listę."
                  action={
                    <Link
                      to="/meal-plan"
                      className="inline-flex min-h-11 items-center rounded-pill bg-terracotta-strong px-5 text-[15px] font-semibold text-paper transition-colors hover:bg-terracotta-hover"
                    >
                      Przejdź do planu posiłków
                    </Link>
                  }
                />
              </div>
            ) : (
              <>
                {/*
                  Pinned under the app header (whose height the shell publishes as
                  `--app-header-h`), so the controls and the running count stay
                  reachable however far down the list the user has scrolled.
                */}
                <div className="sticky top-[var(--app-header-h,0px)] z-20 mb-1 mt-3.5 border-b border-parchment/10 bg-ink pb-3 pt-2.5">
                  <div className="design:hidden">
                    <div className="mb-2.5 flex items-center gap-2.5">
                      <span className="min-w-0 flex-1 truncate text-[13px] font-bold text-parchment">
                        {list.name}
                      </span>
                      <span className="flex-none text-[12.5px] font-bold tabular-nums text-success-onDark">
                        {boughtCount} / {items.length}
                      </span>
                    </div>
                    <div
                      aria-hidden="true"
                      className="mb-2.5 h-[5px] overflow-hidden rounded-pill bg-parchment/10"
                    >
                      <div
                        className="h-full rounded-pill bg-success-dot transition-[width] duration-300"
                        style={{ width: `${progressPercent(boughtCount, items.length)}%` }}
                      />
                    </div>
                  </div>

                  <div className="flex flex-wrap items-center gap-2">
                    <div
                      role="group"
                      aria-label="Widok listy"
                      className="flex flex-none items-center gap-0.5 rounded-pill border border-parchment/15 p-[3px]"
                    >
                      {VIEWS.map((option) => {
                        const selected = option.value === view
                        return (
                          <button
                            key={option.value}
                            type="button"
                            aria-pressed={selected}
                            onClick={() => setView(option.value)}
                            className={[
                              'min-h-[38px] rounded-pill px-3.5 text-[13px] transition-colors',
                              selected
                                ? 'bg-parchment/15 font-bold text-parchment'
                                : 'font-medium text-parchment/55 hover:text-parchment',
                            ].join(' ')}
                          >
                            {option.label}
                          </button>
                        )
                      })}
                    </div>

                    {view === 'items' ? (
                      <>
                        <div className="relative min-w-[150px] flex-1 basis-[170px]">
                          <SearchIcon
                            size={15}
                            className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-parchment/50"
                          />
                          <input
                            type="search"
                            value={search}
                            onChange={(event) => setSearch(event.currentTarget.value)}
                            placeholder="Szukaj produktu…"
                            aria-label="Szukaj produktu na liście"
                            className="min-h-11 w-full rounded-pill border border-parchment/15 bg-parchment/5 px-[38px] text-[13.5px] text-parchment outline-none transition-colors placeholder:text-parchment/40 focus:border-terracotta"
                          />
                          {search !== '' && (
                            <button
                              type="button"
                              onClick={() => setSearch('')}
                              aria-label="Wyczyść wyszukiwanie"
                              className="absolute right-1 top-1/2 flex h-9 w-9 -translate-y-1/2 items-center justify-center rounded-full text-parchment/60 transition-colors hover:text-parchment"
                            >
                              <CloseIcon size={12} />
                            </button>
                          )}
                        </div>

                        <select
                          value={effectiveOrder}
                          onChange={(event) =>
                            setOrder(event.currentTarget.value as ItemsOrder)
                          }
                          aria-label="Sortowanie listy"
                          className="min-h-11 flex-none rounded-pill border border-parchment/15 bg-ink px-3 text-[13px] font-semibold text-parchment"
                        >
                          {ITEMS_ORDERS.map((option) => (
                            <option key={option.value} value={option.value}>
                              {option.label}
                            </option>
                          ))}
                        </select>

                        <button
                          type="button"
                          aria-pressed={hidePurchased}
                          onClick={() => setHidePurchased((previous) => !previous)}
                          className={[
                            'min-h-11 flex-none rounded-pill border px-4 text-[13px] font-semibold transition-colors',
                            hidePurchased
                              ? 'border-terracotta bg-terracotta/15 text-terracotta'
                              : 'border-parchment/15 text-parchment/65 hover:text-parchment',
                          ].join(' ')}
                        >
                          {hidePurchased ? 'Kupione ukryte' : 'Ukryj kupione'}
                        </button>
                      </>
                    ) : (
                      <p className="flex-1 basis-[240px] text-[12.5px] leading-relaxed text-parchment/50">
                        Ten widok pokazuje, skąd pochodzą ilości. Produkty odhaczaj w widoku
                        „Zakupy”.
                      </p>
                    )}
                  </div>
                </div>

                <div className="mt-3 flex flex-col gap-3.5">
                  {view === 'items' ? (
                    <>
                      <ShoppingItemsView
                        groups={groups}
                        readOnly={!isActive}
                        busy={refreshList.isPending || completeList.isPending}
                        showCategoryTag={effectiveOrder !== 'category'}
                        onToggle={handleToggle}
                      />

                      {hiddenCount > 0 && (
                        <p className="flex items-center justify-center gap-2 py-1 text-[13px] text-parchment/50">
                          Ukryto{' '}
                          {formatCount(
                            hiddenCount,
                            'kupiony produkt',
                            'kupione produkty',
                            'kupionych produktów',
                          )}
                          .
                          <button
                            type="button"
                            onClick={() => setHidePurchased(false)}
                            className="p-2 text-[13px] font-bold text-terracotta transition-colors hover:text-terracotta-strong"
                          >
                            Pokaż
                          </button>
                        </p>
                      )}

                      {noResults && (
                        <div className="rounded-card border-[1.5px] border-dashed border-cream-line bg-cream px-5 py-[26px] text-center text-espresso">
                          <SearchEmptyIcon
                            size={26}
                            className="mx-auto mb-2 text-muted"
                          />
                          <p className="text-[14.5px] font-bold">
                            Brak produktów pasujących do „{search.trim()}”
                          </p>
                          <p className="mx-auto mb-3.5 mt-1 max-w-[340px] text-[13px] leading-relaxed text-label">
                            Sprawdź pisownię albo wyczyść wyszukiwanie, aby zobaczyć całą
                            listę.
                          </p>
                          <button
                            type="button"
                            onClick={() => setSearch('')}
                            className="min-h-11 rounded-pill border border-cream-border px-[18px] text-[13.5px] font-semibold text-label transition-colors hover:bg-cream-hover"
                          >
                            Wyczyść wyszukiwanie
                          </button>
                        </div>
                      )}
                    </>
                  ) : (
                    <ShoppingSourceView items={items} mode={view} />
                  )}
                </div>
              </>
            )}
          </>
        )}
      </QueryState>

      {diffOpen && diff.data && (
        <ShoppingListDiffDialog
          diff={diff.data}
          isSubmitting={refreshList.isPending}
          isStale={diffStale}
          isRefreshingPreview={diff.isFetching}
          onClose={() => {
            setDiffOpen(false)
            setDiffStale(false)
          }}
          onConfirm={confirmRefresh}
        />
      )}

      {toast && <Toast tone={toast.tone} message={toast.message} />}
    </section>
  )
}

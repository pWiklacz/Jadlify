import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { Button } from '../ui/Button'
import { Card } from '../ui/Card'
import { PageHeader } from '../ui/PageHeader'
import { QueryState } from '../ui/QueryState'
import { Toast } from '../ui/Toast'
import { formatCount } from '../ui/formatters'
import { DeleteProductDialog } from './DeleteProductDialog'
import { ProductCard } from './ProductCard'
import { ProductFilters } from './ProductFilters'
import { ProductFormModal } from './ProductFormModal'
import {
  PRODUCT_CATEGORIES,
  PRODUCT_SORTS,
  type Product,
  type ProductCatalogSort,
} from './types'
import { useProductCatalog, type CatalogCategoryFilter } from './useProductCatalog'

type ModalState = { mode: 'create' } | { mode: 'edit'; product: Product } | null

const SORT_VALUES = new Set<string>(PRODUCT_SORTS.map((s) => s.value))
const CATEGORY_VALUES = new Set<string>(['all', 'None', ...PRODUCT_CATEGORIES.map((c) => c.value)])

function normalizeSort(value: string | null): ProductCatalogSort {
  return value && SORT_VALUES.has(value) ? (value as ProductCatalogSort) : 'NameAsc'
}

function normalizeCategory(value: string | null): CatalogCategoryFilter {
  return value && CATEGORY_VALUES.has(value) ? value : 'all'
}

/**
 * Product catalog index (S-02). Search / category / sort live in the URL so the
 * list is shareable and survives a round-trip to a product's detail page. The
 * search box is debounced into the URL; category and sort apply immediately.
 * Hosts the add/edit form and the delete confirmation.
 */
export function ProductsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()

  const appliedSearch = searchParams.get('search') ?? ''
  const category = normalizeCategory(searchParams.get('category'))
  const sort = normalizeSort(searchParams.get('sort'))

  const [searchInput, setSearchInput] = useState(appliedSearch)
  const [modal, setModal] = useState<ModalState>(null)
  const [deleteTarget, setDeleteTarget] = useState<Product | null>(null)
  const [toast, setToast] = useState<string | null>(null)

  const catalog = useProductCatalog({ search: appliedSearch, category, sort })
  const products = catalog.data?.items ?? []
  const total = catalog.data?.total ?? 0
  const filtersActive = appliedSearch.trim() !== '' || category !== 'all'

  // Debounce the search box into the URL (the query's source of truth) so the
  // catalog does not refetch on every keystroke. `replace` keeps history clean.
  useEffect(() => {
    const handle = setTimeout(() => {
      setSearchParams(
        (prev) => {
          const next = new URLSearchParams(prev)
          const trimmed = searchInput.trim()
          if (trimmed) {
            next.set('search', trimmed)
          } else {
            next.delete('search')
          }
          return next
        },
        { replace: true },
      )
    }, 300)
    return () => clearTimeout(handle)
  }, [searchInput, setSearchParams])

  // Auto-dismiss the success toast.
  useEffect(() => {
    if (!toast) {
      return
    }
    const handle = setTimeout(() => setToast(null), 3200)
    return () => clearTimeout(handle)
  }, [toast])

  function patchParam(key: string, value: string | null) {
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev)
      if (value) {
        next.set(key, value)
      } else {
        next.delete(key)
      }
      return next
    })
  }

  function clearSearch() {
    setSearchInput('')
    patchParam('search', null)
  }

  function clearAll() {
    setSearchInput('')
    setSearchParams({}, { replace: true })
  }

  // Barcode "already in catalog" → edit it if it's on the current page, otherwise
  // jump to its detail route (it may be filtered out of the visible page).
  function editExisting(productId: string) {
    const existing = products.find((product) => product.id === productId)
    if (existing) {
      setModal({ mode: 'edit', product: existing })
    } else {
      setModal(null)
      navigate(`/products/${productId}`)
    }
  }

  const isEmptyCatalog =
    !catalog.isPending && !catalog.isError && total === 0 && !filtersActive
  const showNoResults =
    !catalog.isPending && !catalog.isError && total === 0 && filtersActive

  return (
    <section>
      <PageHeader
        title="Produkty"
        subtitle="Twój prywatny katalog z wartościami odżywczymi na 100 g — z nich budujesz przepisy, a kategorie porządkują listę zakupów."
        actions={
          !isEmptyCatalog ? (
            <Button onClick={() => setModal({ mode: 'create' })}>Dodaj produkt</Button>
          ) : undefined
        }
      />

      <QueryState
        isPending={catalog.isPending}
        isError={catalog.isError}
        onRetry={() => void catalog.refetch()}
        loadingLabel="Wczytujemy Twoje produkty…"
        errorTitle="Nie udało się pobrać produktów"
        errorDescription="Wystąpił problem z połączeniem. Twój katalog jest bezpieczny — spróbuj ponownie."
      >
        {isEmptyCatalog ? (
          <Card className="mx-auto mt-2 flex max-w-xl flex-col items-center gap-3 text-center">
            <h2 className="font-serif text-2xl font-normal text-espresso">
              Twój katalog jest jeszcze pusty
            </h2>
            <p className="max-w-md text-sm leading-relaxed text-mocha">
              Produkty to podstawa Jadlify — z ich wartości na 100 g budujesz przepisy i liczysz makro.
              Dodaj pierwszy produkt: wpisz dane ręcznie albo wklej kod kreskowy z opakowania, a
              spróbujemy pobrać dane za Ciebie.
            </p>
            <Button onClick={() => setModal({ mode: 'create' })}>Dodaj pierwszy produkt</Button>
            <p className="text-[12.5px] text-mocha">
              Kod kreskowy jest opcjonalny — zawsze możesz uzupełnić wszystko ręcznie.
            </p>
          </Card>
        ) : (
          <>
            <ProductFilters
              search={searchInput}
              onSearchChange={setSearchInput}
              onClearSearch={clearSearch}
              category={category}
              onCategoryChange={(value) => patchParam('category', value === 'all' ? null : value)}
              sort={sort}
              onSortChange={(value) => patchParam('sort', value === 'NameAsc' ? null : value)}
              countLabel={formatCount(total, 'produkt', 'produkty', 'produktów')}
              appliedSearch={appliedSearch}
              onClearAll={clearAll}
            />

            {showNoResults ? (
              <Card
                tone="panel"
                className="mx-auto mt-2 flex max-w-lg flex-col items-center gap-3 border-dashed text-center"
              >
                <h2 className="text-base font-bold text-espresso">Brak pasujących produktów</h2>
                <p className="max-w-sm text-[13.5px] leading-relaxed text-mocha">
                  Sprawdź pisownię lub kod, zmień kategorię albo wyczyść filtry, aby zobaczyć wszystkie
                  produkty.
                </p>
                <div className="flex flex-wrap justify-center gap-2">
                  <Button variant="ghost" size="sm" onClick={clearAll}>
                    Wyczyść wyszukiwanie i filtry
                  </Button>
                  <Button variant="secondary" size="sm" onClick={() => setModal({ mode: 'create' })}>
                    Dodaj nowy produkt
                  </Button>
                </div>
              </Card>
            ) : (
              <ul
                className="grid grid-cols-1 gap-3.5 sm:grid-cols-2 design:grid-cols-3"
                aria-busy={catalog.isFetching}
              >
                {products.map((product) => (
                  <li key={product.id}>
                    <ProductCard
                      product={product}
                      to={`/products/${product.id}`}
                      fromSearch={searchParams.toString()}
                      onEdit={() => setModal({ mode: 'edit', product })}
                      onDelete={() => setDeleteTarget(product)}
                    />
                  </li>
                ))}
              </ul>
            )}
          </>
        )}
      </QueryState>

      {modal && (
        <ProductFormModal
          key={modal.mode === 'edit' ? `edit-${modal.product.id}` : 'create'}
          mode={modal.mode}
          product={modal.mode === 'edit' ? modal.product : undefined}
          onClose={() => setModal(null)}
          onEditExisting={editExisting}
          onSaved={(savedMode) =>
            setToast(savedMode === 'create' ? 'Dodano produkt.' : 'Zapisano zmiany.')
          }
        />
      )}

      {deleteTarget && (
        <DeleteProductDialog
          product={deleteTarget}
          onClose={() => setDeleteTarget(null)}
          onDeleted={() => setToast('Usunięto produkt.')}
        />
      )}

      {toast && <Toast tone="success" message={toast} />}
    </section>
  )
}

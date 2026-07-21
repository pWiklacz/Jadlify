import { useEffect, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { Button } from '../ui/Button'
import { Card } from '../ui/Card'
import { PageHeader } from '../ui/PageHeader'
import { QueryState } from '../ui/QueryState'
import { Toast } from '../ui/Toast'
import { formatCount } from '../ui/formatters'
import { DeleteRecipeDialog } from './DeleteRecipeDialog'
import { RecipeCard } from './RecipeCard'
import { RecipeFilters } from './RecipeFilters'
import { RecipeFormModal } from './RecipeFormModal'
import { useProductSearch } from './useProductSearch'
import { useRecipeCatalog } from './useRecipeCatalog'
import { RECIPE_SORTS, type RecipeCatalogSort, type RecipeSummary } from './types'

type ModalState = { mode: 'create' } | { mode: 'edit'; recipeId: string } | null

const SORT_VALUES = new Set<string>(RECIPE_SORTS.map((option) => option.value))

function normalizeSort(value: string | null): RecipeCatalogSort {
  return value && SORT_VALUES.has(value) ? (value as RecipeCatalogSort) : 'NameAsc'
}

/**
 * Recipe catalog index (S-03). Search and sort live in the URL so the list is
 * shareable and survives a round-trip to a recipe's detail page. The search box
 * is debounced into the URL; sort applies immediately. Hosts the builder and the
 * delete confirmation.
 */
export function RecipesPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()

  const appliedSearch = searchParams.get('search') ?? ''
  const sort = normalizeSort(searchParams.get('sort'))

  const [searchInput, setSearchInput] = useState(appliedSearch)
  const [modal, setModal] = useState<ModalState>(null)
  const [deleteTarget, setDeleteTarget] = useState<RecipeSummary | null>(null)
  const [toast, setToast] = useState<string | null>(null)

  const catalog = useRecipeCatalog({ search: appliedSearch, sort })
  // A one-row probe: recipes are built from products, so an empty product catalog
  // needs its own empty state that routes the user to /products first.
  const productProbe = useProductSearch('', 1)

  const recipes = catalog.data?.items ?? []
  const total = catalog.data?.total ?? 0
  const searchActive = appliedSearch.trim() !== ''
  const hasProducts = (productProbe.data?.length ?? 0) > 0

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

  useEffect(() => {
    if (!toast) {
      return
    }
    const handle = setTimeout(() => setToast(null), 3200)
    return () => clearTimeout(handle)
  }, [toast])

  function clearSearch() {
    setSearchInput('')
    setSearchParams(
      (prev) => {
        const next = new URLSearchParams(prev)
        next.delete('search')
        return next
      },
      { replace: true },
    )
  }

  function changeSort(value: RecipeCatalogSort) {
    setSearchParams((prev) => {
      const next = new URLSearchParams(prev)
      if (value === 'NameAsc') {
        next.delete('sort')
      } else {
        next.set('sort', value)
      }
      return next
    })
  }

  const settled = !catalog.isPending && !catalog.isError
  const isEmptyCatalog = settled && total === 0 && !searchActive
  const showNoResults = settled && total === 0 && searchActive
  // The probe decides which empty state to show, so wait for it before choosing.
  const showEmptyBranch = isEmptyCatalog && !productProbe.isPending

  return (
    <section>
      <PageHeader
        title="Przepisy"
        subtitle="Przepisy budujesz z własnych produktów — kalorie i makro liczymy automatycznie z gramatur i liczby porcji."
        actions={
          !isEmptyCatalog ? (
            <Button onClick={() => setModal({ mode: 'create' })}>Utwórz przepis</Button>
          ) : undefined
        }
      />

      <QueryState
        isPending={catalog.isPending}
        isError={catalog.isError}
        onRetry={() => void catalog.refetch()}
        loadingLabel="Wczytujemy Twoje przepisy…"
        errorTitle="Nie udało się pobrać przepisów"
        errorDescription="Wystąpił problem z połączeniem. Twoje przepisy i produkty są bezpieczne — spróbuj ponownie."
      >
        {isEmptyCatalog ? (
          showEmptyBranch && (
            <Card className="mx-auto mt-2 flex max-w-xl flex-col items-center gap-3 text-center">
              {hasProducts ? (
                <>
                  <h2 className="font-serif text-2xl font-normal text-espresso">
                    Nie masz jeszcze przepisów
                  </h2>
                  <p className="max-w-md text-sm leading-relaxed text-mocha">
                    Przepis to zestaw Twoich produktów z gramaturami i liczbą porcji. Kalorie, białko,
                    tłuszcz i węglowodany policzymy automatycznie — nic nie wpisujesz ręcznie.
                  </p>
                  <Button onClick={() => setModal({ mode: 'create' })}>Utwórz pierwszy przepis</Button>
                </>
              ) : (
                <>
                  <h2 className="font-serif text-2xl font-normal text-espresso">
                    Zacznij od dodania produktów
                  </h2>
                  <p className="max-w-md text-sm leading-relaxed text-mocha">
                    Przepisy powstają z Twoich produktów i ich wartości na 100 g. Nie masz jeszcze
                    żadnych produktów — dodaj pierwszy, a potem złożysz z nich przepis.
                  </p>
                  <div className="flex flex-wrap justify-center gap-2">
                    <Button onClick={() => navigate('/products')}>Dodaj pierwszy produkt</Button>
                    <Button variant="ghost" disabled title="Najpierw dodaj produkt">
                      Utwórz pierwszy przepis
                    </Button>
                  </div>
                  <p className="text-[12.5px] text-mocha">
                    Tworzenie przepisu będzie dostępne, gdy dodasz przynajmniej jeden produkt.
                  </p>
                </>
              )}
            </Card>
          )
        ) : (
          <>
            <RecipeFilters
              search={searchInput}
              onSearchChange={setSearchInput}
              onClearSearch={clearSearch}
              sort={sort}
              onSortChange={changeSort}
              countLabel={formatCount(total, 'przepis', 'przepisy', 'przepisów')}
            />

            {showNoResults ? (
              <Card
                tone="panel"
                className="mx-auto mt-2 flex max-w-lg flex-col items-center gap-3 border-dashed text-center"
              >
                <h2 className="text-base font-bold text-espresso">
                  Brak przepisów dla „{appliedSearch.trim()}”
                </h2>
                <p className="max-w-sm text-[13.5px] leading-relaxed text-mocha">
                  Sprawdź pisownię albo wyczyść wyszukiwanie, aby zobaczyć wszystkie przepisy.
                </p>
                <div className="flex flex-wrap justify-center gap-2">
                  <Button variant="ghost" size="sm" onClick={clearSearch}>
                    Wyczyść wyszukiwanie
                  </Button>
                  <Button variant="secondary" size="sm" onClick={() => setModal({ mode: 'create' })}>
                    Utwórz nowy przepis
                  </Button>
                </div>
              </Card>
            ) : (
              <ul
                className="grid grid-cols-1 gap-3.5 sm:grid-cols-2 design:grid-cols-3"
                aria-busy={catalog.isFetching}
              >
                {recipes.map((recipe) => (
                  <li key={recipe.id}>
                    <RecipeCard
                      recipe={recipe}
                      to={`/recipes/${recipe.id}`}
                      fromSearch={searchParams.toString()}
                      onEdit={() => setModal({ mode: 'edit', recipeId: recipe.id })}
                      onDelete={() => setDeleteTarget(recipe)}
                    />
                  </li>
                ))}
              </ul>
            )}
          </>
        )}
      </QueryState>

      {modal && (
        <RecipeFormModal
          key={modal.mode === 'edit' ? `edit-${modal.recipeId}` : 'create'}
          mode={modal.mode}
          recipeId={modal.mode === 'edit' ? modal.recipeId : undefined}
          onClose={() => setModal(null)}
          onSaved={(savedMode) =>
            setToast(savedMode === 'create' ? 'Dodano przepis.' : 'Zapisano zmiany.')
          }
        />
      )}

      {deleteTarget && (
        <DeleteRecipeDialog
          recipeId={deleteTarget.id}
          recipeName={deleteTarget.name}
          onClose={() => setDeleteTarget(null)}
          onDeleted={() => setToast('Usunięto przepis.')}
          planLink={
            <Link
              to="/meal-plan"
              className="font-semibold text-terracotta underline underline-offset-[3px]"
            >
              Przejdź do planu posiłków
            </Link>
          }
        />
      )}

      {toast && <Toast tone="success" message={toast} />}
    </section>
  )
}

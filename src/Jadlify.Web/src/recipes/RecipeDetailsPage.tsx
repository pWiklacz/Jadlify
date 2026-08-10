import { useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { ApiError } from '../api/client'
import { Button } from '../ui/Button'
import { Card } from '../ui/Card'
import { QueryState } from '../ui/QueryState'
import { Toast } from '../ui/Toast'
import { formatCount, formatGrams, formatKcal, formatMacro } from '../ui/formatters'
import { DeleteRecipeDialog } from './DeleteRecipeDialog'
import { RecipeFormModal } from './RecipeFormModal'
import { useRecipe } from './useRecipes'
import { buildAddToPlanUrl } from './addToPlan'
import type { Recipe } from './types'

/**
 * Recipe detail route (`/recipes/:id`). Reads the recipe via the existing
 * `GET /api/recipes/{id}` (the only place the full ingredient composition is
 * shipped), and hosts the builder, the delete confirmation and the
 * "Dodaj do planu" handoff. The back link restores the index filters carried in
 * navigation state.
 */
export function RecipeDetailsPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const location = useLocation()
  const query = useRecipe(id ?? null)

  const [editing, setEditing] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [toast, setToast] = useState<string | null>(null)

  const fromSearch = (location.state as { fromSearch?: string } | null)?.fromSearch ?? ''
  const backTo = fromSearch ? `/recipes?${fromSearch}` : '/recipes'
  const isNotFound = query.error instanceof ApiError && query.error.status === 404

  function addToPlan(recipe: Recipe) {
    navigate(
      buildAddToPlanUrl({
        recipeId: recipe.id,
        returnTo: `${location.pathname}${location.search}`,
      }),
    )
  }

  return (
    <section>
      <button
        type="button"
        onClick={() => navigate(backTo)}
        className="mb-2 inline-flex items-center gap-1.5 rounded-field px-1 py-2 text-[13px] font-semibold text-parchment/60 transition-colors hover:text-parchment"
      >
        <svg width="14" height="14" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.9" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
          <path d="M12.5 4 6.5 10l6 6" />
        </svg>
        Wszystkie przepisy
      </button>

      {isNotFound ? (
        <Card className="flex flex-col items-start gap-3">
          <h1 className="font-serif text-2xl font-normal text-espresso">Nie znaleziono przepisu</h1>
          <p className="text-sm text-mocha">
            Ten przepis nie istnieje albo został usunięty. Wróć do katalogu, aby zobaczyć swoje
            przepisy.
          </p>
          <Button variant="ghost" size="sm" onClick={() => navigate('/recipes')}>
            Wróć do przepisów
          </Button>
        </Card>
      ) : (
        <QueryState
          isPending={query.isPending}
          isError={query.isError}
          onRetry={() => void query.refetch()}
          loadingLabel="Wczytujemy przepis…"
          errorTitle="Nie udało się pobrać przepisu"
        >
          {query.data && (
            <RecipeDetail
              recipe={query.data}
              onAddToPlan={() => addToPlan(query.data!)}
              onEdit={() => setEditing(true)}
              onDelete={() => setDeleting(true)}
            />
          )}
        </QueryState>
      )}

      {editing && query.data && (
        <RecipeFormModal
          mode="edit"
          recipeId={query.data.id}
          onClose={() => setEditing(false)}
          onSaved={() => setToast('Zapisano zmiany.')}
        />
      )}

      {deleting && query.data && (
        <DeleteRecipeDialog
          recipeId={query.data.id}
          recipeName={query.data.name}
          onClose={() => setDeleting(false)}
          onDeleted={() => navigate('/recipes')}
        />
      )}

      {toast && <Toast tone="success" message={toast} />}
    </section>
  )
}

function RecipeDetail({
  recipe,
  onAddToPlan,
  onEdit,
  onDelete,
}: {
  recipe: Recipe
  onAddToPlan: () => void
  onEdit: () => void
  onDelete: () => void
}) {
  const portionsLabel = formatCount(recipe.portions, 'porcja', 'porcje', 'porcji')
  const metaLine = `${portionsLabel} · ${formatCount(recipe.ingredients.length, 'składnik', 'składniki', 'składników')}`
  const perServing = recipe.perServingMacros
  const total = recipe.totalMacros

  return (
    <div className="flex flex-col gap-4">
      <Card className="flex flex-col gap-3">
        <span className="w-fit rounded-pill border border-terracotta/35 bg-terracotta/15 px-3 py-1 text-[10.5px] font-bold uppercase tracking-eyebrow text-terracotta-strong">
          Przepis
        </span>
        <div>
          <h1 className="break-words font-serif text-3xl font-normal leading-tight text-espresso design:text-4xl">
            {recipe.name}
          </h1>
          <p className="mt-1 text-sm text-mocha">{metaLine}</p>
        </div>

        {recipe.isInPlan && (
          <p className="rounded-panel border border-success/35 bg-success/10 px-3.5 py-2.5 text-[13px] leading-relaxed text-success-ink">
            <b>Ten przepis jest używany w zaplanowanych posiłkach.</b> Zmiany w przepisie wpłyną na
            przyszłe wyliczenia planu.
          </p>
        )}

        <div className="mt-1 flex flex-wrap items-center gap-2">
          <Button onClick={onAddToPlan}>Dodaj do planu</Button>
          <Button variant="ghost" onClick={onEdit}>
            Edytuj
          </Button>
          <div className="flex-1" />
          <Button variant="ghost" onClick={onDelete}>
            Usuń przepis
          </Button>
        </div>
      </Card>

      <div className="flex flex-col gap-4 design:flex-row design:items-start">
        <Card className="flex flex-col gap-3 design:flex-1">
          <div className="text-[11px] font-bold uppercase tracking-eyebrow text-label">
            Wartości odżywcze
          </div>
          <div className="rounded-panel border-[1.5px] border-terracotta/45 bg-terracotta/[0.06] p-4">
            <div className="text-[11px] font-bold uppercase tracking-eyebrow text-terracotta">
              Na 1 porcję
            </div>
            <div className="mt-2 flex items-baseline gap-2">
              <span className="font-serif text-[40px] leading-none tabular-nums text-espresso">
                {formatKcal(perServing.calories)}
              </span>
              <span className="text-[13px] font-bold text-mocha">kcal</span>
            </div>
            <dl className="mt-3 grid grid-cols-3 gap-2">
              <MacroCell label="Białko" value={perServing.protein} />
              <MacroCell label="Tłuszcz" value={perServing.fat} />
              <MacroCell label="Węglow." value={perServing.carbohydrates} />
            </dl>
          </div>

          <div className="rounded-panel border border-cream-border bg-cream-panel p-4">
            <div className="text-[11px] font-bold uppercase tracking-eyebrow text-label">
              Cały przepis · {portionsLabel}
            </div>
            <p className="mt-1.5 text-sm tabular-nums text-espresso">
              <b>{formatKcal(total.calories)} kcal</b>{' '}
              <span className="text-label">
                · B {formatMacro(total.protein)} · T {formatMacro(total.fat)} · W{' '}
                {formatMacro(total.carbohydrates)}
              </span>
            </p>
          </div>

          <p className="text-[12.5px] leading-relaxed text-mocha">
            Wartości liczymy automatycznie z gramatur składników i danych produktów (na 100 g).
          </p>
        </Card>

        <Card className="flex flex-col gap-2 design:flex-[1.4]">
          <div className="flex flex-wrap items-baseline gap-2">
            <span className="text-[11px] font-bold uppercase tracking-eyebrow text-label">
              Składniki
            </span>
            <span className="rounded-pill border border-terracotta/30 bg-terracotta/10 px-2 py-0.5 text-[10px] font-bold uppercase tracking-eyebrow text-terracotta">
              Cały przepis
            </span>
          </div>
          <p className="text-[13px] leading-relaxed text-mocha">
            Podane ilości dotyczą całego przygotowywanego przepisu ({portionsLabel}) — nie jednej
            porcji.
          </p>

          <ul>
            {recipe.ingredients.map((ingredient) => (
              <li
                key={ingredient.productId}
                className="flex items-baseline gap-2 border-b border-dotted border-cream-line py-2 last:border-b-0"
              >
                <span className="min-w-0 flex-1 break-words text-[13.5px] text-espresso">
                  {ingredient.productName}
                </span>
                <span className="whitespace-nowrap text-[13.5px] font-bold tabular-nums text-espresso">
                  {formatGrams(ingredient.wholeRecipeGrams)}
                </span>
              </li>
            ))}
          </ul>
        </Card>
      </div>
    </div>
  )
}

function MacroCell({ label, value }: { label: string; value: number }) {
  return (
    <div>
      <dt className="text-[10px] font-bold uppercase tracking-eyebrow text-mocha">{label}</dt>
      <dd className="mt-0.5 text-[15px] font-bold tabular-nums text-espresso">
        {formatMacro(value)}
      </dd>
    </div>
  )
}

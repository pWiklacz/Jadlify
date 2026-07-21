import { useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { ApiError } from '../api/client'
import { Button } from '../ui/Button'
import { Card } from '../ui/Card'
import { QueryState } from '../ui/QueryState'
import { Toast } from '../ui/Toast'
import { formatKcal, formatMacro } from '../ui/formatters'
import { DeleteProductDialog } from './DeleteProductDialog'
import { ProductFormModal } from './ProductFormModal'
import { EXTENDED_FIELD_GROUPS, formatNutrient } from './nutrients'
import { useProduct } from './useProduct'
import { categoryLabel, type Product } from './types'

/** Scales a per-100g value to the whole package. */
function perPackage(per100g: number, packageSizeGrams: number): number {
  return (per100g * packageSizeGrams) / 100
}

/**
 * Product detail route (`/products/:id`). Reads the product via the existing
 * `GET /api/products/{id}`, renders the per-100g and per-package nutrition plus
 * the extended profile, and hosts the edit form and delete confirmation. The
 * back link restores the index filters carried in navigation state.
 */
export function ProductDetailsPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const location = useLocation()
  const query = useProduct(id)

  const [editing, setEditing] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [toast, setToast] = useState<string | null>(null)

  const fromSearch = (location.state as { fromSearch?: string } | null)?.fromSearch ?? ''
  const backTo = fromSearch ? `/products?${fromSearch}` : '/products'
  const isNotFound = query.error instanceof ApiError && query.error.status === 404

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
        Wszystkie produkty
      </button>

      {isNotFound ? (
        <Card className="flex flex-col items-start gap-3">
          <h1 className="font-serif text-2xl font-normal text-espresso">Nie znaleziono produktu</h1>
          <p className="text-sm text-mocha">
            Ten produkt nie istnieje albo został usunięty. Wróć do katalogu, aby zobaczyć swoje
            produkty.
          </p>
          <Button variant="ghost" size="sm" onClick={() => navigate('/products')}>
            Wróć do produktów
          </Button>
        </Card>
      ) : (
        <QueryState
          isPending={query.isPending}
          isError={query.isError}
          onRetry={() => void query.refetch()}
          loadingLabel="Wczytujemy produkt…"
          errorTitle="Nie udało się pobrać produktu"
        >
          {query.data && (
            <ProductDetail
              product={query.data}
              onEdit={() => setEditing(true)}
              onDelete={() => setDeleting(true)}
            />
          )}
        </QueryState>
      )}

      {editing && query.data && (
        <ProductFormModal
          mode="edit"
          product={query.data}
          onClose={() => setEditing(false)}
          onEditExisting={() => setEditing(false)}
          onSaved={() => setToast('Zapisano zmiany.')}
        />
      )}

      {deleting && query.data && (
        <DeleteProductDialog
          product={query.data}
          onClose={() => setDeleting(false)}
          onDeleted={() => navigate('/products')}
        />
      )}

      {toast && <Toast tone="success" message={toast} />}
    </section>
  )
}

function ProductDetail({
  product,
  onEdit,
  onDelete,
}: {
  product: Product
  onEdit: () => void
  onDelete: () => void
}) {
  const pkg = product.packageSizeGrams

  return (
    <div className="flex flex-col gap-4">
      <Card className="flex flex-col gap-3">
        <span className="w-fit rounded-pill border border-terracotta/35 bg-terracotta/15 px-3 py-1 text-[10.5px] font-bold uppercase tracking-eyebrow text-terracotta-strong">
          Produkt
        </span>
        <div>
          <h1 className="break-words font-serif text-3xl font-normal leading-tight text-espresso design:text-4xl">
            {product.name}
          </h1>
          {product.brand && <p className="mt-1 text-sm text-mocha">{product.brand}</p>}
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {product.category ? (
            <span className="rounded-pill border border-success/30 bg-success/10 px-3 py-1 text-[10.5px] font-bold uppercase tracking-eyebrow text-success-ink">
              {categoryLabel(product.category)}
            </span>
          ) : (
            <span className="rounded-pill border border-cream-line bg-cream-hover px-3 py-1 text-[10.5px] font-bold uppercase tracking-eyebrow text-mocha">
              Bez kategorii
            </span>
          )}
          {product.barcode && (
            <span className="rounded-pill border border-cream-border bg-cream-panel px-3 py-1 text-xs font-semibold tabular-nums text-label">
              Kod: {product.barcode}
            </span>
          )}
          {pkg != null && (
            <span className="rounded-pill border border-cream-border bg-cream-panel px-3 py-1 text-xs font-semibold tabular-nums text-label">
              Opakowanie: {formatMacro(pkg)} g
            </span>
          )}
        </div>

        <div className="mt-1 flex flex-wrap items-center gap-2">
          <Button onClick={onEdit}>Edytuj produkt</Button>
          <div className="flex-1" />
          <Button variant="ghost" onClick={onDelete}>
            Usuń produkt
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
              Na 100 g
            </div>
            <div className="mt-2 flex items-baseline gap-2">
              <span className="font-serif text-[40px] leading-none tabular-nums text-espresso">
                {formatKcal(product.calories)}
              </span>
              <span className="text-[13px] font-bold text-mocha">kcal</span>
            </div>
            <dl className="mt-3 grid grid-cols-3 gap-2">
              <MacroCell label="Białko" value={product.protein} />
              <MacroCell label="Tłuszcz" value={product.fat} />
              <MacroCell label="Węglow." value={product.carbohydrates} />
            </dl>
          </div>

          {pkg != null && (
            <div className="rounded-panel border border-cream-border bg-cream-panel p-4">
              <div className="flex flex-wrap items-center gap-2">
                <span className="text-[11px] font-bold uppercase tracking-eyebrow text-label">
                  Całe opakowanie · {formatMacro(pkg)} g
                </span>
                <span className="rounded-pill bg-cream-hover px-2 py-0.5 text-[9.5px] font-bold uppercase tracking-eyebrow text-mocha">
                  Wartość wyliczona
                </span>
              </div>
              <p className="mt-1.5 text-sm tabular-nums text-espresso">
                <b>{formatKcal(perPackage(product.calories, pkg))} kcal</b>{' '}
                <span className="text-label">
                  · B {formatMacro(perPackage(product.protein, pkg))} · T{' '}
                  {formatMacro(perPackage(product.fat, pkg))} · W{' '}
                  {formatMacro(perPackage(product.carbohydrates, pkg))}
                </span>
              </p>
              <p className="mt-1.5 text-xs leading-relaxed text-mocha">
                Przeliczenie z danych na 100 g — wartości bazowe produktu pozostają zawsze na 100 g.
              </p>
            </div>
          )}
        </Card>

        <ExtendedNutritionCard product={product} />
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

function ExtendedNutritionCard({ product }: { product: Product }) {
  const groups = EXTENDED_FIELD_GROUPS.map((group) => ({
    legend: group.legend,
    rows: group.fields
      .filter((meta) => product[meta.key] != null)
      .map((meta) => ({
        key: meta.key,
        label: meta.label,
        value: formatNutrient(product[meta.key] as number, meta.unit),
      })),
  })).filter((group) => group.rows.length > 0)

  return (
    <Card className="flex flex-col gap-2 design:flex-[1.4]">
      <div className="flex flex-wrap items-baseline gap-2">
        <span className="text-[11px] font-bold uppercase tracking-eyebrow text-label">
          Dodatkowe wartości odżywcze
        </span>
        <span className="rounded-pill border border-terracotta/30 bg-terracotta/10 px-2 py-0.5 text-[10px] font-bold uppercase tracking-eyebrow text-terracotta">
          Na 100 g
        </span>
      </div>

      {groups.length === 0 ? (
        <div className="mt-1 rounded-panel border border-dashed border-cream-line bg-cream-panel px-4 py-3 text-[13.5px] leading-relaxed text-mocha">
          Brak dodatkowych danych odżywczych. Możesz je uzupełnić w edycji produktu — nie są wymagane.
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-x-7 sm:grid-cols-2">
          {groups.map((group) => (
            <div key={group.legend}>
              <div className="mb-1 mt-3 text-[10.5px] font-bold uppercase tracking-eyebrow text-mocha">
                {group.legend}
              </div>
              {group.rows.map((row) => (
                <div
                  key={row.key}
                  className="flex items-baseline gap-2 border-b border-dotted border-cream-line py-1.5"
                >
                  <span className="text-[13.5px] text-label">{row.label}</span>
                  <span className="flex-1" />
                  <span className="whitespace-nowrap text-[13.5px] font-bold tabular-nums text-espresso">
                    {row.value}
                  </span>
                </div>
              ))}
            </div>
          ))}
        </div>
      )}
    </Card>
  )
}

import { calculateRecipePreview, formatMacro, type MacroPreviewIngredient } from './macroMath'
import type { RecipeMacroSummary } from './types'

interface RecipeMacroPreviewProps {
  ingredients: MacroPreviewIngredient[]
  portions: number
}

const MACRO_LABELS = [
  { key: 'calories', label: 'kcal' },
  { key: 'protein', label: 'Protein' },
  { key: 'fat', label: 'Fat' },
  { key: 'carbohydrates', label: 'Carbs' },
] as const

/** Live preview only; the API remains authoritative after save. */
export function RecipeMacroPreview({ ingredients, portions }: RecipeMacroPreviewProps) {
  const preview = calculateRecipePreview(ingredients, portions)

  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
      <MacroPanel title="Whole recipe" macros={preview.total} />
      <MacroPanel title="Per serving" macros={preview.perServing} />
    </div>
  )
}

export function MacroPanel({ title, macros }: { title: string; macros: RecipeMacroSummary }) {
  return (
    <section className="rounded-lg border border-slate-200 bg-slate-50 p-3">
      <h3 className="text-sm font-semibold text-slate-700">{title}</h3>
      <dl className="mt-2 grid grid-cols-2 gap-2 text-sm">
        {MACRO_LABELS.map((item) => (
          <div key={item.key} className="flex justify-between gap-2">
            <dt className="text-slate-500">{item.label}</dt>
            <dd className="font-medium text-slate-900">
              {formatMacro(macros[item.key])}
              {item.key === 'calories' ? '' : ' g'}
            </dd>
          </div>
        ))}
      </dl>
    </section>
  )
}

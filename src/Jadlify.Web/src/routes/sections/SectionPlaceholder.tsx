interface SectionPlaceholderProps {
  /** Section name, rendered as the page heading. */
  title: string
}

/**
 * Minimal placeholder for an MVP section route. Each section (products,
 * recipes, meal plan, daily goals, shopping list) gets a real UI in a later
 * roadmap slice (S-02…S-06); this gives the navigation a reachable destination
 * in the meantime without locking in any feature layout.
 */
export function SectionPlaceholder({ title }: SectionPlaceholderProps) {
  return (
    <section className="flex flex-col gap-2">
      <h1 className="text-2xl font-bold">{title}</h1>
      <p className="max-w-prose text-slate-600">
        This section is a placeholder. Its features are built in a later roadmap
        slice.
      </p>
    </section>
  )
}

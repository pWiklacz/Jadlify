/**
 * The dashboard's single contextual "Następny krok".
 *
 * Deliberately one step, not a five-point onboarding checklist: the app has a
 * strict data dependency chain (products → recipes → goal → plan → list), so at
 * any moment exactly one thing unblocks the next. Resolving it as a pure function
 * of the counts keeps the rule testable and stops the copy drifting between the
 * dashboard and any other surface that wants to nudge the user.
 */

export type NextStepId = 'products' | 'recipes' | 'goal' | 'plan' | 'list' | 'shop'

export interface NextStep {
  id: NextStepId
  title: string
  description: string
  actionLabel: string
  /** Where the action goes, or `null` when it is handled in place (opening the add-meal dialog). */
  to: string | null
}

export interface NextStepInput {
  productCount: number
  recipeCount: number
  hasGoal: boolean
  /** Meals planned on the day currently in view. */
  plannedMealCount: number
  /** The active list's id, or null when no list is in progress. */
  activeListId: string | null
}

/**
 * Picks the one step that unblocks the most, in dependency order. Each earlier
 * gap makes the later ones unreachable, so the first unmet condition wins.
 */
export function resolveNextStep({
  productCount,
  recipeCount,
  hasGoal,
  plannedMealCount,
  activeListId,
}: NextStepInput): NextStep {
  if (productCount === 0) {
    return {
      id: 'products',
      title: 'Dodaj pierwszy produkt',
      description:
        'Produkty z wartościami na 100 g są podstawą przepisów, makro i listy zakupów.',
      actionLabel: 'Przejdź do produktów',
      to: '/products',
    }
  }

  if (recipeCount === 0) {
    return {
      id: 'recipes',
      title: 'Utwórz pierwszy przepis',
      description: 'Z przepisu zaplanujesz posiłek w porcjach, a my policzymy makro za Ciebie.',
      actionLabel: 'Przejdź do przepisów',
      to: '/recipes',
    }
  }

  if (!hasGoal) {
    return {
      id: 'goal',
      title: 'Ustaw dzienne cele',
      description:
        'Bez celu planowanie działa, ale nie zobaczysz, ile kalorii i makro zostało Ci na dziś.',
      actionLabel: 'Ustaw dzienne cele',
      to: '/goals',
    }
  }

  if (plannedMealCount === 0) {
    return {
      id: 'plan',
      title: 'Zaplanuj swój pierwszy posiłek',
      description: 'Dodaj przepis albo pojedynczy produkt do tego dnia, aby zobaczyć bilans.',
      actionLabel: 'Dodaj posiłek',
      to: null,
    }
  }

  if (activeListId === null) {
    return {
      id: 'list',
      title: 'Utwórz listę zakupów',
      description: 'Wybierz dni z planu, a złożymy z nich listę produktów do kupienia.',
      actionLabel: 'Utwórz listę zakupów',
      to: '/shopping-list',
    }
  }

  return {
    id: 'shop',
    title: 'Dokończ zakupy',
    description: 'Masz aktywną listę zakupów — odhacz kupione produkty i zamknij ją.',
    actionLabel: 'Przejdź do listy',
    to: `/shopping-list/${activeListId}`,
  }
}

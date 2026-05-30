/** A primary navigation destination shown in the app bar / mobile drawer. */
export interface NavItem {
  /** Router path (absolute). */
  to: string
  /** Human-readable label. */
  label: string
}

/**
 * The MVP section destinations. Each currently resolves to a placeholder page;
 * later roadmap slices (S-02…S-06) fill them in without changing this list or
 * the routing structure. `/` is the post-login home/landing.
 */
export const navItems: NavItem[] = [
  { to: '/', label: 'Home' },
  { to: '/products', label: 'Products' },
  { to: '/recipes', label: 'Recipes' },
  { to: '/meal-plan', label: 'Meal plan' },
  { to: '/goals', label: 'Daily goals' },
  { to: '/shopping-list', label: 'Shopping list' },
]

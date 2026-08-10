/** A primary navigation destination shown in the app-shell pill nav. */
export interface NavItem {
  /** Router path (absolute). */
  to: string
  /** Human-readable Polish label. */
  label: string
}

/**
 * The six primary sections, in the fixed order from the design mockups. `/` is
 * the post-login home/dashboard. Labels are the accessible names the shell nav
 * (and tests) rely on.
 */
export const navItems: NavItem[] = [
  { to: '/', label: 'Strona główna' },
  { to: '/products', label: 'Produkty' },
  { to: '/recipes', label: 'Przepisy' },
  { to: '/meal-plan', label: 'Plan posiłków' },
  { to: '/goals', label: 'Dzienne cele' },
  { to: '/shopping-list', label: 'Lista zakupów' },
]

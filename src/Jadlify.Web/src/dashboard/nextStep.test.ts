import { describe, expect, it } from 'vitest'
import { resolveNextStep, type NextStepInput } from './nextStep'

/** A fully set-up account with shopping already in progress. */
function ready(overrides: Partial<NextStepInput> = {}): NextStepInput {
  return {
    productCount: 12,
    recipeCount: 4,
    hasGoal: true,
    plannedMealCount: 3,
    activeListId: 'l1',
    ...overrides,
  }
}

describe('resolveNextStep', () => {
  it('sends a brand-new account to products first', () => {
    const step = resolveNextStep(ready({ productCount: 0, recipeCount: 0, hasGoal: false }))

    expect(step.id).toBe('products')
    expect(step.to).toBe('/products')
  })

  it('asks for a recipe once products exist', () => {
    expect(resolveNextStep(ready({ recipeCount: 0, hasGoal: false })).id).toBe('recipes')
  })

  it('asks for a goal once recipes exist', () => {
    const step = resolveNextStep(ready({ hasGoal: false }))

    expect(step.id).toBe('goal')
    expect(step.to).toBe('/goals')
  })

  it('asks to plan the day once a goal is set, handling the action in place', () => {
    const step = resolveNextStep(ready({ plannedMealCount: 0 }))

    expect(step.id).toBe('plan')
    expect(step.actionLabel).toBe('Dodaj posiłek')
    // No route: adding a meal opens the dashboard's own dialog.
    expect(step.to).toBeNull()
  })

  it('asks to create a list once the day is planned', () => {
    const step = resolveNextStep(ready({ activeListId: null }))

    expect(step.id).toBe('list')
    expect(step.to).toBe('/shopping-list')
  })

  it('points at the active list when everything else is done', () => {
    const step = resolveNextStep(ready({ activeListId: 'list-42' }))

    expect(step.id).toBe('shop')
    expect(step.to).toBe('/shopping-list/list-42')
  })

  it('resolves the earliest gap when several are open at once', () => {
    // Oracle: the chain is products → recipes → goal → plan → list; the first
    // unmet link wins because every later one depends on it.
    const step = resolveNextStep({
      productCount: 0,
      recipeCount: 0,
      hasGoal: false,
      plannedMealCount: 0,
      activeListId: null,
    })

    expect(step.id).toBe('products')
  })

  it('does not skip the goal step just because meals are already planned', () => {
    // Planning works without a goal, so a planned day must not mask the missing goal.
    expect(resolveNextStep(ready({ hasGoal: false, plannedMealCount: 5 })).id).toBe('goal')
  })
})

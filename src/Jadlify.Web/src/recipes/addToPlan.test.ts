import { describe, expect, it } from 'vitest'
import {
  buildAddToPlanUrl,
  readAddToPlanParams,
  stripAddToPlanParams,
} from './addToPlan'

describe('buildAddToPlanUrl', () => {
  it('carries recipe, date and return path in the URL', () => {
    const url = buildAddToPlanUrl({
      recipeId: 'r1',
      date: '2026-07-21',
      returnTo: '/recipes/r1?sort=NameAsc',
    })

    const params = new URLSearchParams(url.slice(url.indexOf('?') + 1))
    expect(url.startsWith('/meal-plan?')).toBe(true)
    expect(params.get('addRecipe')).toBe('r1')
    expect(params.get('date')).toBe('2026-07-21')
    expect(params.get('returnTo')).toBe('/recipes/r1?sort=NameAsc')
  })

  it('omits the optional parts when they are not supplied', () => {
    const url = buildAddToPlanUrl({ recipeId: 'r1' })

    expect(url).toBe('/meal-plan?addRecipe=r1')
  })
})

describe('readAddToPlanParams', () => {
  it('returns null when no recipe is handed off', () => {
    expect(readAddToPlanParams(new URLSearchParams('view=day'))).toBeNull()
  })

  it('reads a well-formed handoff', () => {
    const parsed = readAddToPlanParams(
      new URLSearchParams('addRecipe=r1&date=2026-07-21&returnTo=%2Frecipes%2Fr1'),
    )

    expect(parsed).toEqual({ recipeId: 'r1', date: '2026-07-21', returnTo: '/recipes/r1' })
  })

  it('drops a malformed date rather than passing it on', () => {
    const parsed = readAddToPlanParams(new URLSearchParams('addRecipe=r1&date=21-07-2026'))

    expect(parsed).toEqual({ recipeId: 'r1', date: undefined, returnTo: undefined })
  })

  it.each(['https://evil.example/steal', '//evil.example/steal', 'recipes/r1'])(
    'drops the off-site return path %s',
    (returnTo) => {
      const search = new URLSearchParams()
      search.set('addRecipe', 'r1')
      search.set('returnTo', returnTo)

      expect(readAddToPlanParams(search)?.returnTo).toBeUndefined()
    },
  )
})

describe('stripAddToPlanParams', () => {
  it('removes only the handoff parameters', () => {
    const stripped = stripAddToPlanParams(
      new URLSearchParams('addRecipe=r1&date=2026-07-21&returnTo=%2Frecipes%2Fr1&view=week'),
    )

    expect(stripped.toString()).toBe('view=week')
  })
})

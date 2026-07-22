import { describe, expect, it } from 'vitest'
import { entryMacroLine, kcalPercent, kcalStatus, macroTripletLabel } from './plannerMacros'

describe('plannerMacros', () => {
  it('reports "cel osiągnięty" within the ±5% band', () => {
    expect(kcalStatus(1950, 2000)?.tone).toBe('on-target')
    expect(kcalStatus(2050, 2000)?.tone).toBe('on-target')
  })

  it('reports remaining below the band and over above it', () => {
    const under = kcalStatus(1500, 2000)
    expect(under?.tone).toBe('under')
    expect(under?.label).toMatch(/zostało/)

    const over = kcalStatus(2400, 2000)
    expect(over?.tone).toBe('over')
    expect(over?.label).toMatch(/przekroczono/)
  })

  it('returns null when no usable goal is set', () => {
    expect(kcalStatus(1500, null)).toBeNull()
    expect(kcalStatus(1500, 0)).toBeNull()
  })

  it('clamps goal progress to 100%', () => {
    expect(kcalPercent(1000, 2000)).toBe(50)
    expect(kcalPercent(3000, 2000)).toBe(100)
    expect(kcalPercent(1000, null)).toBe(0)
  })

  it('formats compact macro readouts', () => {
    const macros = { calories: 250, protein: 12, fat: 8, carbohydrates: 30 }
    expect(macroTripletLabel(macros)).toBe('12 B · 8 T · 30 W')
    expect(entryMacroLine(macros)).toBe('250 kcal · 12 B · 8 T · 30 W')
  })
})

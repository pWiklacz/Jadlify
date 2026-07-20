import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { MacroCompareRow } from './MacroCompareRow'

describe('MacroCompareRow', () => {
  it('shows the value without a status when there is no goal', () => {
    render(<MacroCompareRow label="Białko" value={60} kind="protein" />)

    expect(screen.getByText('Białko')).toBeInTheDocument()
    expect(screen.queryByText(/zostało|cel osiągnięty|przekroczono/)).not.toBeInTheDocument()
  })

  it('reports the remaining amount below the goal', () => {
    render(<MacroCompareRow label="Białko" value={60} goal={100} kind="protein" />)

    expect(screen.getByText('zostało 40 g')).toBeInTheDocument()
  })

  it('reports "cel osiągnięty" within the tolerance band', () => {
    render(<MacroCompareRow label="Białko" value={98} goal={100} kind="protein" />)

    expect(screen.getByText('cel osiągnięty')).toBeInTheDocument()
  })

  it('reports the overshoot above the goal', () => {
    render(<MacroCompareRow label="Białko" value={120} goal={100} kind="protein" />)

    expect(screen.getByText('przekroczono o 20 g')).toBeInTheDocument()
  })

  it('uses kcal units for the calories kind', () => {
    render(<MacroCompareRow label="Kalorie" value={1600} goal={2000} kind="kcal" />)

    expect(screen.getByText('zostało 400 kcal')).toBeInTheDocument()
  })
})

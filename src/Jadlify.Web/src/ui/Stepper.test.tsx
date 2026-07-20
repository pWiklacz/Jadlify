import { useState } from 'react'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { Stepper } from './Stepper'

function ControlledStepper(props: {
  initial: number
  step: number
  min: number
  max: number
  unit?: string
}) {
  const [value, setValue] = useState(props.initial)
  return (
    <Stepper
      label="Liczba porcji"
      value={value}
      onChange={setValue}
      step={props.step}
      min={props.min}
      max={props.max}
      unit={props.unit}
    />
  )
}

describe('Stepper', () => {
  it('increments and decrements by the given step', async () => {
    const user = userEvent.setup()
    render(<ControlledStepper initial={1} step={0.5} min={0.5} max={5} />)

    const spin = screen.getByRole('spinbutton', { name: 'Liczba porcji' })
    expect(spin).toHaveAttribute('aria-valuenow', '1')

    await user.click(screen.getByRole('button', { name: 'Zwiększ: Liczba porcji' }))
    expect(spin).toHaveAttribute('aria-valuenow', '1.5')

    await user.click(screen.getByRole('button', { name: 'Zmniejsz: Liczba porcji' }))
    expect(spin).toHaveAttribute('aria-valuenow', '1')
  })

  it('disables the decrement button at the minimum', () => {
    render(<ControlledStepper initial={0.5} step={0.5} min={0.5} max={5} />)

    expect(screen.getByRole('button', { name: 'Zmniejsz: Liczba porcji' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Zwiększ: Liczba porcji' })).toBeEnabled()
  })

  it('disables the increment button at the maximum and clamps', () => {
    render(<ControlledStepper initial={5} step={0.5} min={0.5} max={5} />)

    expect(screen.getByRole('button', { name: 'Zwiększ: Liczba porcji' })).toBeDisabled()
  })

  it('exposes the value with its unit as aria-valuetext', () => {
    render(<ControlledStepper initial={120} step={10} min={10} max={500} unit="g" />)

    expect(screen.getByRole('spinbutton', { name: 'Liczba porcji' })).toHaveAttribute(
      'aria-valuetext',
      '120 g',
    )
  })
})

import { useState } from 'react'
import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { Dialog } from './Dialog'

function Harness({ role }: { role?: 'dialog' | 'alertdialog' }) {
  const [open, setOpen] = useState(false)
  return (
    <>
      <button type="button" onClick={() => setOpen(true)}>
        Otwórz
      </button>
      <Dialog
        open={open}
        onClose={() => setOpen(false)}
        title="Tytuł okna"
        role={role}
        footer={<button type="button">Zapisz</button>}
      >
        <button type="button">W środku</button>
      </Dialog>
    </>
  )
}

describe('Dialog', () => {
  it('is not mounted until opened and exposes its title as the accessible name', async () => {
    const user = userEvent.setup()
    render(<Harness />)

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Otwórz' }))

    expect(screen.getByRole('dialog', { name: 'Tytuł okna' })).toBeInTheDocument()
  })

  it('moves initial focus to the first focusable (the close button)', async () => {
    const user = userEvent.setup()
    render(<Harness />)

    await user.click(screen.getByRole('button', { name: 'Otwórz' }))

    expect(screen.getByRole('button', { name: 'Zamknij' })).toHaveFocus()
  })

  it('closes on Escape and returns focus to the trigger', async () => {
    const user = userEvent.setup()
    render(<Harness />)

    const trigger = screen.getByRole('button', { name: 'Otwórz' })
    await user.click(trigger)

    fireEvent.keyDown(screen.getByRole('dialog'), { key: 'Escape' })

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(trigger).toHaveFocus()
  })

  it('traps Tab focus within the panel', async () => {
    const user = userEvent.setup()
    render(<Harness />)

    await user.click(screen.getByRole('button', { name: 'Otwórz' }))
    const panel = screen.getByRole('dialog')
    const close = screen.getByRole('button', { name: 'Zamknij' })
    const save = screen.getByRole('button', { name: 'Zapisz' })

    // Forward from the last focusable wraps to the first.
    save.focus()
    fireEvent.keyDown(panel, { key: 'Tab' })
    expect(close).toHaveFocus()

    // Backward from the first focusable wraps to the last.
    close.focus()
    fireEvent.keyDown(panel, { key: 'Tab', shiftKey: true })
    expect(save).toHaveFocus()
  })

  it('dismisses a dialog on backdrop click', async () => {
    const user = userEvent.setup()
    render(<Harness />)

    await user.click(screen.getByRole('button', { name: 'Otwórz' }))
    const overlay = screen.getByRole('dialog').parentElement as HTMLElement

    fireEvent.mouseDown(overlay)

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('does not dismiss an alertdialog on backdrop click', async () => {
    const user = userEvent.setup()
    render(<Harness role="alertdialog" />)

    await user.click(screen.getByRole('button', { name: 'Otwórz' }))
    const overlay = screen.getByRole('alertdialog').parentElement as HTMLElement

    fireEvent.mouseDown(overlay)

    expect(screen.getByRole('alertdialog')).toBeInTheDocument()
  })
})

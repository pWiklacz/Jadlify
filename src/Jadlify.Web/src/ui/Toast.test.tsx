import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { Toast } from './Toast'

describe('Toast', () => {
  it('uses role="status" for non-error tones', () => {
    render(<Toast tone="success" message="Zapisano zmiany" />)

    const toast = screen.getByRole('status')
    expect(toast).toHaveTextContent('Zapisano zmiany')
    expect(toast).toHaveAttribute('aria-live', 'polite')
  })

  it('uses role="alert" for the error tone', () => {
    render(<Toast tone="error" message="Nie udało się zapisać" />)

    const toast = screen.getByRole('alert')
    expect(toast).toHaveTextContent('Nie udało się zapisać')
    expect(toast).toHaveAttribute('aria-live', 'assertive')
    expect(screen.queryByRole('status')).not.toBeInTheDocument()
  })

  it('renders an optional inline action and fires it on click', async () => {
    const user = userEvent.setup()
    const onClick = vi.fn()
    render(
      <Toast tone="info" message="Usunięto wpis" action={{ label: 'Cofnij', onClick }} />,
    )

    await user.click(screen.getByRole('button', { name: 'Cofnij' }))

    expect(onClick).toHaveBeenCalledTimes(1)
  })
})

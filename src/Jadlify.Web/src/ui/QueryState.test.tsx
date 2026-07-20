import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { QueryState } from './QueryState'

describe('QueryState', () => {
  it('renders a polite loading state', () => {
    render(
      <QueryState isPending isError={false} loadingLabel="Wczytujemy Twoje produkty…">
        <p>Treść</p>
      </QueryState>,
    )

    const status = screen.getByRole('status')
    expect(status).toHaveTextContent('Wczytujemy Twoje produkty…')
    expect(screen.queryByText('Treść')).not.toBeInTheDocument()
  })

  it('renders an error state and wires retry', async () => {
    const user = userEvent.setup()
    const onRetry = vi.fn()
    render(
      <QueryState isPending={false} isError onRetry={onRetry}>
        <p>Treść</p>
      </QueryState>,
    )

    expect(screen.getByRole('alert')).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'Wczytaj ponownie' }))
    expect(onRetry).toHaveBeenCalledTimes(1)
  })

  it('renders the empty state when empty', () => {
    render(
      <QueryState
        isPending={false}
        isError={false}
        isEmpty
        empty={{ title: 'Brak produktów' }}
      >
        <p>Treść</p>
      </QueryState>,
    )

    expect(screen.getByRole('heading', { name: 'Brak produktów' })).toBeInTheDocument()
    expect(screen.queryByText('Treść')).not.toBeInTheDocument()
  })

  it('renders children on success', () => {
    render(
      <QueryState isPending={false} isError={false}>
        <p>Treść</p>
      </QueryState>,
    )

    expect(screen.getByText('Treść')).toBeInTheDocument()
  })
})

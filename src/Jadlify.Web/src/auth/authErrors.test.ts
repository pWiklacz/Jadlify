import { describe, expect, it } from 'vitest'
import { toAuthMessage } from './authErrors'

describe('toAuthMessage', () => {
  it('maps invalid credentials to a friendly message (by code)', () => {
    expect(
      toAuthMessage({ code: 'invalid_credentials', message: 'Invalid login credentials' }),
    ).toBe('Nieprawidłowy e-mail lub hasło.')
  })

  it('maps invalid credentials by message when code is absent', () => {
    expect(toAuthMessage({ message: 'Invalid login credentials' })).toBe(
      'Nieprawidłowy e-mail lub hasło.',
    )
  })

  it('maps an already-registered e-mail to a friendly message', () => {
    expect(
      toAuthMessage({ code: 'user_already_exists', message: 'User already registered' }),
    ).toBe('Konto z tym adresem e-mail już istnieje. Spróbuj się zalogować.')
  })

  it('maps a weak/short password to a friendly message', () => {
    expect(
      toAuthMessage({
        code: 'weak_password',
        message: 'Password should be at least 6 characters',
      }),
    ).toBe('Hasło musi mieć co najmniej 6 znaków.')
  })

  it('falls back to a safe default for network / unknown errors', () => {
    expect(toAuthMessage({ message: 'Failed to fetch' })).toBe(
      'Coś poszło nie tak. Spróbuj ponownie.',
    )
    expect(toAuthMessage(new Error('boom'))).toBe('Coś poszło nie tak. Spróbuj ponownie.')
    expect(toAuthMessage(null)).toBe('Coś poszło nie tak. Spróbuj ponownie.')
    expect(toAuthMessage('weird string')).toBe('Coś poszło nie tak. Spróbuj ponownie.')
  })

  it('never echoes the attempted credentials in the returned message', () => {
    const email = 'victim@example.com'
    const password = 'SuperSecret123'
    // Even if the SDK surfaced the credentials in its raw message, the mapped
    // string must not leak them.
    const message = toAuthMessage({
      code: 'invalid_credentials',
      message: `Invalid login credentials for ${email} / ${password}`,
    })

    expect(message).not.toContain(email)
    expect(message).not.toContain(password)
  })
})

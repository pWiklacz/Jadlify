/**
 * Converts a Supabase auth error (or any thrown/returned value) into a short,
 * user-facing Polish message. The form renders ONLY these mapped strings, never
 * the raw SDK message.
 *
 * Pure and side-effect free: it never logs and never includes the attempted
 * e-mail or password, so credentials cannot leak through the error UI
 * (PRD: hasła i tokeny nigdy nie pojawiają się w logach).
 */
export function toAuthMessage(error: unknown): string {
  const code = readField(error, 'code')
  const message = readField(error, 'message').toLowerCase()

  if (code === 'invalid_credentials' || message.includes('invalid login credentials')) {
    return 'Nieprawidłowy e-mail lub hasło.'
  }

  if (
    code === 'user_already_exists' ||
    code === 'email_exists' ||
    message.includes('user already registered') ||
    message.includes('already been registered')
  ) {
    return 'Konto z tym adresem e-mail już istnieje. Spróbuj się zalogować.'
  }

  if (code === 'weak_password' || message.includes('password should be at least')) {
    return 'Hasło musi mieć co najmniej 6 znaków.'
  }

  return 'Coś poszło nie tak. Spróbuj ponownie.'
}

/** Defensively read a string field off an unknown value (AuthError or plain object). */
function readField(error: unknown, key: 'code' | 'message'): string {
  if (typeof error === 'object' && error !== null && key in error) {
    const value = (error as Record<string, unknown>)[key]
    return typeof value === 'string' ? value : ''
  }
  return ''
}

import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, createApiClient } from './client'

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

describe('createApiClient', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('attaches the bearer token from the resolver on each request', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ userId: 'user-123' }))
    vi.stubGlobal('fetch', fetchMock)

    const client = createApiClient(async () => 'token-abc')
    const body = await client.get<{ userId: string }>('/api/me')

    expect(body).toEqual({ userId: 'user-123' })
    expect(fetchMock).toHaveBeenCalledTimes(1)

    const [path, init] = fetchMock.mock.calls[0]
    expect(path).toBe('/api/me')
    const headers = new Headers(init.headers)
    expect(headers.get('Authorization')).toBe('Bearer token-abc')
  })

  it('resolves the token per request (fresh, not cached)', async () => {
    // Fresh Response per call — a Response body can only be read once.
    const fetchMock = vi.fn()
    fetchMock.mockImplementation(async () => jsonResponse({ ok: true }))
    vi.stubGlobal('fetch', fetchMock)

    const tokens = ['first', 'second']
    const client = createApiClient(async () => tokens.shift() ?? null)

    await client.get('/api/me')
    await client.get('/api/me')

    expect(new Headers(fetchMock.mock.calls[0][1].headers).get('Authorization')).toBe(
      'Bearer first',
    )
    expect(new Headers(fetchMock.mock.calls[1][1].headers).get('Authorization')).toBe(
      'Bearer second',
    )
  })

  it('omits the Authorization header when no token is available', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({}))
    vi.stubGlobal('fetch', fetchMock)

    const client = createApiClient(async () => null)
    await client.get('/api/me')

    const headers = new Headers(fetchMock.mock.calls[0][1].headers)
    expect(headers.has('Authorization')).toBe(false)
  })

  it('throws ApiError carrying the status on a non-2xx response', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('nope', { status: 401 }))
    vi.stubGlobal('fetch', fetchMock)

    const client = createApiClient(async () => null)

    await expect(client.get('/api/me')).rejects.toBeInstanceOf(ApiError)
    await expect(client.get('/api/me')).rejects.toMatchObject({ status: 401 })
  })
})

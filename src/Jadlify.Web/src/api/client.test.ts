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

  it('posts a JSON body with the Content-Type header and returns the parsed response', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ id: 'p-1' }, 201))
    vi.stubGlobal('fetch', fetchMock)

    const client = createApiClient(async () => 'token-abc')
    const result = await client.post<{ id: string }>('/api/products', { name: 'Oats' })

    expect(result).toEqual({ id: 'p-1' })
    const [path, init] = fetchMock.mock.calls[0]
    expect(path).toBe('/api/products')
    expect(init.method).toBe('POST')
    expect(init.body).toBe(JSON.stringify({ name: 'Oats' }))
    expect(new Headers(init.headers).get('Content-Type')).toBe('application/json')
  })

  it('returns undefined from put on a 204 No Content response', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetchMock)

    const client = createApiClient(async () => null)
    const result = await client.put<void>('/api/products/p-1', { name: 'Oats' })

    expect(result).toBeUndefined()
    const [, init] = fetchMock.mock.calls[0]
    expect(init.method).toBe('PUT')
    expect(init.body).toBe(JSON.stringify({ name: 'Oats' }))
  })

  it('patches a JSON body and returns the parsed response', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ version: 3 }))
    vi.stubGlobal('fetch', fetchMock)

    const client = createApiClient(async () => null)
    const result = await client.patch<{ version: number }>('/api/shopping-lists/l-1/items/i-1', {
      isBought: true,
      expectedVersion: 2,
    })

    expect(result).toEqual({ version: 3 })
    const [path, init] = fetchMock.mock.calls[0]
    expect(path).toBe('/api/shopping-lists/l-1/items/i-1')
    expect(init.method).toBe('PATCH')
    expect(init.body).toBe(JSON.stringify({ isBought: true, expectedVersion: 2 }))
    expect(new Headers(init.headers).get('Content-Type')).toBe('application/json')
  })

  it('issues a DELETE without a body and resolves to void', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetchMock)

    const client = createApiClient(async () => null)
    await expect(client.del('/api/products/p-1')).resolves.toBeUndefined()

    const [path, init] = fetchMock.mock.calls[0]
    expect(path).toBe('/api/products/p-1')
    expect(init.method).toBe('DELETE')
    expect(init.body).toBeUndefined()
  })
})

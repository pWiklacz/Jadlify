/** Thrown for any non-2xx API response. */
export class ApiError extends Error {
  readonly status: number
  readonly body: unknown

  constructor(status: number, message: string, body: unknown = null) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.body = body
  }
}

/** Resolves the bearer access token for the current request, or null when signed out. */
export type AccessTokenResolver = () => Promise<string | null>

export interface ApiClient {
  get<T>(path: string): Promise<T>
  post<T>(path: string, body: unknown): Promise<T>
  put<T>(path: string, body: unknown): Promise<T>
  del(path: string): Promise<void>
}

/**
 * Builds a typed fetch wrapper that attaches the current bearer token (resolved
 * per request, so rotated tokens are picked up) and surfaces non-2xx as ApiError.
 * Targets relative `/api/...` URLs — single-origin, no CORS.
 */
export function createApiClient(getAccessToken: AccessTokenResolver): ApiClient {
  async function request<T>(path: string, init: RequestInit = {}, body?: unknown): Promise<T> {
    const token = await getAccessToken()

    const headers = new Headers(init.headers)
    headers.set('Accept', 'application/json')
    if (token) {
      headers.set('Authorization', `Bearer ${token}`)
    }

    let serializedBody: BodyInit | undefined
    if (body !== undefined) {
      headers.set('Content-Type', 'application/json')
      serializedBody = JSON.stringify(body)
    }

    const response = await fetch(path, { ...init, headers, body: serializedBody })

    if (!response.ok) {
      const body = await safeReadBody(response)
      throw new ApiError(
        response.status,
        `Request to ${path} failed with status ${response.status}`,
        body,
      )
    }

    if (response.status === 204) {
      return undefined as T
    }

    return (await response.json()) as T
  }

  return {
    get: <T>(path: string) => request<T>(path, { method: 'GET' }),
    post: <T>(path: string, body: unknown) => request<T>(path, { method: 'POST' }, body),
    put: <T>(path: string, body: unknown) => request<T>(path, { method: 'PUT' }, body),
    del: (path: string) => request<void>(path, { method: 'DELETE' }),
  }
}

async function safeReadBody(response: Response): Promise<unknown> {
  try {
    const text = await response.text()
    if (!text) {
      return null
    }
    try {
      return JSON.parse(text)
    } catch {
      return text
    }
  } catch {
    return null
  }
}

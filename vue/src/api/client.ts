const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5000/api'

interface RequestOptions extends Omit<RequestInit, 'body'> {
  body?: unknown
}

export async function apiClient<T>(
  path: string,
  options: RequestOptions = {},
): Promise<T> {
  const { body, headers: customHeaders, ...rest } = options

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...customHeaders as Record<string, string>,
  }

  const token = localStorage.getItem('auth_token')
  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }

  const response = await fetch(`${API_BASE}${path}`, {
    ...rest,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({ detail: 'An unexpected error occurred.' }))
    throw new ApiError(response.status, error)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json()
}

export async function apiUpload<T>(
  path: string,
  file: File,
  method = 'POST',
): Promise<T> {
  const formData = new FormData()
  formData.append('file', file)

  const headers: Record<string, string> = {}
  const token = localStorage.getItem('auth_token')
  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }

  const response = await fetch(`${API_BASE}${path}`, {
    method,
    headers,
    body: formData,
  })

  if (!response.ok) {
    const error = await response.json().catch(() => ({ detail: 'An unexpected error occurred.' }))
    throw new ApiError(response.status, error)
  }

  return response.json()
}

export class ApiError extends Error {
  constructor(
    public status: number,
    public data: unknown,
  ) {
    super(`API error ${status}`)
    this.name = 'ApiError'
  }
}

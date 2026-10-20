import type { AccessToken, SlipwayDetails, SlipwaySummary, User, Water } from './types'

const TOKEN_KEY = 'slipways.token'

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

export function getStoredToken(): string | null {
  try {
    return localStorage.getItem(TOKEN_KEY)
  } catch {
    return null
  }
}

export function storeToken(token: string | null) {
  try {
    if (token) localStorage.setItem(TOKEN_KEY, token)
    else localStorage.removeItem(TOKEN_KEY)
  } catch {
    // Storage can be unavailable (private mode); the session then lasts until reload.
  }
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')
  if (init.body) headers.set('Content-Type', 'application/json')
  const token = getStoredToken()
  if (token) headers.set('Authorization', `Bearer ${token}`)

  const response = await fetch(`/api${path}`, { ...init, headers })
  if (!response.ok) {
    throw new ApiError(response.status, await errorMessage(response))
  }
  if (response.status === 204 || response.headers.get('Content-Length') === '0') {
    return undefined as T
  }
  const text = await response.text()
  return (text ? JSON.parse(text) : undefined) as T
}

async function errorMessage(response: Response): Promise<string> {
  try {
    const problem = await response.json()
    if (problem.errors) return Object.values(problem.errors).flat().join(' ')
    return problem.detail ?? problem.title ?? response.statusText
  } catch {
    return response.statusText
  }
}

export const api = {
  slipways: (search?: string, signal?: AbortSignal) =>
    request<SlipwaySummary[]>(search ? `/slipways?search=${encodeURIComponent(search)}` : '/slipways', { signal }),
  slipway: (id: string, signal?: AbortSignal) => request<SlipwayDetails>(`/slipways/${id}`, { signal }),
  waters: (signal?: AbortSignal) => request<Water[]>('/waters', { signal }),
  login: (email: string, password: string) =>
    request<AccessToken>('/account/login', { method: 'POST', body: JSON.stringify({ email, password }) }),
  register: (email: string, password: string) =>
    request<void>('/account/register', { method: 'POST', body: JSON.stringify({ email, password }) }),
  me: () => request<User>('/account/me'),
}

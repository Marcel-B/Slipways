import type { SlipwayDetails, SlipwaySummary, WaterDetails, WaterSummary } from './types'

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')

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
  waters: (search?: string, signal?: AbortSignal) =>
    request<WaterSummary[]>(search ? `/waters?search=${encodeURIComponent(search)}` : '/waters', { signal }),
  water: (id: string, signal?: AbortSignal) => request<WaterDetails>(`/waters/${id}`, { signal }),
}

import type {
  Marina,
  MarinaDetails,
  NearbySlipway,
  ProofChallenge,
  SuggestionCreated,
  SuggestionRequest,
  SuggestionStatus,
  SlipwayDetails,
  SlipwaySummary,
  WaterDetails,
  WaterShapes,
  WaterSummary,
} from './types'

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

/** A suggested new slipway lies close to ones we have; sending it again with `confirmNew` takes it anyway. */
export class NearbySlipwaysError extends ApiError {
  readonly slipways: NearbySlipway[]

  constructor(slipways: NearbySlipway[]) {
    super(409, 'In der Nähe gibt es schon Slipanlagen.')
    this.slipways = slipways
  }
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')

  const response = await fetch(`/api${path}`, { ...init, headers })
  if (response.status === 409 && path === '/suggestions') {
    throw new NearbySlipwaysError(((await response.json()) as { slipways: NearbySlipway[] }).slipways)
  }
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
  waterShapes: (search?: string, signal?: AbortSignal) =>
    request<WaterShapes>(search ? `/waters/shapes?search=${encodeURIComponent(search)}` : '/waters/shapes', { signal }),
  water: (id: string, signal?: AbortSignal) => request<WaterDetails>(`/waters/${id}`, { signal }),
  marinas: (search?: string, signal?: AbortSignal) =>
    request<Marina[]>(search ? `/marinas?search=${encodeURIComponent(search)}` : '/marinas', { signal }),
  marina: (id: string, signal?: AbortSignal) => request<MarinaDetails>(`/marinas/${id}`, { signal }),
  challenge: () => request<ProofChallenge>('/suggestions/challenge'),
  suggest: (suggestion: SuggestionRequest) =>
    request<SuggestionCreated>('/suggestions', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(suggestion),
    }),
  suggestion: (id: string, token: string, signal?: AbortSignal) =>
    request<SuggestionStatus>(`/suggestions/${id}?token=${encodeURIComponent(token)}`, { signal }),
  withdrawSuggestion: (id: string, token: string) =>
    request<void>(`/suggestions/${id}?token=${encodeURIComponent(token)}`, { method: 'DELETE' }),
}

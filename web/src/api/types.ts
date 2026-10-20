export interface Water {
  id: string
  name: string
}

export interface SlipwaySummary {
  id: string
  name: string
  latitude: number
  longitude: number
  postalCode: string | null
  city: string | null
  country: string | null
  water: Water
}

export interface SlipwayDetails extends SlipwaySummary {
  street: string | null
  comment: string | null
  rating: number
  costs: number | null
  pro: string | null
  contra: string | null
  marina: { id: string; name: string; url: string | null } | null
  extras: string[]
  created: string
  updated: string | null
}

export interface User {
  id: string
  email: string
  displayName: string | null
}

export interface AccessToken {
  tokenType: string
  accessToken: string
  expiresIn: number
  refreshToken: string
}

import type { PlaceInfo, PlaceType } from '../api/types'

/** A field users can suggest a value for, as the server knows it (SuggestionFields.cs). */
export interface FieldDef {
  name: string
  label: string
  /** Choices as value and label; without them the field takes text. */
  options?: [string, string][]
  placeholder?: string
  inputMode?: 'decimal' | 'tel' | 'email'
}

const yesNo: [string, string][] = [
  ['yes', 'ja'],
  ['no', 'nein'],
]

export const FIELDS: Record<string, FieldDef> = {
  fee: {
    name: 'fee',
    label: 'Gebühr',
    options: [
      ['no', 'kostenlos'],
      ['yes', 'kostenpflichtig'],
    ],
  },
  costs: { name: 'costs', label: 'Kosten in Euro', placeholder: 'z. B. 5', inputMode: 'decimal' },
  access: {
    name: 'access',
    label: 'Zugang',
    options: [
      ['yes', 'öffentlich'],
      ['permissive', 'geduldet'],
      ['customers', 'nur für Kunden'],
      ['members', 'nur für Mitglieder'],
      ['private', 'privat'],
      ['no', 'kein Zugang'],
    ],
  },
  opening_hours: { name: 'opening_hours', label: 'Öffnungszeiten', placeholder: 'z. B. Mo–Fr 8–18 Uhr, Sa 9–12 Uhr' },
  surface: {
    name: 'surface',
    label: 'Untergrund',
    options: [
      ['concrete', 'Beton'],
      ['asphalt', 'Asphalt'],
      ['paving_stones', 'Pflaster'],
      ['gravel', 'Kies'],
      ['wood', 'Holz'],
      ['sand', 'Sand'],
      ['grass', 'Gras'],
      ['unpaved', 'unbefestigt'],
    ],
  },
  operator: { name: 'operator', label: 'Betreiber', placeholder: 'z. B. Stadt, Verein oder Marina' },
  phone: { name: 'phone', label: 'Telefon', placeholder: '+49 …', inputMode: 'tel' },
  email: { name: 'email', label: 'E-Mail', placeholder: 'hafen@example.de', inputMode: 'email' },
  trailer_parking: { name: 'trailer_parking', label: 'Parkplatz für Gespann', options: yesNo },
  jetty: { name: 'jetty', label: 'Steg', options: yesNo },
  toilet: { name: 'toilet', label: 'Toilette', options: yesNo },
  crane: { name: 'crane', label: 'Kran', options: yesNo },
  steep: { name: 'steep', label: 'Rampe steil', options: yesNo },
  slippery_at_low_water: { name: 'slippery_at_low_water', label: 'Rutschig bei Niedrigwasser', options: yesNo },
  key_required: { name: 'key_required', label: 'Nur mit Schlüssel', options: yesNo },
}

/** Facts about a slipway to tick, shown together under „Was gibt es dort?“. */
export const FACTS = ['trailer_parking', 'jetty', 'toilet', 'crane', 'steep', 'slippery_at_low_water', 'key_required']

/** The few fields a place should have; only these are asked for when missing. */
export const WANTED: Record<PlaceType, string[]> = {
  slipway: ['fee', 'access', 'opening_hours', 'surface'],
  marina: ['phone', 'opening_hours', 'access', 'fee'],
}

export function optionLabel(field: string, value: string) {
  return FIELDS[field]?.options?.find(([v]) => v === value)?.[1] ?? value
}

/** The newest value users sent in for a field, if any; it wins over the place's own one. */
export function fromUsers(infos: PlaceInfo[], field: string) {
  return infos
    .filter((i) => i.field === field && i.source === 'user')
    .sort((a, b) => b.retrieved.localeCompare(a.retrieved))[0]
}

export const monthYear = (date: string) =>
  new Date(date).toLocaleDateString('de-DE', { month: '2-digit', year: 'numeric' })

/** The OpenStreetMap editor for an element such as "node/123". */
export function osmEditUrl(element: string) {
  const [type, id] = element.split('/')
  return `https://www.openstreetmap.org/edit?${type}=${id}`
}

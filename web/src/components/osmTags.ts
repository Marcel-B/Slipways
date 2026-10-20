// OpenStreetMap tag values in German; values without a label are shown as they are.

const feeLabels: Record<string, string> = { yes: 'kostenpflichtig', no: 'kostenlos' }

const accessLabels: Record<string, string> = {
  yes: 'öffentlich',
  permissive: 'geduldet',
  customers: 'nur für Kunden',
  members: 'nur für Mitglieder',
  private: 'privat',
  no: 'kein Zugang',
}

const surfaceLabels: Record<string, string> = {
  concrete: 'Beton',
  asphalt: 'Asphalt',
  paved: 'befestigt',
  paving_stones: 'Pflaster',
  sett: 'Pflaster',
  gravel: 'Kies',
  fine_gravel: 'Kies',
  wood: 'Holz',
  sand: 'Sand',
  grass: 'Gras',
  unpaved: 'unbefestigt',
}

export const feeLabel = (fee: string) => feeLabels[fee] ?? fee
export const accessLabel = (access: string) => accessLabels[access] ?? access
export const surfaceLabel = (surface: string) => surfaceLabels[surface] ?? surface

export const osmUrl = (element: string) => `https://www.openstreetmap.org/${element}`

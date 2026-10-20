// Angaben zum Betreiber für Impressum, Datenschutz und Support.
// Werte in eckigen Klammern sind Platzhalter und werden auf den Seiten markiert, bis sie ersetzt sind.
export const operator = {
  name: 'Marcel Benders',
  street: 'Scheiblerstraße 111',
  city: '47800 Krefeld',
  country: 'Deutschland',
  email: 'slipways@marcelbenders.com',
  // Zweiter schneller Kontaktweg neben der E-Mail; leer lassen, wenn es keinen gibt
  phone: '',
  // Nur angeben, wenn vorhanden
  vatId: '',
}

export const legalUpdatedAt = '20. Oktober 2026'

export function isPlaceholder(value: string) {
  return value.startsWith('[')
}

// Angaben zum Betreiber für Impressum, Datenschutz und Support.
// Werte in eckigen Klammern sind Platzhalter und werden auf den Seiten markiert, bis sie ersetzt sind.
export const operator = {
  name: '[Vor- und Nachname bzw. Firma]',
  street: '[Straße und Hausnummer]',
  city: '[PLZ und Ort]',
  country: 'Deutschland',
  email: '[E-Mail-Adresse]',
  // Zweiter schneller Kontaktweg neben der E-Mail; leer lassen, wenn es keinen gibt
  phone: '[Telefonnummer]',
  // Nur angeben, wenn vorhanden
  vatId: '',
}

export const legalUpdatedAt = '20. Oktober 2026'

export function isPlaceholder(value: string) {
  return value.startsWith('[')
}

// Moderation of suggestions; only reachable in the tailnet, see deploy/README.md.
'use strict'

const kinds = {
  new_slipway: 'Neue Slipanlage',
  edit: 'Ergänzung',
  tip: 'Tipp',
  report: 'Problem gemeldet',
}

const fieldLabels = {
  fee: 'Gebühr',
  costs: 'Kosten (€)',
  access: 'Zugang',
  opening_hours: 'Öffnungszeiten',
  surface: 'Untergrund',
  operator: 'Betreiber',
  phone: 'Telefon',
  email: 'E-Mail',
  trailer_parking: 'Parkplatz für Gespann',
  jetty: 'Steg',
  toilet: 'Toilette',
  crane: 'Kran',
  steep: 'Rampe steil',
  slippery_at_low_water: 'Rutschig bei Niedrigwasser',
  key_required: 'Nur mit Schlüssel',
}

const yesNo = { yes: 'ja', no: 'nein' }
const valueLabels = {
  fee: { yes: 'kostenpflichtig', no: 'kostenlos' },
  access: { yes: 'öffentlich', permissive: 'geduldet', customers: 'nur für Kunden', members: 'nur für Mitglieder', private: 'privat', no: 'kein Zugang' },
  surface: { concrete: 'Beton', asphalt: 'Asphalt', paving_stones: 'Pflaster', gravel: 'Kies', wood: 'Holz', sand: 'Sand', grass: 'Gras', unpaved: 'unbefestigt' },
}
const label = (field, value) => (value === null ? '–' : ((valueLabels[field] ?? yesNo)[value] ?? value))

const publicSite = 'https://slipways.de'
const dateFormat = new Intl.DateTimeFormat('de-DE', { dateStyle: 'medium', timeStyle: 'short' })
let status = 'pending'

const list = document.getElementById('list')
const message = document.getElementById('message')

function element(tag, props = {}, ...children) {
  const node = document.createElement(tag)
  Object.assign(node, props)
  node.append(...children.filter((c) => c !== null && c !== undefined))
  return node
}

function link(href, text) {
  return element('a', { href, target: '_blank', rel: 'noreferrer', textContent: text })
}

async function load() {
  message.textContent = 'Lade…'
  list.replaceChildren()
  try {
    const response = await fetch(`/api/moderation/suggestions?status=${status}`)
    if (!response.ok) throw new Error(response.statusText)
    const suggestions = await response.json()
    if (status === 'pending') document.getElementById('pending-count').textContent = `(${suggestions.length})`
    message.textContent = suggestions.length === 0 ? 'Nichts da.' : ''
    list.replaceChildren(...suggestions.map(card))
  } catch (error) {
    message.textContent = `Laden fehlgeschlagen: ${error.message}`
  }
}

function card(s) {
  const placePath = s.placeType === 'marina' ? 'marinas' : 'slipways'
  const title = s.kind === 'new_slipway'
    ? element('h2', { textContent: s.name })
    : element('h2', {}, s.placeName ? link(`${publicSite}/${placePath}/${s.placeId}`, s.placeName) : 'Eintrag gelöscht')

  const meta = [
    `eingegangen ${dateFormat.format(new Date(s.created))}`,
    s.decided && `entschieden ${dateFormat.format(new Date(s.decided))}`,
    s.placeSource === 'osm' && 'Eintrag aus OpenStreetMap: landet getrennt, unter ODbL',
    s.fromServer && 'vom OSM-Abgleich',
  ].filter(Boolean).join(' · ')

  const parts = [element('span', { className: 'kind', textContent: kinds[s.kind] ?? s.kind }), title, element('p', { className: 'meta', textContent: meta })]

  if (s.latitude !== null) {
    const position = `${s.latitude.toFixed(5)}, ${s.longitude.toFixed(5)}`
    parts.push(element('p', {}, 'Position: ', link(`https://www.openstreetmap.org/?mlat=${s.latitude}&mlon=${s.longitude}#map=17/${s.latitude}/${s.longitude}`, position)))
  }
  if (s.nearby.length > 0) {
    parts.push(element('p', { className: 'warn', textContent: 'In der Nähe: ' + s.nearby.map((n) => `${n.name} (${n.distance} m)`).join(', ') }))
  }
  if (s.fields.length > 0) {
    const rows = s.fields.map((f) => element('tr', {},
      element('td', { textContent: fieldLabels[f.field] ?? f.field }),
      element('td', { textContent: label(f.field, f.value) }),
      element('td', { textContent: label(f.field, f.current) })))
    parts.push(element('table', {},
      element('thead', {}, element('tr', {}, element('th', { textContent: 'Feld' }), element('th', { textContent: 'Vorschlag' }), element('th', { textContent: 'jetzt' }))),
      element('tbody', {}, ...rows)))
  }
  if (s.text) parts.push(element('p', { className: 'text', textContent: s.text }))

  if (s.status === 'pending') {
    const accept = element('button', { type: 'button', className: 'accept', textContent: 'Annehmen' })
    const reject = element('button', { type: 'button', className: 'reject', textContent: 'Ablehnen' })
    accept.addEventListener('click', () => decide(s.id, 'accept', [accept, reject]))
    reject.addEventListener('click', () => decide(s.id, 'reject', [accept, reject]))
    parts.push(element('div', { className: 'actions' }, accept, reject))
  }
  return element('li', { className: 'card' }, ...parts)
}

async function decide(id, decision, buttons) {
  buttons.forEach((b) => (b.disabled = true))
  const response = await fetch(`/api/moderation/suggestions/${id}/${decision}`, {
    method: 'POST',
    headers: { 'X-Moderation': '1' },
  })
  if (response.ok) {
    await load()
  } else {
    message.textContent = `Fehlgeschlagen: ${await response.text() || response.statusText}`
    buttons.forEach((b) => (b.disabled = false))
  }
}

document.querySelectorAll('nav button').forEach((button) => {
  button.addEventListener('click', () => {
    status = button.dataset.status
    document.querySelectorAll('nav button').forEach((b) => b.setAttribute('aria-pressed', String(b === button)))
    load()
  })
})

load()

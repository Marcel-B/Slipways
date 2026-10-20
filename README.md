# Slipways

Slipways sammelt Slipanlagen (Bootsrampen) an deutschen und niederländischen Gewässern.

| Teil | Ordner | Technik |
| --- | --- | --- |
| Backend | [`backend/`](backend) | .NET 10, ASP.NET Core Minimal APIs, EF Core mit SQLite, ASP.NET Identity |
| Web | [`web/`](web) | React 19, Vite, TypeScript, TanStack Query, React Router, Leaflet |

## Lokal starten

Voraussetzung: Docker mit Compose.

```sh
docker compose up --build
```

- Web: http://localhost:8080
- API: http://localhost:5080 (OpenAPI-Oberfläche unter http://localhost:5080/scalar)
- Datenbank: SQLite-Datei `slipways.db` im Docker-Volume `slipways_api-data`

Beim ersten Start legt die API das Schema an und spielt die mitgelieferten Daten ein (Slipanlagen, Gewässer, Marinas, Pegelstationen, Motorservices).

## Pegelstände

Ein Hintergrund-Job in der API holt alle 15 Minuten die aktuellen Wasserstände aller Pegel von [PEGELONLINE](https://www.pegelonline.wsv.de/) (WSV, Lizenz DL-DE->Zero-2.0) und speichert sie an den Pegelstationen. Im selben Abruf gleicht er die Stationen ab: neue Pegel und Gewässer werden angelegt, vorhandene über die PEGELONLINE-ID bzw. die Pegelnummer aktualisiert. Clients fragen PEGELONLINE nie selbst ab, sondern lesen `/api/stations`.

Einstellungen (Abschnitt `Pegelonline`, z. B. als Umgebungsvariable `Pegelonline__Enabled=false`):

| Schlüssel | Standard | Bedeutung |
| --- | --- | --- |
| `Enabled` | `true` | Job ein/aus |
| `Interval` | `00:15:00` | Abstand der Abrufe |
| `BaseUrl` | `https://www.pegelonline.wsv.de/webservices/rest-api/v2/` | REST-API |

## Gewässerverläufe

Die Detailseite eines Gewässers hebt seinen Verlauf (Flüsse, Kanäle) bzw. seine Fläche (Seen) aus [OpenStreetMap](https://www.openstreetmap.org/) hervor. Die Übersicht zeichnet die Verläufe aller Gewässer, die die Suche findet, und hebt das angeklickte hervor; dafür liefert `/api/waters/shapes` sie gröber vereinfacht (etwa 500 m, zusammen rund 160 KB, mit gzip rund 55 KB). Welche OSM-Elemente zu einem Gewässer gehören, steht in [`waterShapes.json`](backend/src/Slipways.Infrastructure/Seed/waterShapes.json), zugeordnet über den Kurznamen, den auch PEGELONLINE verwendet. Eine leere Liste heißt: geprüft, es gibt keinen passenden Verlauf (Nord- und Ostsee, Grundwassermessstellen, Abschnitte, die OSM nicht kennt).

Ein Hintergrund-Job in der API übernimmt die Zuordnung beim Start und holt die Formen bei [Nominatim](https://nominatim.org/), auf etwa 100 m vereinfacht (zusammen rund 0,6 MB). Danach prüft er einmal am Tag, ob eine Form älter als 30 Tage ist. Clients fragen Nominatim nie selbst ab, sondern lesen `/api/waters/{id}`.

Einstellungen (Abschnitt `Nominatim`):

| Schlüssel | Standard | Bedeutung |
| --- | --- | --- |
| `Enabled` | `true` | Job ein/aus |
| `Interval` | `1.00:00:00` | Abstand der Prüfungen |
| `RefreshAfter` | `30.00:00:00` | Alter, ab dem eine Form neu geholt wird |
| `RequestSpacing` | `00:00:01` | Pause zwischen Anfragen (Nutzungsbedingungen von Nominatim) |
| `BaseUrl` | `https://nominatim.openstreetmap.org/` | Nominatim-Server |

Für neue Gewässer, etwa die von PEGELONLINE angelegten, schlägt ein Skript passende OSM-Elemente vor. Es bewertet die Kandidaten danach, wie viele unserer Slipanlagen, Pegel und Marinas höchstens 1,5 km von ihnen entfernt liegen:

```sh
cd backend
dotnet run tools/suggest-water-shapes.cs -- http://localhost:5080 suggestions.json
```

Die Vorschläge vor dem Übernehmen prüfen. Große internationale Flüsse findet die Suche oft nicht, weil sie in OSM mehrsprachig benannt sind; ihre Relation steht bei Wikidata (Eigenschaft P402).

## Entwickeln ohne Docker

Backend (braucht nur das .NET 10 SDK; die Datenbank `slipways.db` entsteht im Arbeitsverzeichnis):

```sh
cd backend
dotnet run --project src/Slipways.Api      # http://localhost:5080
dotnet test
```

Web (braucht Node 22+; leitet `/api` an http://localhost:5080 weiter):

```sh
cd web
npm install
npm run dev      # http://localhost:5173
npm test
npm run lint
```

Neue Datenbank-Migration:

```sh
cd backend
dotnet tool restore
dotnet ef migrations add <Name> --project src/Slipways.Infrastructure --startup-project src/Slipways.Api --output-dir Persistence/Migrations
```

## API-Überblick

| Methode | Pfad | Beschreibung |
| --- | --- | --- |
| GET | `/api/slipways?search=&waterId=` | Slipanlagen, optional gefiltert |
| GET | `/api/slipways/{id}` | Details einer Slipanlage |
| GET | `/api/waters?search=` | Gewässer mit Anzahl Slipanlagen und Pegel, Kartenposition (`position`) und dem Wasserstand, wenn es genau einen Pegel gibt (`gauge`) |
| GET | `/api/waters/shapes?search=` | Verläufe der gefundenen Gewässer als GeoJSON-FeatureCollection für die Übersichtskarte (ODbL), Feature-ID = Gewässer-ID |
| GET | `/api/waters/{id}` | Details eines Gewässers mit Slipanlagen, Pegeln, Marinas, Verlauf als GeoJSON (`shape`, ODbL) und Kartenausschnitt (`bounds`) |
| GET | `/api/marinas?waterId=`, `/api/extras`, `/api/services` | Stammdaten |
| GET | `/api/stations?waterId=` | Pegelstationen mit aktuellem Wasserstand (`waterLevel`, in cm) |
| POST | `/api/account/register`, `/api/account/login`, `/api/account/refresh` | Konto und Bearer-Token (ASP.NET Identity), nur mit `Accounts__Enabled=true` |
| GET | `/api/account/me` | Angemeldeter Benutzer, nur mit `Accounts__Enabled=true` |
| GET | `/health` | Health-Check |

## Datenquellen und Lizenzen

| Daten | Quelle | Lizenz | Pflichten |
| --- | --- | --- | --- |
| Pegelstationen, Wasserstände, Gewässer an Pegeln | [PEGELONLINE](https://www.pegelonline.wsv.de/webservice/ueberblick) der WSV | [DL-DE->Zero-2.0](https://www.govdata.de/dl-de/zero-2-0) | keine; wir nennen die Quelle trotzdem |
| Kartenkacheln | [OpenStreetMap](https://www.openstreetmap.org/copyright) | ODbL 1.0 / CC BY-SA | „© OpenStreetMap-Mitwirkende“ sichtbar an der Karte |
| Gewässerverläufe (`shape`) | [OpenStreetMap](https://www.openstreetmap.org/copyright) über Nominatim | [ODbL 1.0](https://opendatacommons.org/licenses/odbl/) | Namensnennung (Seitenfuß, Link zum OSM-Element auf der Detailseite); die API gibt die Formen weiter, sie stehen deshalb selbst unter ODbL (Share-Alike) |
| Slipanlagen, Marinas, Motorservices (Seed) | von Hand erfasst (2019/20) | eigene Daten | – |

Die Web-Oberfläche nennt die Quellen im Seitenfuß. Für einen geplanten Import von Slipanlagen und Marinas aus OpenStreetMap gilt zusätzlich: Die ODbL verlangt Namensnennung, und wer eine Datenbank mit OSM-Daten weitergibt, muss sie unter ODbL weitergeben (Share-Alike). Importierte OSM-Einträge werden deshalb mit Quelle und OSM-ID getrennt von den eigenen Daten gehalten.

## Betrieb

Auf Proxmox läuft Slipways in einem LXC mit Tailscale, nächtlicher Nextcloud-Sicherung und Auto-Update von `master`: siehe [`deploy/README.md`](deploy/README.md).

Abhängigkeiten hält Dependabot wöchentlich aktuell.

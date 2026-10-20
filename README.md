# Slipways

Slipways sammelt Slipanlagen (Bootsrampen) an deutschen und niederländischen Gewässern.

| Teil | Ordner | Technik |
| --- | --- | --- |
| Backend | [`backend/`](backend) | .NET 10, ASP.NET Core Minimal APIs, EF Core mit SQLite, ASP.NET Identity |
| Web | [`web/`](web) | React 19, Vite, TypeScript, TanStack Query, React Router, Leaflet |
| Android | [`mobile/android/`](mobile/android) | Kotlin, Jetpack Compose, Material 3, Ktor |
| iOS | [`mobile/ios/`](mobile/ios) | SwiftUI, MapKit, Swift Package `SlipwaysKit` |

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

## Mobile-Apps

Beide Apps sprechen mit der lokalen API aus `docker compose up` (Port 5080).

Android (Android Studio oder JDK 17+ mit Android SDK):

```sh
cd mobile/android
./gradlew :shared:test :app:assembleDebug
```

Im Emulator erreicht die Debug-Version die API unter `http://10.0.2.2:5080`. Der API-Client liegt im reinen Kotlin-Modul `shared`, die Oberfläche in `app`.

iOS (Xcode 26 und [XcodeGen](https://github.com/yonaskolb/XcodeGen), `brew install xcodegen`):

```sh
cd mobile/ios
xcodegen generate      # erzeugt Slipways.xcodeproj aus project.yml
open Slipways.xcodeproj
swift test --package-path SlipwaysKit
```

Im Simulator nutzt die Debug-Version `http://localhost:5080`. Der API-Client liegt im Swift-Package `SlipwaysKit`.

## API-Überblick

| Methode | Pfad | Beschreibung |
| --- | --- | --- |
| GET | `/api/slipways?search=&waterId=` | Slipanlagen, optional gefiltert |
| GET | `/api/slipways/{id}` | Details einer Slipanlage |
| GET | `/api/waters`, `/api/marinas`, `/api/extras`, `/api/services` | Stammdaten |
| GET | `/api/stations?waterId=` | Pegelstationen mit aktuellem Wasserstand (`waterLevel`, in cm) |
| POST | `/api/account/register`, `/api/account/login`, `/api/account/refresh` | Konto und Bearer-Token (ASP.NET Identity) |
| GET | `/api/account/me` | Angemeldeter Benutzer |
| GET | `/health` | Health-Check |

## Datenquellen und Lizenzen

| Daten | Quelle | Lizenz | Pflichten |
| --- | --- | --- | --- |
| Pegelstationen, Wasserstände, Gewässer an Pegeln | [PEGELONLINE](https://www.pegelonline.wsv.de/webservice/ueberblick) der WSV | [DL-DE->Zero-2.0](https://www.govdata.de/dl-de/zero-2-0) | keine; wir nennen die Quelle trotzdem |
| Kartenkacheln | [OpenStreetMap](https://www.openstreetmap.org/copyright) | ODbL 1.0 / CC BY-SA | „© OpenStreetMap-Mitwirkende“ sichtbar an der Karte |
| Slipanlagen, Marinas, Motorservices (Seed) | von Hand erfasst (2019/20) | eigene Daten | – |

Die Web-Oberfläche nennt die Quellen im Seitenfuß. Für einen geplanten Import von Slipanlagen und Marinas aus OpenStreetMap gilt zusätzlich: Die ODbL verlangt Namensnennung, und wer eine Datenbank mit OSM-Daten weitergibt, muss sie unter ODbL weitergeben (Share-Alike). Importierte OSM-Einträge werden deshalb mit Quelle und OSM-ID getrennt von den eigenen Daten gehalten.

## Betrieb

Auf Proxmox läuft Slipways in einem LXC mit Tailscale, nächtlicher Nextcloud-Sicherung und Auto-Update von `master`: siehe [`deploy/README.md`](deploy/README.md).

Abhängigkeiten hält Dependabot wöchentlich aktuell.

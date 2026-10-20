# Slipways

Slipways sammelt Slipanlagen (Bootsrampen) an deutschen und niederländischen Gewässern.

| Teil | Ordner | Technik |
| --- | --- | --- |
| Backend | [`backend/`](backend) | .NET 10, ASP.NET Core Minimal APIs, EF Core mit PostgreSQL, ASP.NET Identity |
| Web | [`web/`](web) | React 19, Vite, TypeScript, TanStack Query, React Router, Leaflet |

## Lokal starten

Voraussetzung: Docker mit Compose.

```sh
docker compose up --build
```

- Web: http://localhost:8080
- API: http://localhost:5080 (OpenAPI-Oberfläche unter http://localhost:5080/scalar)
- PostgreSQL: localhost:5432, Datenbank, Benutzer und Passwort `slipways`

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

Backend (braucht .NET 10 SDK und ein laufendes PostgreSQL, z. B. `docker compose up db`):

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
| GET | `/api/waters`, `/api/marinas`, `/api/extras`, `/api/services` | Stammdaten |
| GET | `/api/stations?waterId=` | Pegelstationen mit aktuellem Wasserstand (`waterLevel`, in cm) |
| POST | `/api/account/register`, `/api/account/login`, `/api/account/refresh` | Konto und Bearer-Token (ASP.NET Identity) |
| GET | `/api/account/me` | Angemeldeter Benutzer |
| GET | `/health` | Health-Check |

## Betrieb

Auf Proxmox läuft Slipways in einem LXC mit Tailscale, nächtlicher Nextcloud-Sicherung und Auto-Update von `master`: siehe [`deploy/README.md`](deploy/README.md).

Abhängigkeiten hält Dependabot wöchentlich aktuell.

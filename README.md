# Slipways

Slipways sammelt Slipanlagen (Bootsrampen) an deutschen und niederländischen Gewässern.

| Teil | Ordner | Technik |
| --- | --- | --- |
| Backend | [`backend/`](backend) | .NET 10, ASP.NET Core Minimal APIs, EF Core mit PostgreSQL, ASP.NET Identity |
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
- PostgreSQL: localhost:5432, Datenbank, Benutzer und Passwort `slipways`

Beim ersten Start legt die API das Schema an und spielt die mitgelieferten Daten ein (Slipanlagen, Gewässer, Marinas, Pegelstationen, Motorservices).

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

## Mobile-Apps

Beide Apps sprechen mit der lokalen API aus `docker compose up` (Port 5080).

Android (Android Studio oder JDK 17+ mit Android SDK):

```sh
cd mobile/android
./gradlew :shared:test :app:assembleDebug
```

Im Emulator erreicht die Debug-Version die API unter `http://10.0.2.2:5080`. API-Client und Login-Logik liegen im reinen Kotlin-Modul `shared`, die Oberfläche in `app`.

iOS (Xcode 26 und [XcodeGen](https://github.com/yonaskolb/XcodeGen), `brew install xcodegen`):

```sh
cd mobile/ios
xcodegen generate      # erzeugt Slipways.xcodeproj aus project.yml
open Slipways.xcodeproj
swift test --package-path SlipwaysKit
```

Im Simulator nutzt die Debug-Version `http://localhost:5080`. API-Client und Login-Logik liegen im Swift-Package `SlipwaysKit`, das Token im Schlüsselbund.

## API-Überblick

| Methode | Pfad | Beschreibung |
| --- | --- | --- |
| GET | `/api/slipways?search=&waterId=` | Slipanlagen, optional gefiltert |
| GET | `/api/slipways/{id}` | Details einer Slipanlage |
| GET | `/api/waters`, `/api/marinas`, `/api/extras`, `/api/services`, `/api/stations` | Stammdaten |
| POST | `/api/account/register`, `/api/account/login`, `/api/account/refresh` | Konto und Bearer-Token (ASP.NET Identity) |
| GET | `/api/account/me` | Angemeldeter Benutzer |
| GET | `/health` | Health-Check |

Abhängigkeiten hält Dependabot wöchentlich aktuell.

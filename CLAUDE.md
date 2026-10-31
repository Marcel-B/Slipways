# Slipways

Slipanlagen, Marinas, Gewässer und Pegel als Web-App. Backend in `backend/` (.NET 10, Minimal APIs, EF Core mit SQLite), Web in `web/` (React 19, Vite, TypeScript). Überblick, Betrieb und Datenquellen stehen im `README.md`.

**Zuerst lesen:** [`docs/offene-punkte.md`](docs/offene-punkte.md). Dort stehen offene Entscheidungen und Restarbeiten früherer Sessions. Neue offene Punkte dort eintragen und erledigte im selben Commit löschen. Dafür keine GitHub-Issues anlegen. Ändert sich, was fertig ist oder als Nächstes kommt, die Roadmap in [`docs/roadmap.md`](docs/roadmap.md) und [`docs/roadmap.en.md`](docs/roadmap.en.md) mitziehen.

## Prüfen, bevor etwas fertig ist

```sh
cd backend && dotnet test
cd web && npm run lint && npm test && npm run build
cd mobile/ios && swift test --package-path SlipwaysKit        # nur bei Änderungen an den Apps
cd mobile/android && ./gradlew :shared:test :app:assembleDebug
```

Die Mobile-Apps (`mobile/`) lesen dieselbe API; ändert sich ein DTO in `Contracts.cs`, die Modelle dort mitziehen (siehe `docs/mobile-apps.md`).

Die CI (`.github/workflows/`) führt genau das aus, dazu `docker compose up` als Rauchtest. `TreatWarningsAsErrors` ist an, Nullable-Warnungen brechen also den Build.

Migrationen werden mit `dotnet ef migrations add <Name> --project src/Slipways.Infrastructure --startup-project src/Slipways.Api --output-dir Persistence/Migrations` aus `backend/` erzeugt (vorher `dotnet tool restore`).

## Konventionen

- Commit-Nachrichten, Doku und UI-Texte auf Deutsch. Code, Bezeichner und Code-Kommentare auf Englisch.
- Nie direkt auf `master` arbeiten: Die Produktion aktualisiert sich automatisch von `master` (siehe `deploy/README.md`). Immer einen Branch anlegen.
- Umgebungen: Produktion ist slipways.de, im Tailnet `slipways-1` (Docker mit `compose.yaml`, `deploy/compose.lxc.yaml` und `deploy/compose.https.yaml`, Tailscale direkt auf dem Server). Der Proxmox-LXC `slipways` ist nur eine Test-Stage. Überwachung (Prometheus/Loki) hängt nur an der Produktion.
- Datenbankneutral bleiben: keine providerspezifischen SQL- oder Spatial-Features. Geometrie wird im Speicher gerechnet (z. B. `WaterLocator`).
- Hintergrund-Jobs für externe Quellen folgen einem Muster: `…Options` (eigener Konfigurationsabschnitt mit `Enabled`, `BaseUrl`, `Interval`), typisierter `…Client`, ein `…Sync`/`…Import` mit dem Abgleich und ein `…Worker`. Beispiele: `Gauges/` (PEGELONLINE), `Shapes/` (Nominatim), `Osm/` (Overpass). Clients rufen externe Dienste nie selbst auf.
- Externe Dienste schonen: eigener User-Agent (`DependencyInjection.UserAgent`), keine parallelen Anfragen, Pausen und Backoff gemäß den Nutzungsregeln.
- Messwerte stehen in `Infrastructure/Monitoring/SlipwaysMetrics.cs`: Präfix `slipways_`, Zähler auf `_total`, Dauern in `_seconds`, Labels nur mit wenigen festen Werten, nie Benutzer, Inhalte oder IDs (`/metrics` hat keinen Login). Ein neuer Job läuft über `SlipwaysMetrics.MeasureJobAsync` und kommt in `SlipwaysMetrics.Jobs`. Neue Messwerte in die Tabelle im README-Abschnitt „Überwachung“ eintragen.
- Tests sind xUnit v3 in `backend/tests/Slipways.Api.Tests`: In-Memory-SQLite mit echten Seed-Daten, externe Dienste über einen Stub-`HttpMessageHandler` mit kleinen JSON-Fixtures (Vorlage `GaugeSyncTests`). Für jeden neuen Job in `SlipwaysApiFactory` `…:Enabled=false` setzen. Messwerte sind statisch und prozessweit; Tests prüfen deshalb Differenzen oder eigene Label-Werte.

## Lizenzen

Daten aus OpenStreetMap stehen unter ODbL. Sie bleiben über `Source = osm` und das OSM-Element von den eigenen Daten (`Source = manual`) getrennt. Der Import kopiert keine OSM-Werte in eigene Einträge. Der Hinweis „© OpenStreetMap-Mitwirkende“ im Web-Seitenfuß muss erhalten bleiben. Neue Quellen in den README-Abschnitt „Datenquellen und Lizenzen“ eintragen.

## Lokale Eigenheiten

overpass-api.de akzeptiert nur TLS 1.3. Auf macOS braucht .NET deshalb `DOTNET_SYSTEM_NET_SECURITY_USENETWORKFRAMEWORK=1`, sonst `Osm__Enabled=false` setzen.

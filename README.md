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

Ein Hintergrund-Job in der API übernimmt die Zuordnung beim Start und holt die Formen bei [Nominatim](https://nominatim.org/), auf etwa 100 m vereinfacht (zusammen rund 0,6 MB). Danach prüft er stündlich, ob ein Gewässer noch keine Form hat (etwa weil PEGELONLINE es gerade erst angelegt hat) oder seine Form älter als 30 Tage ist; nur dann fragt er Nominatim. Clients fragen Nominatim nie selbst ab, sondern lesen `/api/waters/{id}`.

Einstellungen (Abschnitt `Nominatim`):

| Schlüssel | Standard | Bedeutung |
| --- | --- | --- |
| `Enabled` | `true` | Job ein/aus |
| `Interval` | `01:00:00` | Abstand der Prüfungen |
| `RefreshAfter` | `30.00:00:00` | Alter, ab dem eine Form neu geholt wird |
| `RequestSpacing` | `00:00:01` | Pause zwischen Anfragen (Nutzungsbedingungen von Nominatim) |
| `BaseUrl` | `https://nominatim.openstreetmap.org/` | Nominatim-Server |

Für neue Gewässer, etwa die von PEGELONLINE angelegten, schlägt ein Skript passende OSM-Elemente vor. Es bewertet die Kandidaten danach, wie viele unserer Slipanlagen, Pegel und Marinas höchstens 1,5 km von ihnen entfernt liegen:

```sh
cd backend
dotnet run tools/suggest-water-shapes.cs -- http://localhost:5080 suggestions.json
```

Die Vorschläge vor dem Übernehmen prüfen. Große internationale Flüsse findet die Suche oft nicht, weil sie in OSM mehrsprachig benannt sind; ihre Relation steht bei Wikidata (Eigenschaft P402).

## Slipanlagen und Marinas aus OpenStreetMap

Ein Hintergrund-Job in der API importiert beim Start und danach alle 30 Tage Slipanlagen (`leisure=slipway`) und Marinas (`leisure=marina`) aus [OpenStreetMap](https://www.openstreetmap.org/) über die [Overpass API](https://wiki.openstreetmap.org/wiki/Overpass_API), je Land und Art eine Abfrage mit `out center tags;` (Wege und Relationen bekommen so einen Mittelpunkt). Die Abfragen laufen nacheinander mit Pause dazwischen und Timeout in der Abfrage. Ist Overpass ausgelastet (429, 502, 503, 504), wiederholt der Client mit exponentiellem Backoff; scheitert der ganze Lauf, versucht der Job es nach einer Stunde erneut. Stand Oktober 2026 sind es etwa 5.100 Slipanlagen und 4.600 Marinas (DE 3.342 / 2.727, AT 391 / 199, CH 447 / 446, NL 916 / 1.224).

Abgleich, getrennt für Slipanlagen und Marinas:

1. Gibt es schon einen Eintrag mit demselben OSM-Element (`OsmType` + `OsmId`), werden bei importierten Einträgen (`Source = osm`) die Felder aus OSM aktualisiert. Eigene Einträge, die mit OSM verknüpft sind, bleiben unverändert.
2. Liegt ein eigener Eintrag (`Source = manual`) näher als 50 m (Haversine), wird nur das OSM-Element an ihm hinterlegt und nichts Neues angelegt. Ist jeder eigene Eintrag in der Nähe schon mit einem anderen Element verknüpft, gilt das Element als doppelt erfasst (z. B. als Knoten und als Weg) und wird übersprungen.
3. Sonst entsteht ein neuer Eintrag mit `Source = osm`.

Übernommen werden `name` (sonst „Slipanlage“ bzw. „Marina“ und `addr:city`), `addr:street`/`addr:housenumber`, `addr:postcode`, `addr:city`, `addr:country` (sonst das abgefragte Land, nur Slipanlagen), `website` (oder `contact:website`), `operator`, `fee`, `access`, `opening_hours` und bei Slipanlagen `surface`. Eigene Angaben (Kommentar, Bewertung, Kosten, Pro/Contra, Extras, Marina, Telefon, E-Mail) überschreibt der Import nie. Was aus OSM verschwindet, löscht er nicht, sondern schreibt es ins Log.

OSM kennt kein Gewässer zu einer Slipanlage. Der Import ordnet deshalb das Gewässer zu, dessen Verlauf (siehe oben) höchstens 300 m entfernt ist oder dessen Fläche den Punkt enthält, sonst bleibt `WaterId` leer. Das betrifft vor allem Seen und kleine Flüsse, die PEGELONLINE nicht kennt. Einträge ohne Gewässer bekommen bei jedem Lauf eine neue Chance.

Einstellungen (Abschnitt `Osm`):

| Schlüssel | Standard | Bedeutung |
| --- | --- | --- |
| `Enabled` | `true` | Job ein/aus |
| `Countries` | `DE`, `AT`, `CH`, `NL` | ISO-3166-1-Codes, z. B. `Osm__Countries__0=DE` |
| `Interval` | `30.00:00:00` | Abstand der Importe |
| `RetryInterval` | `01:00:00` | Wartezeit nach einem gescheiterten Lauf |
| `QueryTimeout` | `00:03:00` | Timeout in der Overpass-Abfrage |
| `RequestSpacing` | `00:00:10` | Pause zwischen zwei Abfragen |
| `BusyBackoff`, `MaxAttempts` | `00:00:30`, `4` | Erste Wartezeit (verdoppelt sich) und Versuche, wenn Overpass ausgelastet ist |
| `BaseUrl` | `https://overpass-api.de/api/` | Overpass-Server; für viel Last besser eine eigene Instanz |

overpass-api.de spricht nur noch TLS 1.3. .NET auf macOS kann das nur über das Network-Framework; lokal auf dem Mac deshalb mit `DOTNET_SYSTEM_NET_SECURITY_USENETWORKFRAMEWORK=1` starten oder `Osm__Enabled=false` setzen. Im Linux-Container sollte es ohne Zutun gehen (OpenSSL), getestet ist das noch nicht.

Was beim Import noch offen ist, steht in [`docs/offene-punkte.md`](docs/offene-punkte.md).

## Überwachung

Die API liefert Messwerte für Prometheus unter `/metrics`, aber nur auf einem eigenen Port (`Monitoring:MetricsPort`, Standard 9464). Dieser Port beantwortet nichts anderes, und die übrigen Ports beantworten `/metrics` nicht. Entscheidend ist der Port, auf dem die Verbindung ankommt, nicht der `Host`-Header. Lokal: http://localhost:9464/metrics.

| Messwert | Bedeutung |
| --- | --- |
| `slipways_job_runs_total{task, result}` | Läufe der Hintergrund-Jobs `gauges`, `shapes`, `osm`, nach `success`/`failure` |
| `slipways_job_duration_seconds{task}` | Dauer der Läufe (Histogramm) |
| `slipways_job_last_success_timestamp_seconds{task}` | Zeitpunkt des letzten erfolgreichen Laufs |
| `slipways_gauge_water_levels_updated_total` | übernommene Wasserstände |
| `slipways_osm_import_changes_total{kind, change}` | OSM-Import: `added`/`updated`/`linked` je `slipway`/`marina` |
| `slipways_osm_missing_elements` | Einträge, deren OSM-Element der letzte Import nicht mehr fand |
| `slipways_overpass_busy_responses_total` | „Overpass ausgelastet“, jeweils mit neuem Versuch |
| `slipways_places{kind, source}` | Slipanlagen, Marinas und Pegel in der Datenbank (jede Minute gezählt) |
| `slipways_database_size_bytes` | Größe der SQLite-Datei samt WAL |
| `slipways_loki_lines_pushed_total`, `slipways_loki_lines_dropped_total{reason}` | Logzeilen an Loki bzw. verworfen (`queue_full`, `push_failed`) |
| `http_requests_received_total`, `http_request_duration_seconds` | Anfragen nach Route-Vorlage, Methode und Statuscode (prometheus-net) |
| `process_*`, `dotnet_*` | Prozess und Laufzeit, darunter `process_resident_memory_bytes` |

Labels haben nur wenige feste Werte. Das Label der Jobs heißt `task`, weil Prometheus `job` selbst vergibt. Benutzer, Inhalte und IDs kommen nicht vor.

Mit `Monitoring:Loki:Url` (z. B. `http://monitoring:3100`) schickt die API ihre Logzeilen zusätzlich an Loki, mit den Labels `app=slipways`, `host` (`Monitoring:Loki:Host`), `level` und `source` (`api`, `http`, `db`, `gauges`, `shapes`, `osm`, `http-client`). Die Zeilen werden alle 2 Sekunden gebündelt verschickt. Ist Loki nicht erreichbar, verwirft die API sie, statt zu warten. Das Konsolen-Log bleibt vollständig. Ohne Url ist der Versand aus. Angebunden ist nur slipways.de; Einrichten: [`deploy/README.md`](deploy/README.md#überwachung).

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
| Slipanlagen und Marinas mit `source = osm` | [OpenStreetMap](https://www.openstreetmap.org/copyright) über Overpass | [ODbL 1.0](https://opendatacommons.org/licenses/odbl/) | Namensnennung (Seitenfuß, Quelle und Link zum OSM-Element auf der Detailseite); getrennt gehalten über `Source` und OSM-Element; die API gibt sie weiter, sie stehen deshalb selbst unter ODbL (Share-Alike) |

Die Web-Oberfläche nennt die Quellen im Seitenfuß. Die ausführliche Recherche zu weiteren offenen Quellen (OSM, eHYD, BAFU, Rijkswaterstaat u. a.) steht in [`docs/datenquellen.md`](docs/datenquellen.md). Für den Import von Slipanlagen und Marinas aus OpenStreetMap gilt zusätzlich: Die ODbL verlangt Namensnennung, und wer eine Datenbank mit OSM-Daten weitergibt, muss sie unter ODbL weitergeben (Share-Alike). Importierte Einträge tragen deshalb `Source = osm` und ihr OSM-Element und bleiben von den eigenen Daten getrennt: Der Import kopiert keine OSM-Werte in eigene Einträge, sondern hinterlegt dort höchstens das OSM-Element. Die API liefert bei Slipanlagen und Marinas `source` (`manual` oder `osm`) und, falls vorhanden, `osmElement` (z. B. `node/123`) mit.

## Betrieb

Die Produktion läuft auf slipways.de (im Tailnet `slipways-1`) mit Caddy für HTTPS ([`deploy/README.md`](deploy/README.md#https-mit-eigener-domain)). Eine Test-Stage läuft auf Proxmox in einem LXC mit Tailscale und nächtlicher Nextcloud-Sicherung. Beide spielen jeden neuen Stand auf `master` selbst ein (siehe [`deploy/README.md`](deploy/README.md)).

Abhängigkeiten hält Dependabot wöchentlich aktuell.

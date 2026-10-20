# Slipways

Slipways sammelt Slipanlagen (Bootsrampen) und Marinas in Deutschland und weiteren Ländern Europas.

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

Ein Hintergrund-Job in der API importiert Slipanlagen (`leisure=slipway`) und Marinas (`leisure=marina`) aus [OpenStreetMap](https://www.openstreetmap.org/) über die [Overpass API](https://wiki.openstreetmap.org/wiki/Overpass_API), je Land und Art eine Abfrage mit `out center tags;` (Wege und Relationen bekommen so einen Mittelpunkt). Jedes Land wird alle 30 Tage importiert, ein Land pro Lauf. Die Länder wechseln sich ab, ein Lauf alle 30 Tage geteilt durch die Zahl der Länder (bei neun Ländern etwa alle 3,3 Tage), damit Overpass und der Server kleine Läufe über den Monat verteilt bekommen. Ein Land, das noch nie importiert wurde, läuft sofort. Die beiden Abfragen eines Laufs laufen nacheinander mit Pause dazwischen und Timeout in der Abfrage. Ist Overpass ausgelastet (429, 502, 503, 504), wiederholt der Client mit exponentiellem Backoff; scheitert der Lauf, versucht der Job dieses Land nach einer Stunde erneut. Stand Oktober 2026 waren es für DE, AT, CH und NL etwa 5.100 Slipanlagen und 4.600 Marinas (DE 3.342 / 2.727, AT 391 / 199, CH 447 / 446, NL 916 / 1.224). Dazu kommen DK, SE, NO, IT und HR.

Ein Element, das in den Flächen zweier Länder liegt, gehört dem Land, dessen Import es zuerst gefunden hat (Spalte `OsmCountry`). Nur dieser Import aktualisiert es und meldet es als verschwunden.

Wann der Import eines Landes zuletzt erfolgreich war bzw. gescheitert ist, steht in der Tabelle `JobStates` (eine Zeile je Land, z. B. `osm:DE`). Neustarts und Deploys stoßen deshalb keinen Import an, der Job wartet bis zum nächsten fälligen Zeitpunkt (im Log: „Next OpenStreetMap import at … for DE“). Sofort läuft er nur für Länder, die noch nie importiert wurden. Fehlt die Zeile eines Landes, gilt für die Länder, aus denen es schon importierte Slipanlagen gibt, die alte gemeinsame Zeile `osm` (aus der Zeit vor dem Import je Land) und ohne sie der jüngste importierte Eintrag als letzter Erfolg. Ein Lauf, den ein Neustart abbricht, zählt nicht als gescheitert und beginnt beim nächsten Start neu. Einen Import vorziehen: auf dem Server

```sh
sqlite3 "$(docker volume inspect --format '{{ .Mountpoint }}' slipways_api-data)/slipways.db" \
  "UPDATE JobStates SET LastSuccess = NULL, LastFailure = NULL WHERE Name = 'osm:DE';"
cd /opt/slipways && docker compose restart api
```

Abgleich, getrennt für Slipanlagen und Marinas:

1. Gibt es schon einen Eintrag mit demselben OSM-Element (`OsmType` + `OsmId`), werden bei importierten Einträgen (`Source = osm`) die Felder aus OSM aktualisiert. Eigene Einträge, die mit OSM verknüpft sind, bleiben unverändert.
2. Liegt ein eigener Eintrag (`Source = manual`) näher als 50 m (Haversine), wird nur das OSM-Element an ihm hinterlegt und nichts Neues angelegt. Ist jeder eigene Eintrag in der Nähe schon mit einem anderen Element verknüpft, gilt das Element als doppelt erfasst (z. B. als Knoten und als Weg) und wird übersprungen.
3. Sonst entsteht ein neuer Eintrag mit `Source = osm`.

Übernommen werden `name` (sonst „Slipanlage“ bzw. „Marina“ und `addr:city`), `addr:street`/`addr:housenumber`, `addr:postcode`, `addr:city`, `addr:country` (sonst das abgefragte Land, nur Slipanlagen), `website` (oder `contact:website`), bei Marinas `phone`/`contact:phone` und `email`/`contact:email`, `operator`, `fee`, `access`, `opening_hours` und bei Slipanlagen `surface`. Eigene Angaben (Kommentar, Bewertung, Kosten, Pro/Contra, Extras, Marina, Telefon, E-Mail) überschreibt der Import nie. Was aus OSM verschwindet, löscht er nicht, sondern schreibt es ins Log.

OSM kennt kein Gewässer zu einer Slipanlage. Der Import ordnet deshalb das Gewässer zu, dessen Verlauf (siehe oben) höchstens 300 m entfernt ist oder dessen Fläche den Punkt enthält, sonst bleibt `WaterId` leer. Das betrifft vor allem Seen und kleine Flüsse, die PEGELONLINE nicht kennt. Einträge ohne Gewässer bekommen bei jedem Lauf eine neue Chance.

Einstellungen (Abschnitt `Osm`):

| Schlüssel | Standard | Bedeutung |
| --- | --- | --- |
| `Enabled` | `true` | Job ein/aus |
| `Countries` | `DE`, `AT`, `CH`, `NL`, `DK`, `SE`, `NO`, `IT`, `HR` | ISO-3166-1-Codes, z. B. `Osm__Countries__0=DE` |
| `Interval` | `30.00:00:00` | Abstand der Importe eines Landes, gerechnet ab seinem letzten Erfolg; die Länder verteilen sich darüber |
| `RetryInterval` | `01:00:00` | Wartezeit nach einem gescheiterten Lauf, auch über Neustarts hinweg |
| `QueryTimeout` | `00:03:00` | Timeout in der Overpass-Abfrage |
| `RequestSpacing` | `00:00:10` | Pause zwischen zwei Abfragen, auch zwischen zwei Ländern, die zugleich fällig sind |
| `BusyBackoff`, `MaxAttempts` | `00:00:30`, `4` | Erste Wartezeit (verdoppelt sich) und Versuche, wenn Overpass ausgelastet ist |
| `BaseUrl` | `https://overpass-api.de/api/` | Overpass-Server; für viel Last besser eine eigene Instanz |

overpass-api.de spricht nur noch TLS 1.3. .NET auf macOS kann das nur über das Network-Framework; lokal auf dem Mac deshalb mit `DOTNET_SYSTEM_NET_SECURITY_USENETWORKFRAMEWORK=1` starten oder `Osm__Enabled=false` setzen. Im Linux-Container sollte es ohne Zutun gehen (OpenSSL), getestet ist das noch nicht.

Was beim Import noch offen ist, steht in [`docs/offene-punkte.md`](docs/offene-punkte.md).

## Angaben von den Webseiten der Marinas

Etwa 2.000 Marinas verlinken in OSM ihre Webseite, aber nur wenige haben dort Telefon oder Öffnungszeiten. Ein Hintergrund-Job (`backend/src/Slipways.Infrastructure/Websites/`) liest deshalb die verlinkte Startseite und übernimmt, was sie über die Marina selbst sagt:

- aus strukturierten Daten (schema.org als JSON-LD) `telephone`, `email`, `openingHours` und `openingHoursSpecification` (umgeschrieben in die OSM-Schreibweise, z. B. `Mo-Fr 08:00-18:00; Sa,Su 09:00-12:00`; saisonale Angaben mit `validFrom`/`validThrough` nicht),
- sonst Telefon und E-Mail aus `tel:`- und `mailto:`-Links.

Ein Wert wird nur übernommen, wenn die Seite genau einen nennt (dieselbe Nummer national und international geschrieben zählt als eine). Nennt eine Vereinsseite mehrere Ansprechpartner, bleibt das Feld leer. Objekte vom Typ `Person` und Eigenschaften wie `employee`, `founder` oder `member` liest der Job nie. Gelesen wird nur die Startseite, kein Impressum und keine Unterseiten.

Die Ergebnisse stehen nicht in der Marina, sondern in der Tabelle `PlaceInfos`: eine Zeile je Feld mit `Source = website`, Quell-URL (nach Weiterleitungen) und Abrufdatum. Die Tabelle ist offen für weitere Quellen (z. B. freigegebene Nutzerbeiträge, dafür gibt es auch `OsmType`/`OsmId` und `BaseValue`). Die API liefert sie bei `/api/marinas/{id}` als `infos`; das Web zeigt sie nur, wo die Marina selbst (bzw. OSM) keinen Wert hat, mit „laut Webseite, Stand …“.

Höflich bleiben: eigener User-Agent (`Slipways (+https://github.com/Marcel-B/Slipways)`, in robots.txt als `Slipways` ansprechbar), robots.txt nach RFC 9309 für jeden Host auf dem Weg (fehlt sie, ist alles erlaubt; antwortet der Server mit 5xx, nichts), Weiterleitungen von Hand (höchstens 5), eine Anfrage nach der anderen mit Pause, höchstens 1 MB je Seite. Der Job verbindet sich nur mit öffentlichen Adressen: Links aus OSM kann jeder setzen, deshalb sind Loopback, private Netze, das Tailnet (100.64.0.0/10, `fd7a:…`) und Link-Local gesperrt.

Jede Stunde liest er bis zu 200 fällige Seiten: noch nie gelesen, älter als 30 Tage oder mit geänderter Adresse. Wann und mit welchem Ergebnis eine Seite gelesen wurde, steht in `WebsiteChecks` (`found`, `empty`, `disallowed`, `refused`, `failed`). Ist eine Seite nicht erreichbar, bleiben die alten Angaben stehen; verbietet die robots.txt das Lesen oder steht nichts mehr auf der Seite, verschwinden sie. Widerspricht ein Betreiber, auf dem Server:

```sh
sqlite3 "$(docker volume inspect --format '{{ .Mountpoint }}' slipways_api-data)/slipways.db" \
  "UPDATE WebsiteChecks SET Result = 'excluded' WHERE PlaceType = 'marina' AND PlaceId = '<ID>';
   DELETE FROM PlaceInfos WHERE PlaceType = 'marina' AND PlaceId = '<ID>' AND Source = 'website';"
```

Die ID steht in großen Buchstaben in der Datenbank (wie EF Core GUIDs in SQLite schreibt). Eine ausgeschlossene Seite liest der Job nie wieder.

Einstellungen (Abschnitt `Websites`; eine `BaseUrl` gibt es nicht, jede Marina hat ihre eigene Seite):

| Schlüssel | Standard | Bedeutung |
| --- | --- | --- |
| `Enabled` | `true` | Job ein/aus |
| `Interval` | `01:00:00` | Abstand der Läufe |
| `BatchSize` | `200` | Seiten je Lauf |
| `RefreshAfter` | `30.00:00:00` | Alter, ab dem eine Seite neu gelesen wird |
| `RequestSpacing` | `00:00:02` | Pause zwischen zwei Anfragen (auch robots.txt) |
| `Timeout` | `00:00:15` | Zeit je Anfrage samt Inhalt |
| `MaxPageBytes` | `1048576` | gelesene Bytes je Seite |

Google Maps ist bewusst keine Quelle: Auslesen verbieten die Nutzungsbedingungen, und die Places API kostet je Abfrage und erlaubt kein dauerhaftes Speichern der Inhalte.

## Nutzerbeiträge

Wer Slipways nutzt, kann ohne Konto Angaben ergänzen oder korrigieren, ankreuzen, was es vor Ort gibt (Fakten wie `jetty`, `toilet`, `trailer_parking`), kurze Tipps geben, Probleme melden und fehlende Slipanlagen vorschlagen. Nichts davon ist sofort öffentlich: Jeder Beitrag landet als Vorschlag in der Tabelle `Suggestions` und wird erst nach Freigabe in der Moderation übernommen. Konzept und Abwägung stehen in [`docs/nutzerbeitraege.md`](docs/nutzerbeitraege.md), der Code in `backend/src/Slipways.Infrastructure/Suggestions/`.

**Missbrauchsschutz** vor der Warteschlange, ohne fremde Dienste:

- **Proof of Work** im Format von [ALTCHA](https://altcha.org/): `GET /api/suggestions/challenge` liefert eine signierte Aufgabe (SHA-256, HMAC, Ablauf in `salt`), der Client sucht die Zahl dazu (im Browser per Web Worker, etwa eine Sekunde), jede Lösung gilt nur einmal. Keine Cookies, keine Daten über den Client.
- **Rate-Limit** eigens für `POST`/`DELETE /api/suggestions`: 5 in Folge, dann einer je 10 Minuten pro Adresse (`RateLimit:SuggestionTokenLimit`, `SuggestionReplenishmentPeriod`).
- **Honeypot-Feld** `homepage`: Wer es ausfüllt, bekommt eine Antwort wie bei Erfolg, gespeichert wird nichts.
- **Prüfungen:** nur bekannte Felder mit festen Werten (`SuggestionFields`), Texte ohne Links und mit Längengrenzen, neue Slipanlagen nur in DE, AT, CH und NL. Liegt eine bekannte Slipanlage näher als 100 m, antwortet die API mit `409` und der Liste; erst mit `confirmNew` wird der Vorschlag angenommen.

Gespeichert werden nur Inhalt, Zeitpunkt und der Hash eines Tokens, mit dem der Absender den Stand sieht und den Vorschlag zurückzieht (`/beitrag/{id}#{token}` im Web). Keine IP-Adresse, kein Name. Abgelehnte Vorschläge werden nach 30 Tagen gelöscht, angenommene nach 90 (`Suggestions:KeepRejected`, `KeepAccepted`).

**Was die Freigabe macht:**

| Vorschlag | eigene Einträge (`manual`) | Einträge aus OSM (`osm`) |
| --- | --- | --- |
| neue Slipanlage | neue Slipanlage mit `Source = manual` | – |
| Werte für Felder mit eigener Spalte (`fee`, `costs`, `access`, `opening_hours`, `surface`, `operator`, `phone`, `email`) | in die Spalte | Zeile in `PlaceInfos` mit `Source = user` und dem OSM-Wert von damals in `BaseValue` |
| Fakten zum Ankreuzen, Tipps (`tip`) | Zeile in `PlaceInfos` | Zeile in `PlaceInfos` |
| Problem melden (`report`) | nur für die Moderation | nur für die Moderation |

Werte von Nutzern gewinnen in Web und API über die eigenen Felder des Eintrags. Nach jedem OSM-Import gleicht der Import die Zeilen zu OSM-Einträgen ab: Hat OSM jetzt denselben Wert, fällt die Zeile weg; hat OSM sich anders geändert (weder `BaseValue` noch der Wert der Nutzer), geht der Wert als neuer Vorschlag zurück in die Moderation und bis dahin gilt OSM.

**Moderation** gibt es nur im Tailnet: Die API beantwortet `/moderation` (eine kleine Seite) und `/api/moderation/…` ausschließlich auf Port 9465 (`Moderation:Port`), alle anderen Ports nie. Änderungen brauchen zusätzlich den Header `X-Moderation`, damit eine fremde Seite im Browser nichts auslösen kann. Einrichtung in [`deploy/README.md`](deploy/README.md#moderation).

Einstellungen (Abschnitt `Suggestions`):

| Schlüssel | Standard | Bedeutung |
| --- | --- | --- |
| `Enabled` | `true` | Vorschläge annehmen (aus: `503`, Angenommenes bleibt sichtbar) |
| `ChallengeKey` | zufällig je Start | Schlüssel für die Signatur der Aufgaben |
| `ChallengeMaxNumber` | `100000` | Größe der Aufgabe |
| `ChallengeLifetime` | `00:10:00` | Gültigkeit einer Aufgabe |
| `KeepRejected` / `KeepAccepted` | `30.00:00:00` / `90.00:00:00` | Aufbewahrung entschiedener Vorschläge |

## Überwachung

Die API liefert Messwerte für Prometheus unter `/metrics`, aber nur auf einem eigenen Port (`Monitoring:MetricsPort`, Standard 9464). Dieser Port beantwortet nichts anderes, und die übrigen Ports beantworten `/metrics` nicht. Entscheidend ist der Port, auf dem die Verbindung ankommt, nicht der `Host`-Header. Lokal: http://localhost:9464/metrics.

| Messwert | Bedeutung |
| --- | --- |
| `slipways_job_runs_total{task, result}` | Läufe der Hintergrund-Jobs `gauges`, `shapes`, `osm`, `websites`, nach `success`/`failure` |
| `slipways_job_duration_seconds{task}` | Dauer der Läufe (Histogramm) |
| `slipways_job_last_success_timestamp_seconds{task}` | Zeitpunkt des letzten erfolgreichen Laufs (für `osm` nach einem Neustart aus der Datenbank) |
| `slipways_gauge_water_levels_updated_total` | übernommene Wasserstände |
| `slipways_osm_import_changes_total{kind, change}` | OSM-Import: `added`/`updated`/`linked` je `slipway`/`marina` |
| `slipways_osm_missing_elements{country}` | Einträge, deren OSM-Element der letzte Import ihres Landes nicht mehr fand |
| `slipways_overpass_busy_responses_total` | „Overpass ausgelastet“, jeweils mit neuem Versuch |
| `slipways_website_checks_total{result}` | gelesene Webseiten von Marinas nach Ergebnis (`found`, `empty`, `disallowed`, `refused`, `failed`) |
| `slipways_suggestions_received_total{result}` | eingegangene Vorschläge nach Ergebnis (`queued`, `invalid`, `proof_failed`, `honeypot`, `nearby`) |
| `slipways_suggestions_decided_total{decision}` | Entscheidungen in der Moderation (`accepted`, `rejected`) |
| `slipways_suggestions_pending` | Vorschläge, die auf die Moderation warten (jede Minute gezählt) |
| `slipways_places{kind, source}` | Slipanlagen, Marinas und Pegel in der Datenbank (jede Minute gezählt) |
| `slipways_database_size_bytes` | Größe der SQLite-Datei samt WAL |
| `slipways_loki_lines_pushed_total`, `slipways_loki_lines_dropped_total{reason}` | Logzeilen an Loki bzw. verworfen (`queue_full`, `push_failed`) |
| `http_requests_received_total`, `http_request_duration_seconds` | Anfragen nach Route-Vorlage, Methode und Statuscode (prometheus-net) |
| `process_*`, `dotnet_*` | Prozess und Laufzeit, darunter `process_resident_memory_bytes` |

Labels haben nur wenige feste Werte. Das Label der Jobs heißt `task`, weil Prometheus `job` selbst vergibt. Benutzer, Inhalte und IDs kommen nicht vor.

Mit `Monitoring:Loki:Url` (z. B. `http://monitoring:3100`) schickt die API ihre Logzeilen zusätzlich an Loki, mit den Labels `app=slipways`, `host` (`Monitoring:Loki:Host`), `level` und `source` (`api`, `http`, `db`, `gauges`, `shapes`, `osm`, `websites`, `http-client`). Die Zeilen werden alle 2 Sekunden gebündelt verschickt. Ist Loki nicht erreichbar, verwirft die API sie, statt zu warten. Das Konsolen-Log bleibt vollständig. Ohne Url ist der Versand aus. Angebunden ist nur slipways.de; Einrichten: [`deploy/README.md`](deploy/README.md#überwachung).

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

Beide Apps sind reine Lesesichten mit den Tabs Slipanlagen, Gewässer und Info. Release- und Debug-Versionen sprechen mit https://slipways.de; die Debug-Versionen lassen sich auf die lokale API aus `docker compose up` (Port 5080) umstellen. Aufbau und Lizenzhinweis: [`docs/mobile-apps.md`](docs/mobile-apps.md).

Android (Android Studio oder JDK 17+ mit Android SDK):

```sh
cd mobile/android
./gradlew :shared:test :app:assembleDebug
```

Gegen die lokale API: `./gradlew :app:installDebug -Pslipways.apiBaseUrl=http://10.0.2.2:5080` (10.0.2.2 ist im Emulator der Rechner). Der API-Client liegt im reinen Kotlin-Modul `shared`, die Oberfläche in `app`.

iOS (Xcode 26 und [XcodeGen](https://github.com/yonaskolb/XcodeGen), `brew install xcodegen`):

```sh
cd mobile/ios
xcodegen generate      # erzeugt Slipways.xcodeproj aus project.yml
open Slipways.xcodeproj
swift test --package-path SlipwaysKit
```

Gegen die lokale API: im Schema unter Run > Arguments die Umgebungsvariable `SLIPWAYS_API_BASE_URL=http://localhost:5080` setzen. Der API-Client liegt im Swift-Package `SlipwaysKit`.


## API-Überblick

| Methode | Pfad | Beschreibung |
| --- | --- | --- |
| GET | `/api/slipways?search=&waterId=` | Slipanlagen, optional gefiltert |
| GET | `/api/slipways/{id}` | Details einer Slipanlage, mit `infos` (Werte von Nutzern, Fakten, Tipps) |
| GET | `/api/waters?search=` | Gewässer mit Anzahl Slipanlagen und Pegel, Kartenposition (`position`) und dem Wasserstand, wenn es genau einen Pegel gibt (`gauge`) |
| GET | `/api/waters/shapes?search=` | Verläufe der gefundenen Gewässer als GeoJSON-FeatureCollection für die Übersichtskarte (ODbL), Feature-ID = Gewässer-ID |
| GET | `/api/waters/{id}` | Details eines Gewässers mit Slipanlagen, Pegeln, Marinas, Verlauf als GeoJSON (`shape`, ODbL) und Kartenausschnitt (`bounds`) |
| GET | `/api/marinas?search=&waterId=` | Marinas, optional gefiltert nach Name, Ort oder Gewässer bzw. nach Gewässer-ID |
| GET | `/api/marinas/{id}` | Details einer Marina mit den zugehörigen Slipanlagen |
| GET | `/api/suggestions/challenge` | Proof-of-Work-Aufgabe für einen Vorschlag |
| POST | `/api/suggestions` | Vorschlag senden (`202` mit `id` und `token`, `409` mit Slipanlagen in der Nähe) |
| GET, DELETE | `/api/suggestions/{id}?token=` | Stand eines Vorschlags bzw. zurückziehen, solange er wartet |
| GET | `/api/contributions/odbl` | Werte von Nutzern zu OSM-Einträgen, unter ODbL |
| GET | `/api/extras`, `/api/services` | Stammdaten |
| GET | `/api/stations?waterId=` | Pegelstationen mit aktuellem Wasserstand (`waterLevel`, in cm) |
| POST | `/api/account/register`, `/api/account/login`, `/api/account/refresh` | Konto und Bearer-Token (ASP.NET Identity), nur mit `Accounts__Enabled=true` |
| GET | `/api/account/me` | Angemeldeter Benutzer, nur mit `Accounts__Enabled=true` |
| GET | `/health` | Health-Check |

## Suchmaschinen

Das Web ist eine Single-Page-App, deshalb setzt nginx Titel, Beschreibung, Canonical-Link, Open Graph und strukturierte Daten (schema.org als JSON-LD: `Place` für Slipanlagen, `Marina`, `BodyOfWater`, `WebSite` für die Startseite) schon serverseitig in `index.html`: Ein Server Side Include im Block `seo` holt sie von `/api/seo/head/<Pfad>`. Antwortet die API nicht (Neustart, Rate-Limit), bleiben die allgemeinen Angaben aus `index.html` stehen. Unbekannte Seiten und IDs bekommen `noindex`, weil die SPA auch für sie `200` liefert. `/sitemap.xml` baut die API aus der Datenbank (`/api/seo/sitemap`), `web/public/robots.txt` verweist darauf. Die Adresse in Canonical-Links und Sitemap steht in `Seo:SiteUrl` (Vorgabe `https://slipways.de`, die Test-Stage verweist damit auf die Produktion). Im Browser setzen die Seiten ihren Titel selbst (React-`<title>`); Slipanlagen und Marinas heißen dort wie in der API „Name in Ort“, wenn der Ort nicht schon im Namen steht.

## Absicherung

Die API ist bewusst ohne Anmeldung lesbar, Web und Apps brauchen keinen Schlüssel. Schreiben kann man von außen nur Vorschläge, die erst nach der Moderation sichtbar werden (siehe „Nutzerbeiträge“); Konto-Endpunkte gibt es nur mit `Accounts:Enabled=true`.

- **Erreichbar von außen** ist nur `/api/…` über nginx (und Caddy). `/health`, `/metrics`, die Moderation und die API-Doku hängen nicht daran; `/metrics` und die Moderation antworten nur auf ihren eigenen Ports, Scalar und `/openapi` gibt es nur im Modus `Development`.
- **Rate-Limit:** Jede Client-Adresse (IPv6 je /64) hat für `/api/…` einen Token-Bucket mit 120 Anfragen, der sich um 60 je 10 Sekunden auffüllt (`RateLimit:TokenLimit`, `TokensPerPeriod`, `ReplenishmentPeriod`, abschalten mit `RateLimit:Enabled=false`). Darüber antwortet die API mit `429` und `Retry-After`. Die Grenzen sind großzügig, weil sich viele App-Nutzer eine Adresse teilen können (Mobilfunk). Ein Abruf aller Daten bleibt möglich, denn `/api/slipways` liefert alles in einer Antwort; das Limit schützt vor Überlastung, nicht vor dem Kopieren. Die Client-Adresse kommt aus `X-Forwarded-For`, dem die API nur von Adressen aus privaten Netzen (Docker) traut, höchstens zwei Stationen weit (Caddy, nginx). Caddy ersetzt einen mitgeschickten `X-Forwarded-For`, deshalb kann ein Client sich keine fremde Adresse geben.
- **CORS** ist nicht eingerichtet: Fremde Websites können die API im Browser nicht lesen. Die Apps betrifft das nicht.
- **Header:** nginx setzt für alle Antworten Content-Security-Policy, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` und `Permissions-Policy` (`web/security-headers.conf`), Caddy dazu HSTS. Die CSP erlaubt nur eigene Skripte; deshalb steht das Theme-Skript in `web/public/theme.js` statt inline in `index.html`. Ein neuer Kachel-Server oder eine andere externe Quelle muss dort eingetragen werden.

## Datenquellen und Lizenzen

| Daten | Quelle | Lizenz | Pflichten |
| --- | --- | --- | --- |
| Pegelstationen, Wasserstände, Gewässer an Pegeln | [PEGELONLINE](https://www.pegelonline.wsv.de/webservice/ueberblick) der WSV | [DL-DE->Zero-2.0](https://www.govdata.de/dl-de/zero-2-0) | keine; wir nennen die Quelle trotzdem |
| Kartenkacheln | [OpenStreetMap](https://www.openstreetmap.org/copyright) | ODbL 1.0 / CC BY-SA | „© OpenStreetMap-Mitwirkende“ sichtbar an der Karte |
| Kartenkacheln der Android-App | [OpenFreeMap](https://openfreemap.org/) (OpenMapTiles, Daten aus OpenStreetMap) | ODbL 1.0 (Daten), Stil BSD/CC BY | Namensnennung an der Karte; der Kartenstil bringt sie mit |
| Gewässerverläufe (`shape`) | [OpenStreetMap](https://www.openstreetmap.org/copyright) über Nominatim | [ODbL 1.0](https://opendatacommons.org/licenses/odbl/) | Namensnennung (Seitenfuß, Link zum OSM-Element auf der Detailseite); die API gibt die Formen weiter, sie stehen deshalb selbst unter ODbL (Share-Alike) |
| Slipanlagen, Marinas, Motorservices (Seed) | von Hand erfasst (2019/20) | eigene Daten | – |
| Telefon, E-Mail, Öffnungszeiten von Marinas (`infos` mit `source = website`) | Startseite der Marina, die in OSM verlinkt ist | Fakten, kein Werkschutz; ggf. personenbezogen (DSGVO, siehe Datenschutzseite) | Quelle und Abrufdatum an der Angabe; robots.txt beachten; auf Widerspruch löschen und ausschließen |
| Werte von Nutzern, Fakten, Tipps und neue Slipanlagen (`infos` mit `source = user`, neue Einträge mit `Source = manual`) | Vorschläge der Nutzer nach Moderation | eigene Daten; Werte zu OSM-Einträgen als abgeleitete Datenbank unter [ODbL 1.0](https://opendatacommons.org/licenses/odbl/) | Absender bestätigen eigene Kenntnis und erlauben die Veröffentlichung; Werte zu OSM-Einträgen getrennt in `PlaceInfos`, Download unter `/api/contributions/odbl` |
| Slipanlagen und Marinas mit `source = osm` | [OpenStreetMap](https://www.openstreetmap.org/copyright) über Overpass | [ODbL 1.0](https://opendatacommons.org/licenses/odbl/) | Namensnennung (Seitenfuß, Quelle und Link zum OSM-Element auf der Detailseite); getrennt gehalten über `Source` und OSM-Element; die API gibt sie weiter, sie stehen deshalb selbst unter ODbL (Share-Alike) |

Die Web-Oberfläche nennt die Quellen im Seitenfuß. Die ausführliche Recherche zu weiteren offenen Quellen (OSM, eHYD, BAFU, Rijkswaterstaat u. a.) steht in [`docs/datenquellen.md`](docs/datenquellen.md). Für den Import von Slipanlagen und Marinas aus OpenStreetMap gilt zusätzlich: Die ODbL verlangt Namensnennung, und wer eine Datenbank mit OSM-Daten weitergibt, muss sie unter ODbL weitergeben (Share-Alike). Importierte Einträge tragen deshalb `Source = osm` und ihr OSM-Element und bleiben von den eigenen Daten getrennt: Der Import kopiert keine OSM-Werte in eigene Einträge, sondern hinterlegt dort höchstens das OSM-Element. Die API liefert bei Slipanlagen und Marinas `source` (`manual` oder `osm`) und, falls vorhanden, `osmElement` (z. B. `node/123`) mit.

## Lizenz

Slipways ist **source-available**, aber nicht Open Source im Sinne der OSI: Der Code ist öffentlich einsehbar, jede weitere Nutzung braucht eine Erlaubnis. Es gilt die [PolyForm Strict License 1.0.0](LICENSE).

- **Erlaubt ohne Rückfrage:** Code lesen, lokal ausprobieren und für nicht-kommerzielle, persönliche Zwecke nutzen (Lernen, Testen, Hobby).
- **Nur nach Rückfrage:** kommerzielle Nutzung, Änderungen weitergeben, Forks oder Kopien veröffentlichen, eine eigene Instanz öffentlich betreiben, Teile in andere Projekte übernehmen.
- **Anfragen** an Marcel Benders, slipways@marcelbenders.com. Eine Erlaubnis gilt nur schriftlich.

Die eigenen Daten (von Hand erfasste Slipanlagen, Marinas und Motorservices mit `Source = manual`) gehören ebenfalls dazu. Nicht unter diese Lizenz fallen die Daten Dritter: OpenStreetMap-Daten bleiben unter ODbL 1.0, PEGELONLINE-Daten unter DL-DE->Zero-2.0 (siehe „Datenquellen und Lizenzen“). Abhängigkeiten behalten ihre eigenen Lizenzen.

Beiträge (Pull Requests) sind willkommen; wer beiträgt, erklärt sich damit einverstanden, dass der Beitrag unter dieser Lizenz steht und Marcel Benders ihn auch anders lizenzieren darf.

## Betrieb

Die Produktion läuft auf slipways.de (im Tailnet `slipways-1`) mit Caddy für HTTPS ([`deploy/README.md`](deploy/README.md#https-mit-eigener-domain)). Eine Test-Stage läuft auf Proxmox in einem LXC mit Tailscale und nächtlicher Nextcloud-Sicherung. Beide spielen jeden neuen Stand auf `master` selbst ein (siehe [`deploy/README.md`](deploy/README.md)).

Abhängigkeiten hält Dependabot wöchentlich aktuell.

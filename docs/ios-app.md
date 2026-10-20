# iOS-App: Stand und Weg zurück auf `master`

Analyse vom 20.10.2026. Grundlage für die Arbeit an der iOS-App in einer eigenen Session.

## Wo die App liegt

- Die SwiftUI-App liegt nur auf dem Branch `origin/claude/project-thread-kuf5cb` (Commits `e865543`, `6d799dd`, `eb3c991`, `3b9ced6`) und wurde nie gemergt. Der Branch zweigt bei `ec6d813` (#57) ab und ist inzwischen 19 Commits hinter `master`.
- Auf dem Branch liegen auch die Android-App (`mobile/android/`) und die Workflows `.github/workflows/ios.yml` und `android.yml`. Außerdem enthält er einen Abschnitt „Mobile-Apps“ im README und Gradle in `.github/dependabot.yml`.
- Die alte Xamarin-App (`Mobile/Slipways.Mobile/Slipways.Mobile.iOS`) gibt es nur noch in der Git-Historie. Als Grundlage taugt sie nicht.

Dateien unter `mobile/ios/`:

| Pfad | Inhalt |
| --- | --- |
| `project.yml` | XcodeGen-Projekt, Bundle-ID `com.bvelop.slipways`, iOS 17, Swift 6, iPhone und iPad |
| `Slipways/Info.plist` | `SlipwaysAPIBaseURL` aus der Build-Einstellung `SLIPWAYS_API_BASE_URL`, `NSAllowsLocalNetworking` |
| `Slipways/SlipwaysApp.swift` | Einstieg, API-Client über `EnvironmentValues` |
| `Slipways/Views/SlipwayListView.swift` | Liste mit Suche (Debounce 300 ms), Pull-to-Refresh |
| `Slipways/Views/SlipwayDetailView.swift` | Details mit MapKit-Karte und „In Karten öffnen“ |
| `SlipwaysKit/` | Swift-Package: `SlipwaysAPI` (URLSession hinter dem Protokoll `HTTPTransport`), `Models.swift`, 3 Tests mit Swift Testing |

Login und Registrierung wurden mit `3b9ced6` ausgebaut. Die App ist also wie die Website reine Lesesicht.

## Was kaputt ist (geprüft)

1. **Die Liste lädt nicht.** Seit dem OSM-Import ist `water` in `SlipwaySummaryDto` und `SlipwayDetailsDto` nullable. In der Produktion haben 3.103 von 5.102 Slipanlagen kein Gewässer (4.996 davon mit `source = osm`). `SlipwaySummary.water` und `SlipwayDetails.water` sind in Swift aber nicht optional. Die echte Antwort von `https://slipways.de/api/slipways` mit `SlipwaysKit` zu dekodieren scheitert mit `DecodingError.valueNotFound … Path: [0].water`. Die App zeigt deshalb nur „Slipanlagen konnten nicht geladen werden“. Die vorhandenen Tests laufen unter Xcode 27 grün, weil ihre Fixture noch ein Gewässer enthält.
2. **Die Release-Adresse ist ein Platzhalter.** In `project.yml` steht `SLIPWAYS_API_BASE_URL: https://slipways.example.com` statt `https://slipways.de`.
3. **Der OSM-Hinweis fehlt.** Die App zeigt nirgends „© OpenStreetMap-Mitwirkende“, obwohl fast alle Einträge aus OSM (ODbL) stammen. Das ist Pflicht (siehe `CLAUDE.md`, Abschnitt „Lizenzen“).
4. **Die Rechtstexte versprechen mehr, als die App kann.** `web/src/pages/SupportPage.tsx` und `DatenschutzPage.tsx` sagen, die Apps laden „Slipanlagen und Pegelstände“. Die App kennt aber weder Pegel noch Gewässer. Die übrigen Aussagen passen: Karten über MapKit, nur Internetzugang, ab iOS 17.

## Was im Vergleich zur API fehlt

Maßgeblich sind `backend/src/Slipways.Api/Endpoints/Contracts.cs` und die Tabelle „API-Überblick“ im README.

- **`SlipwaySummaryDto`:** `source` fehlt, `water` muss optional werden.
- **`SlipwayDetailsDto`:** Es fehlen `url`, `operator`, `fee`, `access`, `openingHours`, `surface`, `source`, `osmElement`, `created` und `updated`. `costs` ist in der API `decimal?` und in Swift `Double?`, besser `Decimal?`.
- **Endpunkte, die die App nicht nutzt:**
  - `/api/waters` (mit `position` und `gauge`)
  - `/api/waters/{id}` (Slipanlagen, Pegel, Marinas, `shape` als GeoJSON, `bounds`)
  - `/api/waters/shapes`
  - `/api/stations`
  - `/api/marinas`
- **Ungenutzte Methode:** `SlipwaysAPI.waters()` dekodiert nur `Water(id, name)`.
- **Links:** Impressum, Datenschutz und Support sind in der App nicht verlinkt.

## Was für den App Store fehlt

- Ein App-Icon fehlt, denn auf dem Branch liegt kein `Assets.xcassets`.
- Signing-Team (`DEVELOPMENT_TEAM`) und Export-Compliance (`ITSAppUsesNonExemptEncryption = NO`) sind nicht eingetragen.
- Ein Privacy-Manifest (`PrivacyInfo.xcprivacy`) braucht die App erst, wenn sie Funktionen nutzt, die Apple begründet haben will, z. B. `UserDefaults`.
- Für die Store-Einträge sind Datenschutz- und Support-Adressen nötig: `https://slipways.de/datenschutz` und `https://slipways.de/support`, das Impressum steht unter `/impressum` (Routen in `web/src/routes.tsx`).

## Werkzeuge auf dem Mac

- Xcode 27 ist installiert, aber `xcode-select` zeigt auf die Command Line Tools. Für `xcodebuild` braucht es `sudo xcode-select -s /Applications/Xcode.app/Contents/Developer` oder `DEVELOPER_DIR=/Applications/Xcode.app/Contents/Developer` pro Aufruf.
- XcodeGen fehlt (`brew install xcodegen`). Die App selbst wurde deshalb noch nicht gebaut, nur `swift test --package-path SlipwaysKit`.
- Die CI auf dem Branch nutzt `macos-26` und `actions/checkout@v5`. Beides beim Übernehmen mit den anderen Workflows abgleichen.

## Vorgehen

1. **Neuer Branch von `master`.** `mobile/ios/` und `.github/workflows/ios.yml` per `git checkout origin/claude/project-thread-kuf5cb -- mobile/ios .github/workflows/ios.yml` übernehmen, nicht den alten Branch mit seinem Merge-Commit mergen. Den README-Abschnitt „Mobile-Apps“ und die Komponenten-Tabelle von dort übernehmen.
2. **App wieder lauffähig machen:**
   - Modelle an die DTOs anpassen und den Untertitel ohne Gewässer korrekt bilden.
   - Release-Adresse auf `https://slipways.de` setzen.
   - Tests mit `water: null` und einer OSM-Slipanlage als Fixture ergänzen.
3. **Lizenz:** Der Hinweis im Fuß der Website gilt nicht für die App. Sie ist ein eigenes Werk im Sinne der ODbL (§ 4.3) und braucht einen eigenen Hinweis. Nach den [Attribution Guidelines der OSMF](https://osmfoundation.org/wiki/Licence/Attribution_Guidelines) gehört er an die Karte, in eine Ecke oder daneben (alternativ auf einen Startbildschirm), und muss danach auffindbar bleiben, z. B. über (i) oder „Info“. Ein reiner Credits-Bildschirm reicht nur bei Spielen.
   - MapKit zeigt nur Apples eigenes „Rechtliches“ an, das deckt die OSM-Einträge nicht ab. Deshalb nicht „Kartendaten ©“ wie im Web, sondern „Daten © OpenStreetMap-Mitwirkende (ODbL)“ mit Link auf `https://www.openstreetmap.org/copyright`.
   - Den Hinweis an jeder Karte mit OSM-Einträgen und in den Details bei `source = osm` zeigen. Auf einer Info-Seite den vollständigen Hinweis, PEGELONLINE (DL-DE->Zero-2.0) sowie Impressum, Datenschutz und Support verlinken.
4. **Funktionen nachziehen, damit die Rechtstexte stimmen:**
   - Tab „Gewässer“ mit Pegelstand.
   - Gewässer-Detailseite mit Karte, Verlauf (`MKGeoJSONDecoder`), Slipanlagen, Pegeln und Marinas.
   - In den Slipway-Details Öffnungszeiten, Gebühr, Belag und Link ergänzen.
5. **Später, optional:** eine Übersichtskarte aller Slipanlagen. Bei rund 5.000 Pins braucht MapKit dafür Clustering. `/api/slipways` liefert alles ohne Seitenaufteilung, rund 1 MB.
6. **App Store:** Icon, Signing und Export-Compliance.

## Offene Entscheidungen

- Soll die Android-App aus demselben Branch mit auf Stand kommen? Sie hat sehr wahrscheinlich denselben Fehler mit `water: null` (nicht geprüft).
- Reichen für den ersten PR die Schritte 1 bis 3? Oder sollen Gewässer und Pegel (Schritt 4) gleich mit rein?

# Mobile-Apps

Stand 20.10.2026. Beide Apps sind reine Lesesichten wie die Website: kein Konto, keine Registrierung, nichts wird auf dem Gerät gespeichert. Bauen und Testen steht im README unter „Mobile-Apps“.

## Aufbau

| | iOS (`mobile/ios/`) | Android (`mobile/android/`) |
| --- | --- | --- |
| Oberfläche | SwiftUI, iOS 17, XcodeGen (`project.yml`) | Jetpack Compose, Material 3, ab Android 8.0 |
| API-Client und Modelle | Swift-Package `SlipwaysKit` | Kotlin-Modul `shared` (Ktor) |
| Tests | Swift Testing in `SlipwaysKit` | `shared/src/test` |
| Karten | MapKit | MapLibre mit Kacheln von OpenFreeMap (Datenschutzseite nennt beide); „In Karten öffnen“ übergibt an eine Karten-App |
| API-Adresse | `https://slipways.de` (`project.yml`), lokal per Schema-Umgebungsvariable `SLIPWAYS_API_BASE_URL` | `https://slipways.de` (`app/build.gradle.kts`), lokal per `-Pslipways.apiBaseUrl=…` |

Die Modelle spiegeln `backend/src/Slipways.Api/Endpoints/Contracts.cs`. Ändert sich ein DTO, beide Clients und die Fixtures in den Tests mitziehen. Die Fixtures sind gekürzte echte Antworten, darunter eine OSM-Slipanlage ohne Gewässer. Deutsche Texte für OSM-Tags und Pegelzustände (`Labels`, `Format`) liegen im Client-Paket und entsprechen `web/src/components/osmTags.ts` bzw. `waterLevel.ts`.

Beide Apps haben die Tabs Slipanlagen, Marinas, Gewässer und Info. Slipanlagen und Marinas sind je eine Karte mit Suchfeld (`PlaceOverview` bzw. `PlaceOverviewScreen`); die Liste darunter zeigt nur, was im Kartenausschnitt liegt (höchstens 250, sonst „Zoome hinein oder suche“), oder die Suchtreffer (`MapArea`/`Overview` im Client-Paket). Die Suche startet erst, wenn das Tippen kurz pausiert. Volle Bereiche der Karte zeigen Blasen mit der Anzahl statt einzelner Pins (iOS: Raster in `Overview.clusters`, Android: `GeoJsonSource` mit `cluster`); ein Tipp darauf zoomt hinein. Slipanlagen werden über die API gesucht, Marinas im Gerät. Eine Marina öffnet eine kurze Übersicht (iOS: Sheet, Android: Bottom-Sheet) mit Adresse, Webseite, Telefon, E-Mail, „Route planen“ und „In Karten öffnen“. Die Slipanlage zeigt dieselben Angaben wie im Web samt nächstem Pegel. Das Gewässer zeigt Pegel, Slipanlagen und Marinas, unter iOS dazu eine Karte mit Verlauf (`MapPolyline`/`MapPolygon`).

## Lizenzhinweis

Die Apps sind eigene Werke im Sinne der ODbL (§ 4.3), der Hinweis im Seitenfuß der Website gilt für sie nicht. Nach den [Attribution Guidelines der OSMF](https://osmfoundation.org/wiki/Licence/Attribution_Guidelines) steht „Daten © OpenStreetMap-Mitwirkende (ODbL)“ mit Link auf `https://www.openstreetmap.org/copyright`:

- in der Ecke jeder Karte mit OSM-Einträgen oder Gewässerverlauf (iOS, als Text ohne Link, damit ein Tipp in die Karte nicht den Browser öffnet; MapKits „Rechtliches“ deckt OSM nicht ab; unter Android bringt der OpenFreeMap-Stil den Hinweis selbst mit),
- unter Listen und Detailseiten mit OSM-Einträgen,
- vollständig auf der Info-Seite, zusammen mit PEGELONLINE (DL-DE->Zero-2.0), Impressum, Datenschutz und Support.

Neue Ansichten mit OSM-Daten brauchen den Hinweis ebenfalls.

Die Info-Seite nennt außerdem die Lizenz des Codes (PolyForm Strict 1.0.0, siehe `LICENSE` und README „Lizenz“) mit Kontaktadresse für Anfragen.

## Werkzeuge auf dem Mac

- Xcode 27 ist installiert, aber `xcode-select` zeigt auf die Command Line Tools. Für `xcodebuild` braucht es `sudo xcode-select -s /Applications/Xcode.app/Contents/Developer` oder `DEVELOPER_DIR=/Applications/Xcode.app/Contents/Developer` pro Aufruf.
- XcodeGen: `brew install xcodegen`.
- Die CI baut auf `macos-26` (`.github/workflows/ios.yml`) bzw. `ubuntu-latest` (`android.yml`).

## Für die Stores offen

Steht in [`offene-punkte.md`](offene-punkte.md) unter „Mobile-Apps“.

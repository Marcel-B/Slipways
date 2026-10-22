# Mobile-Apps

Stand 22.10.2026. Beide Apps sind Lesesichten: kein Konto, keine Registrierung, nichts wird auf dem Gerät gespeichert. Vorschläge senden (Nutzerbeiträge) können sie noch nicht. Bauen und Testen steht im README unter „Mobile-Apps“.

## Aufbau

| | iOS (`mobile/ios/`) | Android (`mobile/android/`) |
| --- | --- | --- |
| Oberfläche | SwiftUI, iOS 17, XcodeGen (`project.yml`) | Jetpack Compose, Material 3, ab Android 8.0 |
| API-Client und Modelle | Swift-Package `SlipwaysKit` | Kotlin-Modul `shared` (Ktor) |
| Tests | Swift Testing in `SlipwaysKit` | `shared/src/test` |
| Karten | MapKit | MapLibre mit Kacheln von OpenFreeMap (Datenschutzseite nennt beide); „In Karten öffnen“ übergibt an eine Karten-App |
| API-Adresse | `https://slipways.de` (`project.yml`), lokal per Schema-Umgebungsvariable `SLIPWAYS_API_BASE_URL` | `https://slipways.de` (`app/build.gradle.kts`), lokal per `-Pslipways.apiBaseUrl=…` |

Die Modelle spiegeln `backend/src/Slipways.Api/Endpoints/Contracts.cs`. Ändert sich ein DTO, beide Clients und die Fixtures in den Tests mitziehen. Die Fixtures sind gekürzte echte Antworten, darunter eine OSM-Slipanlage ohne Gewässer. Deutsche Texte für OSM-Tags und Pegelzustände (`Labels`, `Format`) liegen im Client-Paket und entsprechen `web/src/components/osmTags.ts` bzw. `waterLevel.ts`.

Beide Apps haben die Tabs Slipanlagen, Marinas, Gewässer und Info und zeigen dasselbe wie die Website (ohne Mitmachen):

- **Slipanlagen und Marinas** sind je eine Karte mit Suchfeld und Länderwahl (`PlaceOverview` bzw. `PlaceOverviewScreen`). Die Karte startet mit Europa und zeigt bis Zoom 5 eine Blase je Land (`Countries.group`, Schwelle `Overview.countryMaxZoom` bzw. `COUNTRY_MAX_ZOOM` wie `COUNTRY_MAX_ZOOM` im Web), darunter Blasen mit der Anzahl (iOS: Raster in `Overview.clusters`, Android: `GeoJsonSource` mit `cluster`); ein Tipp darauf zoomt hinein. Die Liste darunter zeigt nur, was im Kartenausschnitt liegt (höchstens 250, sonst „Zoome hinein oder suche“), oder die Suchtreffer. Die Suche startet erst, wenn das Tippen kurz pausiert. Slipanlagen werden über die API gesucht, Marinas im Gerät.
- **Gewässer** sind eine Karte mit Verläufen und Seen aus OpenStreetMap: im Kartenausschnitt geladen über `/api/waters/shapes?bbox=…&minExtent=…` (`ShapeArea`, gleiche Rasterung wie `shapeArea` im Web), bei einer Suche die der Treffer. Ein Tipp auf einen Verlauf zeigt das Gewässer mit „Öffnen“. Länderwahl über `WaterSummary.countries`.
- **Zoomstufen** zählen in den Client-Paketen wie im Web (Kacheln zu 256 Punkten). MapKit kennt keinen Zoom, iOS rechnet ihn aus Kartenausschnitt und Breite (`MapArea.zoom(width:)`); MapLibre zählt Kacheln zu 512, Android rechnet `+1` (`WEB_ZOOM_OFFSET`).
- **Slipanlage**: dieselben Angaben wie im Web samt nächstem Pegel, „Route planen“ und „In Karten öffnen“. Werte von Nutzern gewinnen über die eigenen und zeigen „von Nutzern ergänzt, Stand MM.JJJJ“ (`SlipwayDetails.value`); dazu „Was gibt es dort?“ und Tipps.
- **Marina** öffnet als Sheet (iOS) bzw. Bottom-Sheet (Android) aus der Karte, von der Slipanlage und vom Gewässer: Angaben (Nutzer vor eigenen vor „laut Webseite“, `MarinaDetails.value`), Slipanlagen der Marina, Tipps, nächster Pegel, „Route planen“.
- **Gewässer-Detail** zeigt Pegel, Slipanlagen und Marinas mit einer Karte samt Verlauf (iOS `MapPolyline`/`MapPolygon`, Android GeoJSON-Ebene); unter Android mit Legende wie im Web.
- **Detailkarten** starten weiter draußen und zoomen beim Öffnen hinein wie im Web (`DetailMap`), außer die Bewegung ist in den Systemeinstellungen reduziert bzw. Animationen sind aus.

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

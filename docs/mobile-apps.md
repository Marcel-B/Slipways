# Mobile-Apps

Stand 22.10.2026. Beide Apps können alles, was die Website kann: lesen und ohne Konto Vorschläge senden. Auf dem Gerät speichern sie nur die gesendeten Vorschläge. Bauen und Testen steht im README unter „Mobile-Apps“.

## Aufbau

| | iOS (`mobile/ios/`) | Android (`mobile/android/`) |
| --- | --- | --- |
| Oberfläche | SwiftUI, iOS 17, XcodeGen (`project.yml`) | Jetpack Compose, Material 3, ab Android 8.0 |
| API-Client und Modelle | Swift-Package `SlipwaysKit` | Kotlin-Modul `shared` (Ktor) |
| Tests | Swift Testing in `SlipwaysKit` | `shared/src/test` |
| Karten | MapKit | MapLibre mit Kacheln von OpenFreeMap (Datenschutzseite nennt beide); „In Karten öffnen“ übergibt an eine Karten-App |
| API-Adresse | `https://slipways.de` (`project.yml`), lokal per Schema-Umgebungsvariable `SLIPWAYS_API_BASE_URL` | `https://slipways.de` (`app/build.gradle.kts`), lokal per `-Pslipways.apiBaseUrl=…` |

Die Modelle spiegeln `backend/src/Slipways.Api/Endpoints/Contracts.cs`. Ändert sich ein DTO, beide Clients und die Fixtures in den Tests mitziehen. Die Fixtures sind gekürzte echte Antworten, darunter eine OSM-Slipanlage ohne Gewässer. Deutsche Texte für OSM-Tags und Pegelzustände (`Labels`, `Format`) liegen im Client-Paket und entsprechen `web/src/components/osmTags.ts` bzw. `waterLevel.ts`.

Beide Apps haben die Tabs Slipanlagen, Marinas, Gewässer und Info und zeigen dasselbe wie die Website:

- **Slipanlagen und Marinas** sind je eine Karte mit Suchfeld und Länderwahl (`PlaceOverview` bzw. `PlaceOverviewScreen`). Die Karte startet mit Europa und zeigt bis Zoom 5 eine Blase je Land (`Countries.group`, Schwelle `Overview.countryMaxZoom` bzw. `COUNTRY_MAX_ZOOM` wie `COUNTRY_MAX_ZOOM` im Web), darunter Blasen mit der Anzahl (iOS: Raster in `Overview.clusters`, Android: `GeoJsonSource` mit `cluster`); ein Tipp darauf zoomt hinein. Unter iOS füllt die Karte den ganzen Bildschirm, Suchfeld, Länderwahl und Plus schweben darüber, eine Liste gibt es nicht: Ein Tipp auf einen Pin öffnet den Eintrag, eine Zeile unten zeigt Laden, Fehler und die Zahl der Treffer. Android zeigt unter der Karte noch eine Liste mit dem, was im Kartenausschnitt liegt (höchstens 250, sonst „Zoome hinein oder suche“), oder den Suchtreffern. Die Suche startet erst, wenn das Tippen kurz pausiert. Slipanlagen werden über die API gesucht, Marinas im Gerät.
- **Auto-Zoom:** Die Karte bewegt sich nur, wenn jemand etwas verlangt: Steht der Suchbegriff, zeigt sie alle Treffer, bei einer Länderwahl das Land (ohne Ausreißer, `Overview.focus`). Beim Verschieben und Zoomen bewegt sie sich nie von selbst, und das Löschen der Suche lässt sie, wo sie ist. Liegen Treffer außerhalb des Ausschnitts, bietet iOS unten „Alle zeigen“ an (`Overview.anyOutside`).
- **Gewässer** sind eine Karte mit Verläufen und Seen aus OpenStreetMap: im Kartenausschnitt geladen über `/api/waters/shapes?bbox=…&minExtent=…` (`ShapeArea`, gleiche Rasterung wie `shapeArea` im Web), bei einer Suche die der Treffer. Ein Tipp auf einen Verlauf zeigt das Gewässer mit „Öffnen“. Unter iOS ebenfalls als Vollbild-Karte ohne Liste; Suchtreffer bekommen dort zusätzlich einen Pin, damit auch kleine Gewässer antippbar sind. iOS zeichnet nur die Teile der Verläufe in und um den Kartenausschnitt (`Geometry.clipped`), sonst legt MapKit für jeden Fluss eine Zeichenfläche in voller Länge an und die App läuft beim Hineinzoomen aus dem Speicher. Länderwahl über `WaterSummary.countries`.
- **Zoomstufen** zählen in den Client-Paketen wie im Web (Kacheln zu 256 Punkten). MapKit kennt keinen Zoom, iOS rechnet ihn aus Kartenausschnitt und Breite (`MapArea.zoom(width:)`); MapLibre zählt Kacheln zu 512, Android rechnet `+1` (`WEB_ZOOM_OFFSET`).
- **Slipanlage**: dieselben Angaben wie im Web samt nächstem Pegel, „Route planen“ und „In Karten öffnen“. Werte von Nutzern gewinnen über die eigenen und zeigen „von Nutzern ergänzt, Stand MM.JJJJ“ (`SlipwayDetails.value`); dazu „Was gibt es dort?“ und Tipps.
- **Marina** öffnet als Sheet (iOS) bzw. Bottom-Sheet (Android) aus der Karte, von der Slipanlage und vom Gewässer: Angaben (Nutzer vor eigenen vor „laut Webseite“, `MarinaDetails.value`), Slipanlagen der Marina, Tipps, nächster Pegel, „Route planen“.
- **Gewässer-Detail** zeigt Pegel, Slipanlagen und Marinas mit einer Karte samt Verlauf (iOS `MapPolyline`/`MapPolygon`, Android GeoJSON-Ebene); unter Android mit Legende wie im Web.
- **Mitmachen** wie im Web (`docs/nutzerbeitraege.md`): Stifte neben den Werten, „Fehlt noch“, „Angaben ankreuzen“ unter „Was gibt es dort?“, „Tipp geben“, „Etwas stimmt nicht“ und „In OpenStreetMap bearbeiten“ auf Slipanlage und Marina (`ContributeSections` bzw. `ContributeSection`, Formular `SuggestSheet`). Felder, Auswahlwerte und Längen stehen in `Contributions` und entsprechen `web/src/contribute/fields.ts` und `SuggestionFields.cs`. Das Plus auf der Slipanlagen-Karte schlägt eine neue Slipanlage vor: Pin in der Kartenmitte, senden erst ab Zoom 13; meldet der Server Slipanlagen in der Nähe (409), zeigt das Formular sie mit „Nein, trotzdem neu eintragen“.
- **Spam-Schutz**: Das Formular holt beim Öffnen `/api/suggestions/challenge` und löst den Proof of Work im Hintergrund (`Proof.solve`, iOS mit CryptoKit, unter Linux für die Tests mit eigenem SHA-256; Android mit `MessageDigest`). Das Honeypot-Feld des Webs fehlt, es zielt auf Bots in Browsern.
- **Meine Vorschläge** im Info-Tab: Die Apps merken sich Kennung und Token gesendeter Vorschläge (`SuggestionStore`; iOS `UserDefaults`, Android `SharedPreferences`, 90 Tage wie `localStorage` im Web), zeigen den Stand und erlauben „Zurückziehen“, solange er offen ist. Daraus kommt auch „Dein Vorschlag wird geprüft“ an Werten. iOS meldet das im Privacy-Manifest (`PrivacyInfo.xcprivacy`, Grund CA92.1, dazu Nutzerinhalte ohne Tracking).
- **Detailkarten** starten weiter draußen und zoomen beim Öffnen hinein wie im Web (`DetailMap`), außer die Bewegung ist in den Systemeinstellungen reduziert bzw. Animationen sind aus.

## Lizenzhinweis

Die Apps sind eigene Werke im Sinne der ODbL (§ 4.3), der Hinweis im Seitenfuß der Website gilt für sie nicht. Nach den [Attribution Guidelines der OSMF](https://osmfoundation.org/wiki/Licence/Attribution_Guidelines) steht „Daten © OpenStreetMap-Mitwirkende (ODbL)“ mit Link auf `https://www.openstreetmap.org/copyright`:

- in der Ecke jeder Karte mit OSM-Einträgen oder Gewässerverlauf (iOS, als Text ohne Link, damit ein Tipp in die Karte nicht den Browser öffnet; MapKits „Rechtliches“ deckt OSM nicht ab; unter Android bringt der OpenFreeMap-Stil den Hinweis selbst mit),
- unter Listen (Android) und Detailseiten mit OSM-Einträgen,
- vollständig auf der Info-Seite, zusammen mit PEGELONLINE (DL-DE->Zero-2.0), Impressum, Datenschutz und Support.

Neue Ansichten mit OSM-Daten brauchen den Hinweis ebenfalls.

Die Info-Seite nennt außerdem die Lizenz des Codes (PolyForm Strict 1.0.0, siehe `LICENSE` und README „Lizenz“) mit Kontaktadresse für Anfragen.

## Werkzeuge auf dem Mac

- Xcode 27 ist installiert, aber `xcode-select` zeigt auf die Command Line Tools. Für `xcodebuild` braucht es `sudo xcode-select -s /Applications/Xcode.app/Contents/Developer` oder `DEVELOPER_DIR=/Applications/Xcode.app/Contents/Developer` pro Aufruf.
- XcodeGen: `brew install xcodegen`.
- Die CI baut auf `macos-26` (`.github/workflows/ios.yml`) bzw. `ubuntu-latest` (`android.yml`).

## TestFlight

`.github/workflows/testflight.yml` archiviert die iOS-App bei jedem Push auf `master`, der `mobile/ios` ändert, und lädt sie zu TestFlight hoch. Von Hand: in GitHub unter Actions „TestFlight“ starten oder `gh workflow run testflight.yml`.

- Signiert wird automatisch über einen App Store Connect API-Key (`-allowProvisioningUpdates`). Der Key braucht die Rolle „Admin“, damit Xcode das Verteilungszertifikat in der Cloud anlegen darf.
- Die Secrets `APPLE_TEAM_ID`, `APP_STORE_CONNECT_KEY_ID`, `APP_STORE_CONNECT_ISSUER_ID` und `APP_STORE_CONNECT_KEY` (die .p8-Datei in Base64) trägt `mobile/ios/scripts/testflight-secrets.sh` ein; es fragt die Werte ab und braucht die GitHub CLI. Fehlen sie, überspringt der Workflow den Upload mit einer Warnung.
- Die Build-Nummer ist der Zeitpunkt des Laufs (`yyyyMMddHHmm`, UTC), die Version (`MARKETING_VERSION`) steht in `project.yml`.
- Die App muss in App Store Connect mit der Bundle-ID `com.bvelop.slipways` angelegt sein.

## Für die Stores offen

Steht in [`offene-punkte.md`](offene-punkte.md) unter „Mobile-Apps“.

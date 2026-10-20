# Offene Punkte

Was bei einer Umsetzung offen geblieben ist und noch eine Entscheidung oder Arbeit braucht. Erledigte Punkte hier löschen.

## Überwachung (seit 20.10.2026 in Betrieb)

Messwerte, Logs und das Grafana-Dashboard **Slipways** laufen für slipways.de (`slipways-1`). Beschrieben in README („Überwachung“) und `deploy/README.md` („Überwachung“), das Dashboard liegt im YuE-UI-Repo (`deploy/monitoring/grafana/dashboards/slipways.json`). Die Test-Stage im LXC ist bewusst nicht angebunden.

1. **Tailscale-Policy.** slipways-1 ist öffentlich erreichbar und im Tailnet. Vorschlag in `deploy/README.md`: `tag:slipways`, nur `tcp:3100` zu `monitoring` (`100.73.171.118`). Ob die Policy schon so steht, ist nicht bestätigt. Die Test-Stage im LXC hängt außerdem direkt im Heimnetz (eigenes VLAN oder Proxmox-Firewall).
2. **Steckbrief.** Die Tabelle „Angebundene Apps“ steht in keinem Repo, sondern im Monitoring-Steckbrief; dort Slipways nachtragen, falls noch nicht geschehen.
3. **Alarme** (z. B. `time() - slipways_job_last_success_timestamp_seconds{task="gauges"} > 3600`) gibt es noch nicht. Der Steckbrief nennt auch keinen Alertmanager.
4. **Skripte nachschärfen.** `add-prometheus-target.sh` und `setup-loki.sh` sollen `jq` selbst installieren, wenn es fehlt (wie `add-tailscale.sh`). Die Loki-Prüfung in `setup-loki.sh` wartet nur 5 Sekunden: Der erste Verbindungsaufbau eines neuen Geräts im Tailnet kann länger dauern und galt beim ersten Lauf als Fehler. Besser länger warten, einmal wiederholen und in der Meldung Timeout, abgelehnte Verbindung und HTTP-Status unterscheiden.

## OpenStreetMap-Import (Branch `feature/osm-import`, 20.10.2026)

Der Import selbst ist im README beschrieben (Abschnitt „Slipanlagen und Marinas aus OpenStreetMap“), der Code liegt in `backend/src/Slipways.Infrastructure/Osm/`.

1. **Namen ohne Aussage.** Ohne `name` und `addr:city` heißt ein Eintrag nur „Slipanlage“ bzw. „Marina“. Beim Probelauf für Österreich traf das 349 von 391 Slipanlagen. Ideen: Gewässername anhängen („Slipanlage Donau“, wenn `WaterId` gesetzt ist) oder den Ort per Nominatim-Rückwärtssuche holen (1 Anfrage/s, also bei ~5.000 Einträgen einmalig rund 1,5 h). Entscheidung steht aus.
2. **Karibische Niederlande.** Die Abfrage nutzt die Fläche `ISO3166-1=NL`, `admin_level=2` (Relation 2323309). Sie umfasst vermutlich auch Bonaire, Sint Eustatius und Saba, dann kämen ein paar karibische Einträge mit. Nicht geprüft; bei Bedarf nach Breitengrad filtern (alles südlich von 50° N bei NL verwerfen).
3. **Schwelle für die Gewässerzuordnung.** 300 m zum vereinfachten Verlauf (`OsmImport.WaterDistance`) sind ein Schätzwert. Bei breiten Flüssen, deren Verlauf als Mittellinie vorliegt, kann das zu knapp sein, bei kleinen Seen neben großen Flüssen zu großzügig. Nach dem ersten vollen Import stichprobenartig prüfen.

## Angaben von den Webseiten der Marinas (20.10.2026)

Beschrieben im README („Angaben von den Webseiten der Marinas“), Code in `backend/src/Slipways.Infrastructure/Websites/`.

1. **Ausbeute prüfen.** Aus dem Cloud-Container waren die Webseiten nicht erreichbar, die Trefferquote ist also nur geschätzt (viele kleine Vereinsseiten ohne strukturierte Daten). Nach dem ersten Durchlauf (rund 2.000 Seiten, bei 200 je Stunde etwa ein halber Tag) `SELECT Result, COUNT(*) FROM WebsiteChecks GROUP BY Result;` ansehen und Stichproben der übernommenen Werte prüfen.
2. **Seiten, die erst per JavaScript entstehen**, liefern nichts; ein Headless-Browser wäre zu schwer für den Server. Bewusst offen.
3. **Slipanlagen.** Die Tabelle `PlaceInfos` kann auch Slipanlagen aufnehmen, der Job liest aber nur Marinas. Bei Slipanlagen zeigt `url` oft auf eine Gemeindeseite mit vielen Nummern, deshalb erst nach der Auswertung von Punkt 1 entscheiden.

## Nutzerbeiträge (20.10.2026)

Vorschläge, Proof of Work, Moderation und Abgleich nach dem OSM-Import sind im Web und im Backend umgesetzt (README „Nutzerbeiträge“, Konzept in [`nutzerbeitraege.md`](nutzerbeitraege.md)).

1. **Moderation einrichten.** Auf slipways-1 einmal `tailscale serve --bg --https=9465 http://127.0.0.1:9465` ausführen (siehe `deploy/README.md`, „Moderation“). Bis dahin sammeln sich Vorschläge ungesehen. Für die Test-Stage nach Bedarf ebenso.
2. **Alarm bei wartenden Vorschlägen** (`slipways_suggestions_pending`), sobald es Alarme gibt (siehe Überwachung, Punkt 3).
3. **Apps (Stufe 3).** iOS und Android zeigen `infos` noch nicht und können keine Vorschläge senden. Dieselben Endpunkte nutzen, den Proof of Work nativ lösen (SHA-256 mit CryptoKit bzw. `MessageDigest`). `SlipwayDetailsDto` hat `infos` neu; die Apps ignorieren unbekannte Felder, deshalb ist das noch nicht nachgezogen.
4. **Fotos (Stufe 4).** Von Marcel gewünscht: Upload über die Moderation, serverseitig neu kodieren (WebP, lange Kante 1600 px, Vorschau 400 px), EXIF entfernen, keine Originale, höchstens etwa 5 je Eintrag. Speicher im Volume neben der Datenbank, ins Nextcloud-Backup aufnehmen.
5. **Bestätigungen zählen.** Fakten zeigen nur den neuesten Wert der Nutzer. Das Konzept sah „3× bestätigt“ vor; dafür bräuchte `PlaceInfos` einen Zähler, oder die Freigabe zählt gleiche Vorschläge mit.
6. **Werte vor der Freigabe ändern.** Die Moderation kann nur annehmen oder ablehnen. Ist ein Vorschlag fast richtig, muss er abgelehnt und der Eintrag von Hand korrigiert werden.
7. **Extras und Fakten.** Die alten `Extras` der eigenen Slipanlagen (z. B. Parkplatz, Steg) stehen neben den neuen Fakten. Zusammenführen (Extras in Fakten umschreiben) und die alten `Pro`/`Contra`/`Rating`-Werte als Tipps übernehmen oder ausblenden; Entscheidung steht aus.
8. **Marinas: „Dein Vorschlag wird geprüft“.** Der Hinweis verschwindet bei Slipanlagen, sobald der Eintrag sich ändert. Bei eigenen Marinas fehlt `updated` in der API, dort bleibt er bis zu 30 Tage stehen.

## Optik des Webs (PR #69, 20.10.2026)

1. **Kartenstil.** Die Karte nutzt weiter die bunten OSM-Standardkacheln, auf denen die Pins wenig hervorstechen. Ruhiger wäre z. B. OpenFreeMap (Vektorkacheln, ohne Schlüssel, braucht MapLibre GL bzw. `@maplibre/maplibre-gl-leaflet`). Jeder neue Kachel-Server muss in die Datenschutzseite und in die Content-Security-Policy (`web/security-headers.conf`). Entscheidung steht aus, Recherche in den Projektdateien unter `recherche/ui-framework-und-design.md`.

## Suchmaschinen (20.10.2026)

Beschrieben im README („Suchmaschinen“).

1. **Search Console.** slipways.de bei der Google Search Console und den Bing Webmaster Tools anmelden (DNS-TXT-Eintrag bei Contabo) und `https://slipways.de/sitemap.xml` einreichen. Kann nur Marcel.
2. **Vorschaubild.** Für Link-Vorschauen (`og:image`) gibt es noch kein Bild; das Favicon ist ein SVG und wird dafür nicht genommen. Ein PNG mit 1200×630 px nach `web/public/` legen und in `SeoEndpoints.Render` eintragen.
3. **Inhalt ohne JavaScript.** Serverseitig kommen nur Kopfdaten; Text und Links der Seiten rendert erst React. Google und Bing führen JavaScript aus, andere Crawler sehen nur Titel und Beschreibung. Falls das nicht reicht: ein kurzer Inhalt in `#root` per zweitem Include oder Prerendering.

## Mobile-Apps (20.10.2026)

Beide Apps laden wieder, haben Gewässer, Pegel, OSM-Hinweis und Info-Seite. Aufbau in [`docs/mobile-apps.md`](mobile-apps.md).

1. **App Store.** Es fehlen App-Icon (`Assets.xcassets`), Signing-Team (`DEVELOPMENT_TEAM`) und Screenshots. `ITSAppUsesNonExemptEncryption = NO` ist eingetragen. Ein Privacy-Manifest braucht die App erst, wenn sie z. B. `UserDefaults` nutzt.
2. **Google Play.** Es fehlen ein Release-Signing, ein adaptives Icon (bisher nur ein Vektor-Platzhalter) und Screenshots.
3. **AGP von Hand anheben.** Dependabot hebt das Android Gradle Plugin nur noch um Patch-Versionen an, weil eine neue Minor-Version (z. B. 9.4) erst mit einer passenden stabilen Android-Studio-Version öffnet. Stand: AGP 9.3.1. Sobald Android Studio die nächste Minor-Version unterstützt, in `mobile/android/gradle/libs.versions.toml` anheben.

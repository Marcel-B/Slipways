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
2. **Telefon und E-Mail für Marinas.** OSM hat oft `phone`/`contact:phone` und `email`/`contact:email`. Die Spalten `Marina.Phone`/`Email` gibt es schon, der Import übernimmt die Tags aber nicht, weil sie nicht in der vereinbarten Tag-Liste standen. Für Einträge mit `Source = osm` leicht nachzurüsten (`ApplyMarina` in `OsmImport.cs`).
3. **Karibische Niederlande.** Die Abfrage nutzt die Fläche `ISO3166-1=NL`, `admin_level=2` (Relation 2323309). Sie umfasst vermutlich auch Bonaire, Sint Eustatius und Saba, dann kämen ein paar karibische Einträge mit. Nicht geprüft; bei Bedarf nach Breitengrad filtern (alles südlich von 50° N bei NL verwerfen).
4. **Schwelle für die Gewässerzuordnung.** 300 m zum vereinfachten Verlauf (`OsmImport.WaterDistance`) sind ein Schätzwert. Bei breiten Flüssen, deren Verlauf als Mittellinie vorliegt, kann das zu knapp sein, bei kleinen Seen neben großen Flüssen zu großzügig. Nach dem ersten vollen Import stichprobenartig prüfen.

## Optik des Webs (PR #69, 20.10.2026)

1. **Kartenstil.** Die Karte nutzt weiter die bunten OSM-Standardkacheln, auf denen die Pins wenig hervorstechen. Ruhiger wäre z. B. OpenFreeMap (Vektorkacheln, ohne Schlüssel, braucht MapLibre GL bzw. `@maplibre/maplibre-gl-leaflet`). Jeder neue Kachel-Server muss in die Datenschutzseite. Entscheidung steht aus, Recherche in den Projektdateien unter `recherche/ui-framework-und-design.md`.

## iOS-App (Analyse 20.10.2026)

Die SwiftUI-App liegt nur auf dem ungemergten Branch `origin/claude/project-thread-kuf5cb` und lädt gegen die heutige API keine Slipanlagen mehr, weil `water` seit dem OSM-Import null sein kann. Befund, fehlende Funktionen und Vorgehen stehen in [`docs/ios-app.md`](ios-app.md).

1. **Umfang.** Soll die Android-App aus demselben Branch mit auf Stand kommen? Reicht für den ersten PR „lauffähig und lizenzkonform“, oder sollen Gewässer und Pegel gleich mit rein? Entscheidung steht aus.

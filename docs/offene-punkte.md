# Offene Punkte

Was bei einer Umsetzung offen geblieben ist und noch eine Entscheidung oder Arbeit braucht. Erledigte Punkte hier löschen.

## Überwachung (Branch `feature/monitoring`, 20.10.2026)

Beschrieben in README („Überwachung“) und `deploy/README.md` („Überwachung“).

1. **Auf slipways-1 noch nicht eingerichtet.** Es fehlen `tailscale serve --bg --https=9464 http://127.0.0.1:9464` auf slipways-1, das Prometheus-Ziel im Monitoring-LXC (`deploy/monitoring/add-prometheus-target.sh`) und die Loki-Einträge in `/opt/slipways/.env` (`deploy/lxc/setup-loki.sh` auf slipways-1). Beide Skripte sind nur in Teilen getestet: Prüfung aus dem Container, Auslesen der Messwerte, Prometheus-Antwort. Ein Lauf auf den echten Servern steht aus. Danach prüfen: `slipways` als `up` unter `http://monitoring:9090/targets`, Zeilen mit `{app="slipways", host="slipways-1"}` in Grafana. Die Test-Stage im LXC wird bewusst nicht angebunden.
2. **Ungetestet:** ob `tailscale serve --https=9464` neben Dockers Bindung an `127.0.0.1:9464` startet (sollte, andere Adresse), und ob der API-Container `monitoring` per MagicDNS auflöst. Wenn nicht, die 100.x-Adresse nehmen (siehe `deploy/README.md`).
3. **Tailscale-Policy.** slipways-1 ist öffentlich erreichbar und seit 20.10.2026 im Tailnet. Vorschlag in `deploy/README.md`: `tag:slipways`, nur `tcp:3100` zu `monitoring`. Die aktuelle Policy ist unbekannt. Die Test-Stage im LXC hängt außerdem direkt im Heimnetz (eigenes VLAN oder Proxmox-Firewall).
4. **Steckbrief im YuE-UI-Repo.** Die Tabelle „Angebundene Apps“ in `deploy/monitoring/` von `Marcel-B/YuE-UI` um Slipways ergänzen, sobald es läuft.
5. **Dashboard fehlt.** Ein `Slipways`-Dashboard (JSON in `deploy/monitoring/grafana/dashboards/` im YuE-UI-Repo) ist noch nicht gebaut. Sinnvoll: Alter des letzten Erfolgs je Job, Fehlläufe, Anfragen und 5xx je Route, Speicher, Einträge nach Quelle.
6. **Alarme** (z. B. `time() - slipways_job_last_success_timestamp_seconds{job="gauges"} > 3600`) gibt es noch nicht. Der Steckbrief nennt auch keinen Alertmanager.

## OpenStreetMap-Import (Branch `feature/osm-import`, 20.10.2026)

Der Import selbst ist im README beschrieben (Abschnitt „Slipanlagen und Marinas aus OpenStreetMap“), der Code liegt in `backend/src/Slipways.Infrastructure/Osm/`.

1. **Namen ohne Aussage.** Ohne `name` und `addr:city` heißt ein Eintrag nur „Slipanlage“ bzw. „Marina“. Beim Probelauf für Österreich traf das 349 von 391 Slipanlagen. Ideen: Gewässername anhängen („Slipanlage Donau“, wenn `WaterId` gesetzt ist) oder den Ort per Nominatim-Rückwärtssuche holen (1 Anfrage/s, also bei ~5.000 Einträgen einmalig rund 1,5 h). Entscheidung steht aus.
2. **Import bei jedem Start.** Wie die anderen Jobs läuft `OsmImportWorker` beim Start und dann alle 30 Tage, also schickt jeder Deploy bzw. Neustart 8 große Overpass-Abfragen (4 Länder × 2 Arten). Abhilfe wäre, den Zeitpunkt des letzten erfolgreichen Laufs zu speichern (z. B. kleine Tabelle) und beim Start nur zu importieren, wenn er älter als `Interval` ist. Der öffentliche Server empfiehlt für automatisierte Nutzung eine eigene Overpass-Instanz (`Osm:BaseUrl`).
3. **Telefon und E-Mail für Marinas.** OSM hat oft `phone`/`contact:phone` und `email`/`contact:email`. Die Spalten `Marina.Phone`/`Email` gibt es schon, der Import übernimmt die Tags aber nicht, weil sie nicht in der vereinbarten Tag-Liste standen. Für Einträge mit `Source = osm` leicht nachzurüsten (`ApplyMarina` in `OsmImport.cs`).
4. **Karibische Niederlande.** Die Abfrage nutzt die Fläche `ISO3166-1=NL`, `admin_level=2` (Relation 2323309). Sie umfasst vermutlich auch Bonaire, Sint Eustatius und Saba, dann kämen ein paar karibische Einträge mit. Nicht geprüft; bei Bedarf nach Breitengrad filtern (alles südlich von 50° N bei NL verwerfen).
5. **TLS im Container nicht geprüft.** overpass-api.de akzeptiert nur TLS 1.3. Auf macOS braucht .NET dafür `DOTNET_SYSTEM_NET_SECURITY_USENETWORKFRAMEWORK=1` (siehe README). Im Linux-Container (OpenSSL) sollte es ohne Zutun gehen, getestet ist das nicht. Nach dem ersten Deploy im Log auf „OpenStreetMap import finished“ bzw. „failed“ achten.
6. **Schwelle für die Gewässerzuordnung.** 300 m zum vereinfachten Verlauf (`OsmImport.WaterDistance`) sind ein Schätzwert. Bei breiten Flüssen, deren Verlauf als Mittellinie vorliegt, kann das zu knapp sein, bei kleinen Seen neben großen Flüssen zu großzügig. Nach dem ersten vollen Import stichprobenartig prüfen.

## Optik des Webs (PR #69, 20.10.2026)

1. **Kartenstil.** Die Karte nutzt weiter die bunten OSM-Standardkacheln, auf denen die Pins wenig hervorstechen. Ruhiger wäre z. B. OpenFreeMap (Vektorkacheln, ohne Schlüssel, braucht MapLibre GL bzw. `@maplibre/maplibre-gl-leaflet`). Jeder neue Kachel-Server muss in die Datenschutzseite. Entscheidung steht aus, Recherche in den Projektdateien unter `recherche/ui-framework-und-design.md`.

# Betrieb auf Proxmox (LXC)

`proxmox/create-lxc.sh` legt auf dem Proxmox-Host einen unprivilegierten Debian-Container an, installiert Docker und startet darin den ganzen Stack aus `compose.yaml` (API mit SQLite, Web). Dazu kommen Tailscale, eine nächtliche Sicherung in die Nextcloud und ein Auto-Update, das jeden neuen Stand auf `master` selbst einspielt.

```bash
# in der Proxmox-Shell als root
curl -fsSLO https://raw.githubusercontent.com/Marcel-B/Slipways/master/deploy/proxmox/create-lxc.sh

TS_AUTHKEY=tskey-auth-… \
NEXTCLOUD_WEBDAV_URL=https://cloud.example.de/remote.php/dav/files/<benutzer>/Backups/slipways \
NEXTCLOUD_USERNAME=<benutzer> \
NEXTCLOUD_APP_PASSWORD=<app-passwort> \
bash create-lxc.sh
```

Am Ende stehen die Adressen da: `http://<lxc-ip>` im LAN und `http://slipways.<tailnet>.ts.net` über Tailscale. Die API liegt unter `/api`, z. B. `/api/slipways`.

## Einstellungen

Alle über Umgebungsvariablen beim Aufruf:

| Variable | Standard | Bedeutung |
|---|---|---|
| `CTID` | nächste freie ID | Container-ID |
| `CT_HOSTNAME` | `slipways` | Hostname, auch Name im Tailnet |
| `STORAGE` | `local-lvm` | Speicher für die Disk |
| `TEMPLATE_STORAGE` | `local` | Speicher für die Debian-Vorlage |
| `TEMPLATE` | neueste `debian-13`/`debian-12-standard` für die Architektur des Hosts | z. B. `TEMPLATE=debian-12-standard_12.12-1_amd64.tar.zst` |
| `BRIDGE` | `vmbr0` | Netzwerk-Bridge |
| `IP` / `GATEWAY` | `dhcp` | z. B. `IP=192.168.1.60/24 GATEWAY=192.168.1.1` |
| `DISK_GB` / `MEMORY_MB` / `CORES` | `16` / `2048` / `2` | Ressourcen; die Builds beim Update brauchen den Speicher |
| `BRANCH` | `master` | Branch, der gebaut und aktualisiert wird |
| `GITHUB_TOKEN` | – | nur bei privatem Repository (Token mit Lesezugriff, bleibt für den Updater im LXC) |
| `PORT` | `80` | Port der Web-Oberfläche |
| `UPDATE_INTERVAL` | `5min` | wie oft der Updater nach neuen Commits schaut |
| `BACKUP_AT` | `03:00` | Uhrzeit der nächtlichen Sicherung |
| `NEXTCLOUD_WEBDAV_URL`, `NEXTCLOUD_USERNAME`, `NEXTCLOUD_APP_PASSWORD` | – | Nextcloud-Sicherung; ohne sie ist sie aus |
| `TAILSCALE` | `1` | `0`: ohne Tailscale |
| `TS_AUTHKEY` | – | Auth-Key fürs Tailnet; ohne zeigt Tailscale einen Login-Link an |
| `TS_SERVE` | `0` | `1`: zusätzlich `https://slipways.<tailnet>.ts.net` über `tailscale serve` (MagicDNS und HTTPS-Zertifikate im Tailnet an) |

Im LXC liegt alles unter `/opt/slipways`. Die Einstellungen des Stacks stehen in `/opt/slipways/.env` (nur root lesbar). Die Datenbank ist die SQLite-Datei `slipways.db` im Docker-Volume `slipways_api-data`. `deploy/compose.lxc.yaml` ergänzt `compose.yaml` für den Betrieb: Production-Modus (ohne Scalar), API nur lokal auf `127.0.0.1:5080`, Neustart nach einem Reboot.

## HTTPS mit eigener Domain

Für einen öffentlichen Server mit eigener Domain (z. B. ein Hetzner-Cloud-Server mit Debian) steht Caddy vor der Web-App: `deploy/compose.https.yaml` startet ihn auf Port 80 und 443, `deploy/Caddyfile` leitet alles an die Web-App weiter. Caddy holt das Zertifikat selbst bei Let's Encrypt und erneuert es, HTTP und `www.` leiten auf `https://<domain>` um. Die Web-App ist dann nur noch über Caddy erreichbar.

1. Beim Domain-Anbieter einen A-Eintrag (IPv4) und einen AAAA-Eintrag (IPv6) für die Domain und für `www` auf den Server setzen.
2. In `/opt/slipways/.env` die Datei ergänzen und die Domain eintragen:
   ```
   COMPOSE_FILE=compose.yaml:deploy/compose.lxc.yaml:deploy/compose.https.yaml
   SLIPWAYS_DOMAIN=slipways.de
   ```
3. `cd /opt/slipways && docker compose up -d --remove-orphans`, das Zertifikat ist nach einigen Sekunden da (`docker compose logs caddy`).

Die Zertifikate liegen im Volume `slipways_caddy-data`. Ports 80 und 443 (TCP, für HTTP/3 auch UDP 443) müssen von außen erreichbar sein.

## Tailscale

Tailscale läuft direkt im LXC, kein Sidecar-Container. `create-lxc.sh` reicht dafür `/dev/net/tun` durch und ruft `proxmox/add-tailscale.sh` auf. Für einen bestehenden Container, auch nachträglich:

```bash
# in der Proxmox-Shell als root
CTID=<CTID> TS_AUTHKEY=tskey-auth-… bash add-tailscale.sh
```

Mehrmals aufrufen schadet nicht. Tailscale startet ohne `--accept-routes`, damit ein Subnet-Router im Tailnet das LAN nicht umleitet.

## Überwachung

Prometheus und Loki laufen im gemeinsamen Monitoring-LXC `monitoring` (Steckbrief und Quelle: [`Marcel-B/YuE-UI` → `deploy/monitoring/`](https://github.com/Marcel-B/YuE-UI/tree/main/deploy/monitoring)). Angebunden ist nur die Produktion slipways.de (im Tailnet `slipways-1`), nicht die Test-Stage im Proxmox-LXC, damit sich die Daten nicht mischen. Was die API misst, steht im [README](../README.md#überwachung).

**Messwerte.** Mit `deploy/compose.lxc.yaml` gibt die API `/metrics` nur auf `127.0.0.1:9464` frei (`SLIPWAYS_METRICS_PORT`). Ins Tailnet bringt es `tailscale serve`, mit HTTPS und nur für Geräte im Tailnet. Auf slipways-1 läuft Tailscale direkt auf dem Server:

```bash
# auf slipways-1 als root; die Einstellung überlebt Neustarts
tailscale serve --bg --https=9464 http://127.0.0.1:9464
tailscale serve status
```

Das braucht HTTPS-Zertifikate im Tailnet (Admin-Konsole → DNS → HTTPS Certificates). Der Metrik-Port der API beantwortet nichts außer `/metrics`. Für einen LXC erledigt das `add-tailscale.sh` mit `TS_METRICS=1`.

Danach im Monitoring-LXC das Ziel anlegen. Das erledigt ein Skript: Es prüft, ob `/metrics` über Tailscale erreichbar ist, schreibt die Datei und wartet, bis Prometheus das Ziel als `up` führt.

```bash
# im Monitoring-LXC als root
curl -fsSLO https://raw.githubusercontent.com/Marcel-B/Slipways/master/deploy/monitoring/add-prometheus-target.sh
bash add-prometheus-target.sh
# oder aus der Proxmox-Shell:
curl -fsSL https://raw.githubusercontent.com/Marcel-B/Slipways/master/deploy/monitoring/add-prometheus-target.sh | pct exec <CTID> -- bash -s
```

Die Datei, die es anlegt (Prometheus liest sie binnen einer Minute):

```yaml
# /opt/monitoring/prometheus/targets/slipways.yml
- targets: [slipways-1.<tailnet>.ts.net:9464]
  labels:
    app: slipways
    __scheme__: https
    __metrics_path__: /metrics
```

Prüfen unter `http://monitoring:9090/targets`: Dort muss `slipways` als `up` stehen.

**Logs.** Auf slipways-1 trägt ein Skript die Werte in `/opt/slipways/.env` ein und startet die API neu (einige Sekunden nicht erreichbar):

```bash
# auf slipways-1 als root
bash /opt/slipways/deploy/lxc/setup-loki.sh
```

Das Skript prüft zuerst vom Server und dann aus dem API-Container heraus, ob Loki antwortet (`/ready`). Ob der Container den Tailscale-Namen `monitoring` auflösen kann, hängt davon ab, welchen DNS-Server Docker vom Server übernimmt. Mit systemd-resolved (`127.0.0.53`) nimmt Docker einen öffentlichen DNS-Server, und MagicDNS-Namen gehen nicht. Dann trägt das Skript die 100.x-Adresse von `monitoring` ein. Als `host`-Label nimmt es den Namen im Tailnet (`slipways-1`). Am Ende wartet es, bis `slipways_loki_lines_pushed_total` steigt. Von Hand wären es diese Einträge, danach `docker compose up -d`:

```
SLIPWAYS_LOKI_URL=http://monitoring:3100
SLIPWAYS_MONITORING_HOST=slipways-1
```

Ohne `SLIPWAYS_MONITORING_HOST` steht in Loki `host="unknown"`. Kommen später keine Zeilen mehr an, steigt `slipways_loki_lines_dropped_total{reason="push_failed"}`, und im Log steht „Loki is unreachable“.

**Absichern.** slipways.de ist öffentlich erreichbar und zugleich im Tailnet. Damit ein übernommener Server nicht das ganze Tailnet erreicht, sollte slipways-1 in der Tailscale-Policy einen Tag bekommen und nur zu Loki dürfen. Eigene Geräte, auch `monitoring`, dürfen weiter zu slipways-1:

```json
"tagOwners": { "tag:slipways": ["autogroup:admin"] },
"hosts":     { "monitoring": "100.x.y.z" },
"grants": [
  { "src": ["autogroup:member"], "dst": ["*"],          "ip": ["*"] },
  { "src": ["tag:slipways"],     "dst": ["monitoring"], "ip": ["tcp:3100"] }
]
```

Den Tag vergibt `tailscale up --advertise-tags=tag:slipways` auf slipways-1, oder ein Auth-Key mit Tag. Gilt das Gleiche für die Test-Stage im LXC, kommt dort hinzu: Der LXC hängt über `vmbr0` direkt im Heimnetz. Davon trennt ihn nur ein eigenes VLAN oder eine Firewall-Regel in Proxmox.

## Moderation

Vorschläge aus Web und Apps (siehe [README](../README.md#nutzerbeiträge)) gibt die Moderationsseite frei. Sie hat kein Login: Die API beantwortet `/moderation` und `/api/moderation/…` nur auf Port 9465, und `deploy/compose.lxc.yaml` gibt den nur auf `127.0.0.1:9465` frei (`SLIPWAYS_MODERATION_PORT`). Über slipways.de ist sie nicht erreichbar, auch nicht aus dem Tailnet, denn die Domain zeigt auf die öffentliche Adresse. Ins Tailnet bringt sie `tailscale serve`, wie `/metrics`:

```bash
# auf slipways-1 als root; die Einstellung überlebt Neustarts
tailscale serve --bg --https=9465 http://127.0.0.1:9465
tailscale serve status
```

Danach von jedem Gerät im Tailnet (auch vom Handy mit der Tailscale-App): `https://slipways-1.<tailnet>.ts.net:9465/moderation`. Wer im Tailnet ist, kann moderieren; soll das nur für eigene Geräte gelten, in der Tailscale-Policy den Zugriff auf `tcp:9465` von `slipways-1` beschränken.

Neue Vorschläge zeigt der Messwert `slipways_suggestions_pending`; als Push aufs Handy siehe [Benachrichtigungen](#benachrichtigungen).

Lokal mit `docker compose up` liegt die Moderation unter `http://localhost:9465/moderation`.

## Benachrichtigungen

Bei neuen Vorschlägen schickt die API eine Push-Nachricht über einen eigenen [ntfy](https://ntfy.sh)-Server auf slipways-1 (`deploy/compose.ntfy.yaml`). Wie Moderation und `/metrics` ist er nur lokal auf `127.0.0.1:2586` erreichbar und kommt per `tailscale serve` ins Tailnet; das Handy braucht also die Tailscale-App. Die Nachrichten enthalten nur Anzahlen, keine Inhalte der Vorschläge.

1. Einen zufälligen Topic-Namen ausdenken, z. B. `slipways-$(openssl rand -hex 8)`. Wer den Namen kennt und im Tailnet ist, kann mitlesen.
2. In `/opt/slipways/.env` ergänzen:

   ```bash
   COMPOSE_FILE=compose.yaml:deploy/compose.lxc.yaml:deploy/compose.https.yaml:deploy/compose.ntfy.yaml
   SLIPWAYS_NTFY_BASE_URL=https://slipways-1.<tailnet>.ts.net:2586
   SLIPWAYS_NTFY_TOPIC=slipways-…
   SLIPWAYS_MODERATION_URL=https://slipways-1.<tailnet>.ts.net:9465/moderation
   # optional: gesammelt statt sofort, z. B. montags und donnerstags um 18 Uhr
   # SLIPWAYS_NOTIFY_DAYS=Mo,Do
   # SLIPWAYS_NOTIFY_TIMES=18:00
   ```

3. Starten und ins Tailnet bringen:

   ```bash
   # auf slipways-1 als root
   cd /opt/slipways && docker compose up -d
   tailscale serve --bg --https=2586 http://127.0.0.1:2586
   ```

4. In der ntfy-App (iOS, Android) unter „Abonnieren“ den Topic-Namen eintragen, „Anderen Server verwenden“ wählen und `https://slipways-1.<tailnet>.ts.net:2586` angeben. Ein Test: `curl -d "Test" http://127.0.0.1:2586/<topic>` auf slipways-1.

iPhones bekommen Push nur über Apple; dafür reicht der eigene Server bei ntfy.sh eine Anfrage weiter, die nur einen Hash des Topics und die ID der Nachricht enthält (`NTFY_UPSTREAM_BASE_URL`). Den Inhalt holt die App danach vom eigenen Server. Android verbindet sich direkt.

Ohne eigenen Server geht es auch mit ntfy.sh: dann nur `SLIPWAYS_NTFY_TOPIC` setzen und `deploy/compose.ntfy.yaml` weglassen. Ob die Nachrichten rausgehen, zeigen das Log („Notified about … new suggestions“) und `slipways_notifications_sent_total`.

## Auto-Update

Der systemd-Timer `slipways-update.timer` startet alle 5 Minuten `lxc/slipways-update.sh`. Gibt es auf dem Branch neue Commits, holt das Skript sie, baut die Images neu (mit frischen Basis-Images), sichert die Datenbank lokal nach `/var/lib/slipways/backups/pre-update.db` und startet den Stack neu. Die API spielt Migrationen beim Start selbst ein.

- Lässt sich der neue Stand nicht bauen, läuft die bisherige Version weiter, der Checkout bleibt auf ihr, und der kaputte Commit wird nicht alle 5 Minuten neu versucht, sondern erst der nächste.
- Antwortet das Backend nicht (z. B. nach einem abgebrochenen Update), zieht der nächste Lauf den Stack wieder hoch.
- Der letzte Stand steht in `/var/lib/slipways/update.json`.

```bash
pct exec <CTID> -- journalctl -u slipways-update -n 50                  # Log
pct exec <CTID> -- /opt/slipways/deploy/lxc/slipways-update.sh --force  # sofort, auch ohne neue Commits
```

## Nextcloud-Sicherung

Nach dem Vorbild von fil-o-mat: jede Nacht um 3 Uhr eine Kopie der SQLite-Datenbank (`sqlite3 .backup`, konsistent auch während die API läuft), per WebDAV in die Nextcloud. Jede Sicherung schreibt zwei Dateien: eine pro Wochentag (`slipways-mon.db`, die letzten sieben Tage) und eine pro Monat (`slipways-2026-10.db`, der letzte Stand jedes Monats). In der Nextcloud wird nichts gelöscht. Der Sicherungsordner wird angelegt, der darüber muss existieren.

Das Passwort ist ein App-Passwort (Nextcloud: Einstellungen → Sicherheit → Geräte & Sitzungen), das sich einzeln widerrufen lässt. Schlägt eine Sicherung fehl, versucht systemd es nach 30 Minuten noch einmal. Das Ergebnis steht in `/var/lib/slipways/backup.json`.

Nachträglich einschalten: `lxc/setup-nextcloud.sh` fragt Nextcloud-Adresse, Benutzer, App-Passwort und Ordner ab, prüft die Anmeldung, legt den Ordner an, trägt alles in die `.env` ein und startet eine erste Sicherung.

```bash
bash /opt/slipways/deploy/lxc/setup-nextcloud.sh                     # direkt auf dem Server
pct exec <CTID> -- bash /opt/slipways/deploy/lxc/setup-nextcloud.sh  # im LXC, von der Proxmox-Shell
```

Oder von Hand:

```bash
pct exec <CTID> -- bash -c 'cat >> /opt/slipways/.env <<ENV
NEXTCLOUD_WEBDAV_URL=https://cloud.example.de/remote.php/dav/files/<benutzer>/Backups/slipways
NEXTCLOUD_USERNAME=<benutzer>
NEXTCLOUD_APP_PASSWORD=<app-passwort>
ENV
/opt/slipways/deploy/lxc/slipways-backup.sh && cat /var/lib/slipways/backup.json'
```

Von Hand sichern: `pct exec <CTID> -- systemctl start slipways-backup`, Log: `journalctl -u slipways-backup`.

### Zurückspielen

Die Datei aus der Nextcloud in den LXC kopieren (z. B. nach `/root/slipways.db`), dann:

```bash
cd /opt/slipways
docker compose stop api
data="$(docker volume inspect --format '{{ .Mountpoint }}' slipways_api-data)"
rm -f "$data"/slipways.db-*
install -o 1654 -g 1654 -m 644 /root/slipways.db "$data/slipways.db"   # 1654 = Benutzer der API im Container
docker compose start api
```

Die Anmeldungen hängen an den Data-Protection-Schlüsseln im Volume `slipways_api-keys`; die sind nicht in der Sicherung, nach einem Umzug müssen sich Benutzer also neu anmelden.


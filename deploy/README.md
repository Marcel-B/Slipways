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

## Tailscale

Tailscale läuft direkt im LXC, kein Sidecar-Container. `create-lxc.sh` reicht dafür `/dev/net/tun` durch und ruft `proxmox/add-tailscale.sh` auf. Für einen bestehenden Container, auch nachträglich:

```bash
# in der Proxmox-Shell als root
CTID=<CTID> TS_AUTHKEY=tskey-auth-… bash add-tailscale.sh
```

Mehrmals aufrufen schadet nicht. Tailscale startet ohne `--accept-routes`, damit ein Subnet-Router im Tailnet das LAN nicht umleitet.

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

Nachträglich einschalten:

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


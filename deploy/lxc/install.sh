#!/usr/bin/env bash
# Richtet im LXC Auto-Update und Nextcloud-Sicherung als systemd-Timer ein. Mehrmals aufrufen schadet nicht.
#   bash /opt/slipways/deploy/lxc/install.sh
# --units-only erneuert nur die Units (ruft slipways-update.sh nach jedem Update auf).
#
# Einstellungen über Umgebungsvariablen:
#   SLIPWAYS_UPDATE_INTERVAL=5min   wie oft auf neue Commits geprüft wird
#   SLIPWAYS_BACKUP_AT=03:00        Uhrzeit der nächtlichen Sicherung
set -euo pipefail

REPO="$(cd "$(dirname "$(readlink -f "$0")")/../.." && pwd)"
INTERVAL="${SLIPWAYS_UPDATE_INTERVAL:-5min}"
BACKUP_AT="${SLIPWAYS_BACKUP_AT:-03:00}"

[[ $EUID -eq 0 ]] || { echo "Bitte als root ausführen." >&2; exit 1; }

install_units() {
  chmod +x "$REPO/deploy/lxc/"*.sh

  cat > /etc/systemd/system/slipways-update.service <<UNIT
[Unit]
Description=Slipways: neue Commits einspielen
After=docker.service network-online.target
Wants=network-online.target

[Service]
Type=oneshot
ExecStart=$REPO/deploy/lxc/slipways-update.sh
TimeoutStartSec=45min
UNIT

  cat > /etc/systemd/system/slipways-update.timer <<UNIT
[Unit]
Description=Slipways: regelmäßig auf neue Commits prüfen

[Timer]
OnBootSec=3min
OnUnitInactiveSec=$INTERVAL
RandomizedDelaySec=30

[Install]
WantedBy=timers.target
UNIT

  # Eine fehlgeschlagene Sicherung wird nach 30 Minuten wiederholt, höchstens fünfmal am Tag
  cat > /etc/systemd/system/slipways-backup.service <<UNIT
[Unit]
Description=Slipways: Datenbank in die Nextcloud sichern
After=docker.service network-online.target
Wants=network-online.target
StartLimitIntervalSec=1d
StartLimitBurst=5

[Service]
Type=oneshot
ExecStart=$REPO/deploy/lxc/slipways-backup.sh
Restart=on-failure
RestartSec=30min
TimeoutStartSec=30min
UNIT

  cat > /etc/systemd/system/slipways-backup.timer <<UNIT
[Unit]
Description=Slipways: nächtliche Sicherung

[Timer]
OnCalendar=*-*-* $BACKUP_AT
Persistent=true
RandomizedDelaySec=5min

[Install]
WantedBy=timers.target
UNIT

  systemctl daemon-reload
}

install_units
[[ "${1:-}" == --units-only ]] && exit 0

command -v jq >/dev/null && command -v sqlite3 >/dev/null || { apt-get update -qq && apt-get install -y -qq jq sqlite3 >/dev/null; }
mkdir -p /var/lib/slipways/backups
chmod 700 /var/lib/slipways/backups

systemctl enable --now slipways-update.timer slipways-backup.timer
echo "Auto-Update (alle $INTERVAL) und Nextcloud-Sicherung (täglich $BACKUP_AT) eingerichtet:"
systemctl list-timers 'slipways-*' --no-pager

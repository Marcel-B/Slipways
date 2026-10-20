#!/usr/bin/env bash
# Legt auf einem Proxmox-Host einen Debian-LXC an und startet darin Slipways (API, PostgreSQL, Web) mit Docker,
# dazu Tailscale, die nächtliche Nextcloud-Sicherung und das Auto-Update vom Branch.
#
# Aufruf in der Proxmox-Shell (als root):
#   bash create-lxc.sh
# Einstellungen über Umgebungsvariablen, z. B.:
#   CTID=160 IP=192.168.1.60/24 GATEWAY=192.168.1.1 TS_AUTHKEY=tskey-auth-… bash create-lxc.sh
set -euo pipefail

CTID="${CTID:-$(pvesh get /cluster/nextid)}"
CT_HOSTNAME="${CT_HOSTNAME:-slipways}"
STORAGE="${STORAGE:-local-lvm}"            # Speicher für die Container-Disk
TEMPLATE_STORAGE="${TEMPLATE_STORAGE:-local}"
BRIDGE="${BRIDGE:-vmbr0}"
IP="${IP:-dhcp}"                           # oder z. B. 192.168.1.60/24
GATEWAY="${GATEWAY:-}"                     # nur bei fester IP nötig
DISK_GB="${DISK_GB:-16}"
MEMORY_MB="${MEMORY_MB:-2048}"             # .NET- und Vite-Build brauchen beim Update etwas Luft
CORES="${CORES:-2}"
REPO_URL="${REPO_URL:-https://github.com/Marcel-B/Slipways.git}"
BRANCH="${BRANCH:-master}"
GITHUB_TOKEN="${GITHUB_TOKEN:-}"           # nur bei privatem Repository (Token mit Lesezugriff)
PORT="${PORT:-80}"                         # Port der Web-Oberfläche, die API liegt darunter unter /api
UPDATE_INTERVAL="${UPDATE_INTERVAL:-5min}"
BACKUP_AT="${BACKUP_AT:-03:00}"
# optional: nächtliche Sicherung in die Nextcloud
NEXTCLOUD_WEBDAV_URL="${NEXTCLOUD_WEBDAV_URL:-}"
NEXTCLOUD_USERNAME="${NEXTCLOUD_USERNAME:-}"
NEXTCLOUD_APP_PASSWORD="${NEXTCLOUD_APP_PASSWORD:-}"
TAILSCALE="${TAILSCALE:-1}"                # 0: ohne Tailscale
TS_AUTHKEY="${TS_AUTHKEY:-}"               # optional, sonst zeigt Tailscale einen Login-Link an
TS_SERVE="${TS_SERVE:-0}"                  # 1: zusätzlich HTTPS über "tailscale serve"

info() { echo -e "\e[1;34m==>\e[0m $*"; }
fail() { echo -e "\e[1;31mFehler:\e[0m $*" >&2; exit 1; }

command -v pct >/dev/null || fail "Das Skript muss auf dem Proxmox-Host laufen (pct fehlt)."
[[ $EUID -eq 0 ]] || fail "Bitte als root ausführen."
pct status "$CTID" &>/dev/null && fail "Container $CTID existiert bereits. Andere CTID wählen, z. B. CTID=161."

info "Debian-Vorlage suchen"
pveam update >/dev/null
# Die Liste enthält Vorlagen für mehrere Architekturen; eine fremde startet nicht ("Exec format error")
ARCH="$(dpkg --print-architecture)"
TEMPLATE="${TEMPLATE:-$(pveam available --section system | awk -v arch="$ARCH" '$2 ~ "^debian-1[23]-standard_.*_" arch "\\." {print $2}' | sort -V | tail -n1)}"
[[ -n "$TEMPLATE" ]] || fail "Keine debian-standard-Vorlage für $ARCH gefunden."
if ! pveam list "$TEMPLATE_STORAGE" | grep -q "$TEMPLATE"; then
  info "Vorlage $TEMPLATE herunterladen"
  pveam download "$TEMPLATE_STORAGE" "$TEMPLATE"
fi

NET="name=eth0,bridge=${BRIDGE},ip=${IP}"
[[ -n "$GATEWAY" ]] && NET="${NET},gw=${GATEWAY}"

info "Container $CTID ($CT_HOSTNAME) anlegen"
# nesting und keyctl braucht Docker in einem unprivilegierten Container
pct create "$CTID" "${TEMPLATE_STORAGE}:vztmpl/${TEMPLATE}" \
  --hostname "$CT_HOSTNAME" \
  --arch "$ARCH" \
  --cores "$CORES" \
  --memory "$MEMORY_MB" \
  --swap 1024 \
  --rootfs "${STORAGE}:${DISK_GB}" \
  --net0 "$NET" \
  --unprivileged 1 \
  --features nesting=1,keyctl=1 \
  --onboot 1

if [[ "$TAILSCALE" == 1 ]]; then
  # Tailscale braucht /dev/net/tun; vor dem ersten Start eingetragen, spart das einen Neustart
  cat >> "/etc/pve/lxc/${CTID}.conf" <<CONF
lxc.cgroup2.devices.allow: c 10:200 rwm
lxc.mount.entry: /dev/net/tun dev/net/tun none bind,create=file
CONF
fi
pct start "$CTID"

info "Auf Netzwerk warten"
for _ in $(seq 1 30); do
  pct exec "$CTID" -- getent hosts deb.debian.org &>/dev/null && break
  sleep 2
done
pct exec "$CTID" -- getent hosts deb.debian.org &>/dev/null || fail "Container $CTID hat kein Netzwerk."

info "Docker installieren und Slipways starten (der erste Build dauert ein paar Minuten)"
# shellcheck disable=SC2016 # der Befehl läuft im Container, dort werden die Variablen ersetzt
pct exec "$CTID" -- env \
  REPO_URL="$REPO_URL" BRANCH="$BRANCH" GITHUB_TOKEN="$GITHUB_TOKEN" PORT="$PORT" \
  UPDATE_INTERVAL="$UPDATE_INTERVAL" BACKUP_AT="$BACKUP_AT" \
  NEXTCLOUD_WEBDAV_URL="$NEXTCLOUD_WEBDAV_URL" NEXTCLOUD_USERNAME="$NEXTCLOUD_USERNAME" \
  NEXTCLOUD_APP_PASSWORD="$NEXTCLOUD_APP_PASSWORD" \
  bash -euo pipefail -c '
    export DEBIAN_FRONTEND=noninteractive
    apt-get update -qq
    apt-get install -y -qq ca-certificates curl git jq openssl >/dev/null
    curl -fsSL https://get.docker.com | sh >/dev/null

    if [[ -n "$GITHUB_TOKEN" ]]; then
      # Der Updater muss auch später holen können; das Token liegt nur lesbar für root, nicht in der Remote-URL
      git config --global credential.helper store
      umask 077
      echo "https://x-access-token:${GITHUB_TOKEN}@github.com" > /root/.git-credentials
      umask 022
    fi
    git clone --quiet --branch "$BRANCH" "$REPO_URL" /opt/slipways

    umask 077
    cat > /opt/slipways/.env <<ENV
COMPOSE_FILE=compose.yaml:deploy/compose.lxc.yaml
POSTGRES_PASSWORD=$(openssl rand -hex 24)
SLIPWAYS_PORT=$PORT
SLIPWAYS_API_PORT=5080
NEXTCLOUD_WEBDAV_URL=$NEXTCLOUD_WEBDAV_URL
NEXTCLOUD_USERNAME=$NEXTCLOUD_USERNAME
NEXTCLOUD_APP_PASSWORD=$NEXTCLOUD_APP_PASSWORD
ENV
    umask 022

    cd /opt/slipways
    docker compose up -d --build --wait

    SLIPWAYS_UPDATE_INTERVAL="$UPDATE_INTERVAL" SLIPWAYS_BACKUP_AT="$BACKUP_AT" bash deploy/lxc/install.sh >/dev/null

    for _ in $(seq 1 60); do
      curl -fs http://127.0.0.1:5080/health >/dev/null && exit 0
      sleep 2
    done
    docker compose logs --tail 50
    exit 1
  ' || fail "Slipways startet nicht, Logs stehen oben."

if [[ "$TAILSCALE" == 1 ]]; then
  # Das Skript aus dem gerade geklonten Stand nehmen, damit create-lxc.sh allein auf dem Host reicht
  TS_SCRIPT="$(mktemp)"
  pct exec "$CTID" -- cat /opt/slipways/deploy/proxmox/add-tailscale.sh > "$TS_SCRIPT"
  CTID="$CTID" TS_AUTHKEY="$TS_AUTHKEY" TS_HOSTNAME="$CT_HOSTNAME" TS_SERVE="$TS_SERVE" bash "$TS_SCRIPT" \
    || echo "Tailscale ließ sich nicht einrichten; später mit add-tailscale.sh nachholen."
  rm -f "$TS_SCRIPT"
fi

if [[ -n "$NEXTCLOUD_WEBDAV_URL" ]]; then
  info "Erste Sicherung in die Nextcloud"
  pct exec "$CTID" -- /opt/slipways/deploy/lxc/slipways-backup.sh \
    || echo "Sicherung fehlgeschlagen, Details: pct exec $CTID -- cat /var/lib/slipways/backup.json"
fi

CT_IP="$(pct exec "$CTID" -- hostname -I | awk '{print $1}')"
SUFFIX=""
[[ "$PORT" != 80 ]] && SUFFIX=":$PORT"
info "Fertig"
echo "  Container:  $CTID ($CT_HOSTNAME)"
echo "  Slipways:   http://${CT_IP}${SUFFIX}   (API unter /api)"
if [[ -n "$NEXTCLOUD_WEBDAV_URL" ]]; then
  echo "  Sicherung:  täglich um $BACKUP_AT nach $NEXTCLOUD_WEBDAV_URL"
else
  echo "  Sicherung:  aus (NEXTCLOUD_* in /opt/slipways/.env eintragen, siehe deploy/README.md)"
fi
echo "  Update:     alle $UPDATE_INTERVAL von $BRANCH, Log: pct exec $CTID -- journalctl -u slipways-update -n 50"

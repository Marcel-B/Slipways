#!/usr/bin/env bash
# Richtet die nächtliche Nextcloud-Sicherung ein: fragt die Zugangsdaten ab, prüft sie, schreibt sie in die .env
# im Repo-Root und startet eine erste Sicherung. Mehrmals aufrufen ersetzt die bisherigen Werte.
#   bash /opt/slipways/deploy/lxc/setup-nextcloud.sh
#
# Ohne Rückfragen, z. B. aus einem anderen Skript:
#   NEXTCLOUD_URL=https://cloud.example.de NEXTCLOUD_USERNAME=<benutzer> NEXTCLOUD_APP_PASSWORD=<app-passwort> \
#   NEXTCLOUD_FOLDER=Backups/slipways bash setup-nextcloud.sh
set -euo pipefail

REPO="$(cd "$(dirname "$(readlink -f "$0")")/../.." && pwd)"
ENV_FILE="$REPO/.env"
STATE_DIR="${SLIPWAYS_STATE_DIR:-/var/lib/slipways}"

[[ $EUID -eq 0 ]] || { echo "Bitte als root ausführen." >&2; exit 1; }
command -v curl >/dev/null || { echo "curl fehlt: apt install curl" >&2; exit 1; }

ask() {
  local var="$1" prompt="$2" default="${3:-}" secret="${4:-}" value="${!1:-}"
  while [[ -z "$value" ]]; do
    if [[ -n "$secret" ]]; then
      read -rsp "$prompt: " value; echo
    else
      read -rp "$prompt${default:+ [$default]}: " value
      value="${value:-$default}"
    fi
  done
  printf -v "$var" '%s' "$value"
}

ask NEXTCLOUD_URL "Nextcloud-Adresse (z. B. https://cloud.example.de)"
ask NEXTCLOUD_USERNAME "Benutzername"
ask NEXTCLOUD_APP_PASSWORD "App-Passwort (Einstellungen → Sicherheit → Geräte & Sitzungen)" "" secret
ask NEXTCLOUD_FOLDER "Ordner in der Nextcloud" "Backups/slipways"

NEXTCLOUD_URL="${NEXTCLOUD_URL%/}"
[[ "$NEXTCLOUD_URL" == http*://* ]] || NEXTCLOUD_URL="https://$NEXTCLOUD_URL"
NEXTCLOUD_FOLDER="${NEXTCLOUD_FOLDER#/}"; NEXTCLOUD_FOLDER="${NEXTCLOUD_FOLDER%/}"
USER_URL="$NEXTCLOUD_URL/remote.php/dav/files/$NEXTCLOUD_USERNAME"
WEBDAV_URL="$USER_URL/$NEXTCLOUD_FOLDER"

for value in "$WEBDAV_URL" "$NEXTCLOUD_USERNAME" "$NEXTCLOUD_APP_PASSWORD"; do
  [[ "$value" != *"'"* && "$value" != *$'\n'* ]] || { echo "Hochkommas und Zeilenumbrüche gehen in der .env nicht." >&2; exit 1; }
done

# Zugangsdaten über stdin an curl, damit sie nicht in der Prozessliste stehen
webdav() {
  printf 'user = "%s:%s"\n' "${NEXTCLOUD_USERNAME//\"/\\\"}" "${NEXTCLOUD_APP_PASSWORD//\"/\\\"}" \
    | curl --silent --show-error --config - --max-time 60 --output /dev/null --write-out '%{http_code}' "$@" || true
}

echo "Prüfe die Anmeldung bei $NEXTCLOUD_URL …"
status="$(webdav --request PROPFIND --header 'Depth: 0' "$USER_URL/")"
case "$status" in
  207) ;;
  401) echo "Nextcloud lehnt Benutzer oder App-Passwort ab (401)." >&2; exit 1 ;;
  404) echo "Benutzer $NEXTCLOUD_USERNAME nicht gefunden (404). Der Benutzername steht in Nextcloud unter Dateien → Einstellungen → WebDAV." >&2; exit 1 ;;
  000) echo "Nextcloud unter $NEXTCLOUD_URL nicht erreichbar." >&2; exit 1 ;;
  *) echo "Unerwartete Antwort von der Nextcloud (HTTP $status)." >&2; exit 1 ;;
esac

# Ordner Stück für Stück anlegen; 405 heißt, es gibt ihn schon
path="$USER_URL"
IFS=/ read -ra parts <<< "$NEXTCLOUD_FOLDER"
for part in "${parts[@]}"; do
  [[ -n "$part" ]] || continue
  path="$path/$part"
  status="$(webdav --request MKCOL "$path/")"
  [[ "$status" == 201 || "$status" == 405 ]] || { echo "Ordner ${path#"$USER_URL/"} lässt sich nicht anlegen (HTTP $status)." >&2; exit 1; }
done

# Alte Werte ersetzen, die übrigen Einstellungen bleiben
touch "$ENV_FILE"
chmod 600 "$ENV_FILE"
sed -i '/^NEXTCLOUD_WEBDAV_URL=/d; /^NEXTCLOUD_USERNAME=/d; /^NEXTCLOUD_APP_PASSWORD=/d' "$ENV_FILE"
[[ ! -s "$ENV_FILE" || "$(tail -c1 "$ENV_FILE")" == "" ]] || echo >> "$ENV_FILE"
cat >> "$ENV_FILE" <<ENV
NEXTCLOUD_WEBDAV_URL='$WEBDAV_URL'
NEXTCLOUD_USERNAME='$NEXTCLOUD_USERNAME'
NEXTCLOUD_APP_PASSWORD='$NEXTCLOUD_APP_PASSWORD'
ENV
echo "Zugangsdaten in $ENV_FILE eingetragen."

systemctl is-enabled --quiet slipways-backup.timer 2>/dev/null \
  || echo "Hinweis: slipways-backup.timer ist nicht aktiv, für die nächtliche Sicherung erst: bash $REPO/deploy/lxc/install.sh"

echo "Erste Sicherung …"
if "$REPO/deploy/lxc/slipways-backup.sh"; then
  echo "Fertig. Ergebnis in $STATE_DIR/backup.json:"
  cat "$STATE_DIR/backup.json"
else
  echo "Sicherung fehlgeschlagen:" >&2
  cat "$STATE_DIR/backup.json" >&2 2>/dev/null || true
  exit 1
fi

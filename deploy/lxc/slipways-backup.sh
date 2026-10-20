#!/usr/bin/env bash
# Sichert die SQLite-Datenbank über WebDAV in die Nextcloud, nach dem Vorbild von fil-o-mat.
# Läuft im LXC als root, jede Nacht gestartet vom systemd-Timer slipways-backup.timer (siehe install.sh):
#   slipways-backup.sh            sichern und hochladen
#   slipways-backup.sh local      nur lokal sichern (vor einem Update), nichts hochladen
#
# Jede Sicherung schreibt zwei Dateien: eine pro Wochentag (slipways-mon.db, die letzten sieben Tage) und eine pro
# Monat (slipways-2026-10.db, der letzte Stand jedes Monats). In der Nextcloud wird nichts gelöscht.
# Zugangsdaten stehen in der .env im Repo-Root: NEXTCLOUD_WEBDAV_URL, NEXTCLOUD_USERNAME, NEXTCLOUD_APP_PASSWORD.
set -euo pipefail

REPO="${SLIPWAYS_REPO:-$(cd "$(dirname "$(readlink -f "$0")")/../.." && pwd)}"
STATE_DIR="${SLIPWAYS_STATE_DIR:-/var/lib/slipways}"
LOCAL_DIR="$STATE_DIR/backups"

log() { echo "$(date '+%F %T') $*"; }

# Wert aus der .env, ohne sie als Shell-Code auszuführen
env_value() {
  local value
  value="$(sed -n "s/^$1=//p" "$REPO/.env" 2>/dev/null | tail -n1)"
  value="${value%\"}"; value="${value#\"}"; value="${value%\'}"; value="${value#\'}"
  printf '%s' "$value"
}

write_status() {
  mkdir -p "$STATE_DIR"
  jq -n --arg at "$(date -Iseconds)" --argjson ok "$1" --arg files "$2" --arg error "$3" \
    '{at: $at, success: $ok, files: ($files | split(" ") | map(select(. != ""))), error: (if $error == "" then null else $error end)}' \
    > "$STATE_DIR/backup.json.tmp"
  mv "$STATE_DIR/backup.json.tmp" "$STATE_DIR/backup.json"
}

dump() {
  # Die Datenbank liegt im Volume der API; .backup kopiert sie konsistent, auch während die API schreibt
  local volume db
  volume="$(docker volume inspect --format '{{ .Mountpoint }}' slipways_api-data)" \
    || { echo "Volume slipways_api-data fehlt" >&2; return 1; }
  db="$volume/slipways.db"
  [[ -f "$db" ]] || { echo "$db fehlt" >&2; return 1; }
  rm -f "$1"
  sqlite3 "$db" ".backup '$1'"
  [[ -s "$1" ]] || { echo "Sicherung ist leer" >&2; return 1; }
}

# curl liest die Zugangsdaten aus einer Konfigurationsdatei auf stdin, damit sie nicht in der Prozessliste stehen
webdav() {
  local user password
  user="$(env_value NEXTCLOUD_USERNAME)"
  password="$(env_value NEXTCLOUD_APP_PASSWORD)"
  printf 'user = "%s:%s"\n' "${user//\"/\\\"}" "${password//\"/\\\"}" \
    | curl --silent --show-error --config - --max-time 300 --retry 3 --retry-delay 10 "$@"
}

main() {
  mkdir -p "$LOCAL_DIR"
  chmod 700 "$LOCAL_DIR"

  if [[ "${1:-}" == local ]]; then
    local file="$LOCAL_DIR/pre-update.db"
    dump "$file.tmp" && mv "$file.tmp" "$file"
    log "Lokale Sicherung: $file ($(stat -c %s "$file") Bytes)"
    return 0
  fi

  local url
  url="$(env_value NEXTCLOUD_WEBDAV_URL)"
  url="${url%/}"
  if [[ -z "$url" || -z "$(env_value NEXTCLOUD_USERNAME)" || -z "$(env_value NEXTCLOUD_APP_PASSWORD)" ]]; then
    log "Nextcloud-Sicherung aus: NEXTCLOUD_WEBDAV_URL, NEXTCLOUD_USERNAME und NEXTCLOUD_APP_PASSWORD in $REPO/.env fehlen"
    write_status false "" "Keine Nextcloud eingerichtet"
    return 0
  fi

  local file="$LOCAL_DIR/latest.db"
  if ! dump "$file.tmp"; then
    write_status false "" "Sicherung der Datenbank fehlgeschlagen"
    return 1
  fi
  mv "$file.tmp" "$file"

  # Ordner anlegen; 405 heißt, es gibt ihn schon
  local status
  status="$(webdav --request MKCOL --output /dev/null --write-out '%{http_code}' "$url/" || true)"
  case "$status" in
    201|405) ;;
    401) write_status false "" "Nextcloud lehnt Benutzer oder App-Passwort ab (401)"; log "MKCOL: 401"; return 1 ;;
    409) write_status false "" "Der Ordner über dem Sicherungsordner fehlt in der Nextcloud (409)"; log "MKCOL: 409"; return 1 ;;
    *) write_status false "" "Sicherungsordner lässt sich nicht anlegen (HTTP $status)"; log "MKCOL: $status"; return 1 ;;
  esac

  local names=("slipways-$(LC_ALL=C date +%a | tr '[:upper:]' '[:lower:]').db" "slipways-$(date +%Y-%m).db")
  local name
  for name in "${names[@]}"; do
    status="$(webdav --upload-file "$file" --header 'Content-Type: application/octet-stream' \
      --output /dev/null --write-out '%{http_code}' "$url/$name" || true)"
    if [[ "$status" != 2?? ]]; then
      write_status false "" "$name lässt sich nicht hochladen (HTTP $status)"
      log "Upload $name: HTTP $status"
      return 1
    fi
  done

  write_status true "${names[*]}" ""
  log "Nextcloud-Sicherung fertig: ${names[*]}, $(stat -c %s "$file") Bytes"
}

main "$@"

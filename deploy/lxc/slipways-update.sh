#!/usr/bin/env bash
# Spielt neue Commits des gebauten Branches automatisch ein.
# Läuft im LXC als root, alle paar Minuten gestartet vom systemd-Timer slipways-update.timer (siehe install.sh):
#   slipways-update.sh           neue Commits holen, Images bauen, Stack neu starten
#   slipways-update.sh --force   auch ohne neue Commits neu bauen (z. B. für neue Basis-Images)
#
# Lässt sich der neue Stand nicht bauen, läuft die bisherige Version weiter, bis ein weiterer Commit kommt.
# Vor jedem Update sichert das Skript die Datenbank lokal nach /var/lib/slipways/backups/pre-update.db,
# weil die API beim Start Migrationen einspielt.
set -euo pipefail

# Alles steckt in main, damit bash das Skript ganz gelesen hat, bevor "git merge" es ersetzt
main() {
  REPO="${SLIPWAYS_REPO:-$(cd "$(dirname "$(readlink -f "$0")")/../.." && pwd)}"
  STATE_DIR="${SLIPWAYS_STATE_DIR:-/var/lib/slipways}"
  BRANCH="$(git -C "$REPO" rev-parse --abbrev-ref HEAD)"
  API_PORT="$(sed -n 's/^SLIPWAYS_API_PORT=//p' "$REPO/.env" 2>/dev/null | tail -n1)"
  API_PORT="${API_PORT:-5080}"

  log() { echo "$(date '+%F %T') $*"; }
  write_status() {
    mkdir -p "$STATE_DIR"
    jq -n --arg at "$(date -Iseconds)" --arg state "$1" --arg commit "$(git -C "$REPO" rev-parse --short HEAD)" \
      --arg branch "$BRANCH" --arg message "$2" \
      '{at: $at, state: $state, branch: $branch, commit: $commit, message: (if $message == "" then null else $message end)}' \
      > "$STATE_DIR/update.json.tmp"
    mv "$STATE_DIR/update.json.tmp" "$STATE_DIR/update.json"
  }
  healthy() {
    local _
    for _ in $(seq 1 90); do
      curl -fs "http://127.0.0.1:$API_PORT/health" >/dev/null && return 0
      sleep 2
    done
    return 1
  }

  git -C "$REPO" fetch --quiet origin "$BRANCH"
  local old new
  old="$(git -C "$REPO" rev-parse HEAD)"
  new="$(git -C "$REPO" rev-parse "origin/$BRANCH")"

  # Läuft der Stack nicht (z. B. nach einem abgebrochenen Update), wieder hochziehen
  if ! curl -fs "http://127.0.0.1:$API_PORT/health" >/dev/null; then
    log "Backend antwortet nicht, Stack wird gestartet"
    (cd "$REPO" && docker compose up -d) || true
  fi

  if [[ "${1:-}" != --force ]]; then
    [[ "$old" == "$new" ]] && return 0
    # Einen Stand, der sich nicht bauen ließ, nicht alle paar Minuten erneut versuchen
    [[ "$new" == "$(cat "$STATE_DIR/failed-commit" 2>/dev/null)" ]] && return 0
  fi

  log "Update $BRANCH: ${old:0:7} -> ${new:0:7}"
  git -C "$REPO" log --oneline "$old..$new" | sed 's/^/  /'

  if ! git -C "$REPO" merge --quiet --ff-only "origin/$BRANCH"; then
    write_status failed "git merge ging nicht (lokale Änderungen im Checkout?)"
    log "git merge ging nicht"
    return 1
  fi

  # Units und Skripte können sich mit dem neuen Stand geändert haben
  bash "$REPO/deploy/lxc/install.sh" --units-only || log "Units nicht erneuert"

  if ! (cd "$REPO" && docker compose build --pull --quiet); then
    # Die alten Images laufen weiter; den Checkout zurücksetzen, damit Stand und laufende Version passen
    git -C "$REPO" reset --quiet --hard "$old"
    mkdir -p "$STATE_DIR"
    echo "$new" > "$STATE_DIR/failed-commit"
    write_status failed "Neuer Stand ${new:0:7} ließ sich nicht bauen, ${old:0:7} läuft weiter"
    log "Build fehlgeschlagen, ${old:0:7} läuft weiter"
    return 1
  fi

  "$REPO/deploy/lxc/slipways-backup.sh" local || log "Lokale Sicherung vor dem Update fehlgeschlagen"

  (cd "$REPO" && docker compose up -d --remove-orphans)

  if healthy; then
    docker image prune -f >/dev/null || true
    rm -f "$STATE_DIR/failed-commit"
    write_status ok ""
    log "Fertig, läuft auf ${new:0:7}"
    return 0
  fi

  write_status failed "Backend antwortet nach dem Update nicht (docker compose logs api); Sicherung: $STATE_DIR/backups/pre-update.db"
  log "Backend antwortet nach dem Update nicht"
  (cd "$REPO" && docker compose logs --tail 50 api) || true
  return 1
}

main "$@"

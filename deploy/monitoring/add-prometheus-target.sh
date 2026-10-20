#!/usr/bin/env bash
# Meldet slipways.de bei Prometheus an. Prüft, ob /metrics über Tailscale erreichbar ist, legt
# /opt/monitoring/prometheus/targets/slipways.yml an und wartet, bis Prometheus das Ziel als "up" führt.
# Mehrmals aufrufen schadet nicht.
#
# Läuft im Monitoring-LXC als root:
#   curl -fsSLO https://raw.githubusercontent.com/Marcel-B/Slipways/master/deploy/monitoring/add-prometheus-target.sh
#   bash add-prometheus-target.sh
# oder aus der Proxmox-Shell:
#   curl -fsSL https://raw.githubusercontent.com/Marcel-B/Slipways/master/deploy/monitoring/add-prometheus-target.sh \
#     | pct exec <CTID> -- bash -s
#
# Einstellungen über Umgebungsvariablen:
#   SLIPWAYS_NODE=slipways-1                Name von slipways.de im Tailnet
#   SLIPWAYS_METRICS_PORT=9464              Port von "tailscale serve" auf slipways-1
#   SLIPWAYS_TARGET=<name>.<tailnet>:9464   statt der beiden oben, falls hier kein Tailscale-CLI läuft
#   MONITORING_DIR=/opt/monitoring
#   PROMETHEUS_URL=http://127.0.0.1:9090
#   FORCE=1                                 Ziel auch anlegen, wenn /metrics gerade nicht erreichbar ist
set -euo pipefail

SLIPWAYS_NODE="${SLIPWAYS_NODE:-slipways-1}"
SLIPWAYS_METRICS_PORT="${SLIPWAYS_METRICS_PORT:-9464}"
MONITORING_DIR="${MONITORING_DIR:-/opt/monitoring}"
PROMETHEUS_URL="${PROMETHEUS_URL:-http://127.0.0.1:9090}"
FORCE="${FORCE:-0}"
TARGETS_DIR="$MONITORING_DIR/prometheus/targets"
FILE="$TARGETS_DIR/slipways.yml"

fail() { echo "Fehler: $*" >&2; exit 1; }

[[ $EUID -eq 0 ]] || fail "Bitte als root ausführen."
for cmd in curl jq; do
  command -v "$cmd" >/dev/null || fail "$cmd fehlt: apt install $cmd"
done
[[ -d "$TARGETS_DIR" ]] || fail "$TARGETS_DIR fehlt. Läuft das Skript im Monitoring-LXC?"

if [[ -z "${SLIPWAYS_TARGET:-}" ]]; then
  command -v tailscale >/dev/null || fail "tailscale fehlt hier; SLIPWAYS_TARGET=<name>.<tailnet>.ts.net:$SLIPWAYS_METRICS_PORT setzen."
  suffix="$(tailscale status --json | jq -r '.MagicDNSSuffix // empty')"
  [[ -n "$suffix" ]] || fail "MagicDNS ist im Tailnet aus; SLIPWAYS_TARGET=… setzen."
  SLIPWAYS_TARGET="$SLIPWAYS_NODE.$suffix:$SLIPWAYS_METRICS_PORT"
fi

echo "Prüfe https://$SLIPWAYS_TARGET/metrics …"
if curl -fsS --max-time 10 "https://$SLIPWAYS_TARGET/metrics" | grep -q '^slipways_job_runs_total'; then
  echo "erreichbar."
elif [[ "$FORCE" == 1 ]]; then
  echo "nicht erreichbar, lege das Ziel trotzdem an (FORCE=1)."
else
  fail "/metrics ist nicht erreichbar. Auf $SLIPWAYS_NODE: läuft 'tailscale serve --bg --https=$SLIPWAYS_METRICS_PORT http://127.0.0.1:9464' (tailscale serve status), und ist das Update mit der Überwachung eingespielt? Mit FORCE=1 wird das Ziel trotzdem angelegt."
fi

content="# Angelegt von Slipways: deploy/monitoring/add-prometheus-target.sh
- targets: [$SLIPWAYS_TARGET]
  labels:
    app: slipways
    __scheme__: https
    __metrics_path__: /metrics"

if [[ -f "$FILE" && "$(cat "$FILE")" == "$content" ]]; then
  echo "$FILE ist schon aktuell."
else
  # Erst vollständig schreiben, dann umbenennen, damit Prometheus nie eine halbe Datei liest.
  printf '%s\n' "$content" > "$FILE.tmp"
  chmod 644 "$FILE.tmp"
  mv "$FILE.tmp" "$FILE"
  echo "$FILE angelegt."
fi

echo "Warte, bis Prometheus das Ziel abfragt (bis zu 2 Minuten) …"
health=""
for _ in $(seq 1 24); do
  health="$(curl -fs --max-time 5 "$PROMETHEUS_URL/api/v1/targets?state=active" \
    | jq -r '[.data.activeTargets[] | select(.labels.app == "slipways")][0] | if . == null then "" else "\(.health) \(.lastError)" end' \
    || true)"
  [[ "$health" == up* ]] && break
  sleep 5
done

case "$health" in
  up*) echo "Fertig: Prometheus führt slipways als up ($PROMETHEUS_URL/targets)." ;;
  "") fail "Prometheus kennt das Ziel noch nicht. Liest es $TARGETS_DIR (file_sd_configs)? $PROMETHEUS_URL/targets" ;;
  *) fail "Prometheus erreicht das Ziel nicht: ${health#* }" ;;
esac

#!/usr/bin/env bash
# Schickt die Logs der API an Loki im Monitoring-LXC. Prüft, ob der API-Container Loki erreicht, erst über den
# Tailscale-Namen, sonst über die 100.x-Adresse. Trägt SLIPWAYS_LOKI_URL und SLIPWAYS_MONITORING_HOST in die .env
# im Repo-Root ein, startet die API neu und wartet, bis die ersten Zeilen angekommen sind. Mehrmals aufrufen ersetzt
# die bisherigen Werte.
#   bash /opt/slipways/deploy/lxc/setup-loki.sh
#
# Einstellungen über Umgebungsvariablen:
#   LOKI_HOST=monitoring        Name des Monitoring-LXC im Tailnet
#   LOKI_PORT=3100
#   MONITORING_HOST=slipways-1  Wert des host-Labels in Loki (Standard: Name dieses Rechners im Tailnet)
set -euo pipefail

REPO="$(cd "$(dirname "$(readlink -f "$0")")/../.." && pwd)"
ENV_FILE="$REPO/.env"
LOKI_HOST="${LOKI_HOST:-monitoring}"
LOKI_PORT="${LOKI_PORT:-3100}"

fail() { echo "Fehler: $*" >&2; exit 1; }
env_value() { sed -n "s/^$1=//p" "$ENV_FILE" 2>/dev/null | tail -n1; }

[[ $EUID -eq 0 ]] || fail "Bitte als root ausführen."
for cmd in docker curl jq tailscale; do
  command -v "$cmd" >/dev/null || fail "$cmd fehlt."
done
cd "$REPO"

API_PORT="$(env_value SLIPWAYS_API_PORT)"; API_PORT="${API_PORT:-5080}"
METRICS_PORT="$(env_value SLIPWAYS_METRICS_PORT)"; METRICS_PORT="${METRICS_PORT:-9464}"
metric() { curl -fs --max-time 5 "http://127.0.0.1:$METRICS_PORT/metrics" | awk -v name="$1" '$1 == name { print $2 }'; }

[[ -n "$(metric slipways_loki_lines_pushed_total)" ]] \
  || fail "Die laufende API liefert auf 127.0.0.1:$METRICS_PORT noch keine Messwerte. Erst das Update mit der Überwachung abwarten (oder: $REPO/deploy/lxc/slipways-update.sh --force)."

MONITORING_HOST="${MONITORING_HOST:-$(tailscale status --json | jq -r '.Self.DNSName | split(".")[0]')}"
[[ -n "$MONITORING_HOST" && "$MONITORING_HOST" != null ]] || fail "Name im Tailnet unbekannt; MONITORING_HOST=… setzen."

# Fragt Loki aus dem API-Container heraus, so wie die API es später tut. Das Image hat kein curl, aber bash.
from_container() {
  docker compose exec -T api timeout 5 bash -c '
    exec 3<>"/dev/tcp/$0/$1" || exit 1
    printf "GET /ready HTTP/1.0\r\nHost: %s\r\n\r\n" "$0" >&3
    read -r status <&3
    [[ "$status" == *" 200 "* ]]' "$1" "$LOKI_PORT" 2>/dev/null
}

echo "Prüfe Loki unter $LOKI_HOST:$LOKI_PORT …"
LOKI_IP="$(tailscale ip -4 "$LOKI_HOST" 2>/dev/null | head -n1 || true)"
[[ -n "$LOKI_IP" ]] || fail "$LOKI_HOST ist im Tailnet nicht bekannt (tailscale ip -4 $LOKI_HOST)."
curl -fs --max-time 5 "http://$LOKI_IP:$LOKI_PORT/ready" >/dev/null \
  || fail "Loki antwortet auch vom Server aus nicht ($LOKI_IP:$LOKI_PORT). Läuft Loki, und lässt die Tailscale-Policy diesen Rechner zu tcp:$LOKI_PORT?"

if from_container "$LOKI_HOST"; then
  LOKI_URL="http://$LOKI_HOST:$LOKI_PORT"
elif from_container "$LOKI_IP"; then
  echo "Der API-Container löst $LOKI_HOST nicht auf (kein MagicDNS im Container), nehme die Adresse $LOKI_IP."
  LOKI_URL="http://$LOKI_IP:$LOKI_PORT"
else
  fail "Der Server erreicht Loki, der API-Container aber nicht, weder unter $LOKI_HOST noch unter $LOKI_IP."
fi

# Alte Werte ersetzen, die übrigen Einstellungen bleiben
touch "$ENV_FILE"
chmod 600 "$ENV_FILE"
sed -i '/^SLIPWAYS_LOKI_URL=/d; /^SLIPWAYS_MONITORING_HOST=/d' "$ENV_FILE"
[[ ! -s "$ENV_FILE" || "$(tail -c1 "$ENV_FILE")" == "" ]] || echo >> "$ENV_FILE"
cat >> "$ENV_FILE" <<ENV
SLIPWAYS_LOKI_URL=$LOKI_URL
SLIPWAYS_MONITORING_HOST=$MONITORING_HOST
ENV
echo "In $ENV_FILE eingetragen: SLIPWAYS_LOKI_URL=$LOKI_URL, SLIPWAYS_MONITORING_HOST=$MONITORING_HOST"

echo "API neu starten (einige Sekunden nicht erreichbar) …"
docker compose up -d api
for _ in $(seq 1 60); do
  curl -fs --max-time 2 "http://127.0.0.1:$API_PORT/health" >/dev/null && break
  sleep 2
done
curl -fs --max-time 2 "http://127.0.0.1:$API_PORT/health" >/dev/null || fail "Die API kommt nicht wieder hoch: docker compose logs api"

# Beim Start schreibt die API einige Zeilen, die nach etwa 2 Sekunden gebündelt rausgehen.
for _ in $(seq 1 15); do
  pushed="$(metric slipways_loki_lines_pushed_total)"
  [[ "${pushed:-0}" != 0 ]] && break
  sleep 2
done
dropped="$(curl -fs --max-time 5 "http://127.0.0.1:$METRICS_PORT/metrics" \
  | awk '$1 == "slipways_loki_lines_dropped_total{reason=\"push_failed\"}" { print $2 }')"

if [[ "${pushed:-0}" != 0 ]]; then
  echo "Fertig: $pushed Zeilen an Loki geschickt. In Grafana: {app=\"slipways\", host=\"$MONITORING_HOST\"}"
else
  fail "Noch keine Zeilen bei Loki angekommen (verworfen: ${dropped:-?}). Log der API: docker compose logs api | grep -i loki"
fi

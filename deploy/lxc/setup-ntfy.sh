#!/usr/bin/env bash
# Richtet die Push-Nachrichten bei neuen Vorschlägen ein: eigener ntfy-Server (deploy/compose.ntfy.yaml), im Tailnet
# per "tailscale serve" auf Port 2586, dazu die Moderation auf 9465. Trägt COMPOSE_FILE, SLIPWAYS_NTFY_* und
# SLIPWAYS_MODERATION_URL in die .env im Repo-Root ein, startet ntfy und die API neu und schickt eine Testnachricht.
# Mehrmals aufrufen behält das Topic und ersetzt die übrigen Werte.
#   bash /opt/slipways/deploy/lxc/setup-ntfy.sh
#
# Einstellungen über Umgebungsvariablen:
#   NOTIFY_DAYS=Mo,Do      gesammelt an diesen Wochentagen (leer: jeden Tag)
#   NOTIFY_TIMES=18:00     gesammelt zu diesen Uhrzeiten; leer: sofort (Standard)
#   NTFY_TOPIC=…           eigenes Topic statt eines zufälligen
set -euo pipefail

REPO="$(cd "$(dirname "$(readlink -f "$0")")/../.." && pwd)"
ENV_FILE="$REPO/.env"
NTFY_PORT=2586
MODERATION_PORT=9465
NTFY_FILE=deploy/compose.ntfy.yaml

fail() { echo "Fehler: $*" >&2; exit 1; }
env_value() { sed -n "s/^$1=//p" "$ENV_FILE" 2>/dev/null | tail -n1; }

[[ $EUID -eq 0 ]] || fail "Bitte als root ausführen."
if ! command -v jq >/dev/null; then
  apt-get update -qq && apt-get install -y -qq jq >/dev/null
fi
for cmd in docker curl jq tailscale openssl; do
  command -v "$cmd" >/dev/null || fail "$cmd fehlt."
done
cd "$REPO"
[[ -f "$NTFY_FILE" ]] || fail "$NTFY_FILE fehlt; erst das Update mit den Benachrichtigungen abwarten (oder: $REPO/deploy/lxc/slipways-update.sh --force)."

API_PORT="$(env_value SLIPWAYS_API_PORT)"; API_PORT="${API_PORT:-5080}"
METRICS_PORT="$(env_value SLIPWAYS_METRICS_PORT)"; METRICS_PORT="${METRICS_PORT:-9464}"
curl -fs --max-time 5 "http://127.0.0.1:$METRICS_PORT/metrics" | grep -q '^slipways_notifications_sent_total ' \
  || fail "Die laufende API kennt die Benachrichtigungen noch nicht. Erst das Update abwarten (oder: $REPO/deploy/lxc/slipways-update.sh --force)."

COMPOSE_FILE="$(env_value COMPOSE_FILE)"
[[ -n "$COMPOSE_FILE" ]] || fail "COMPOSE_FILE fehlt in $ENV_FILE."
[[ ":$COMPOSE_FILE:" == *":$NTFY_FILE:"* ]] || COMPOSE_FILE="$COMPOSE_FILE:$NTFY_FILE"

TAILNET_NAME="$(tailscale status --json | jq -r '.Self.DNSName | rtrimstr(".")')"
[[ -n "$TAILNET_NAME" && "$TAILNET_NAME" != null ]] || fail "Name im Tailnet unbekannt (tailscale status)."
NTFY_URL="https://$TAILNET_NAME:$NTFY_PORT"
MODERATION_URL="https://$TAILNET_NAME:$MODERATION_PORT/moderation"

TOPIC="${NTFY_TOPIC:-$(env_value SLIPWAYS_NTFY_TOPIC)}"
TOPIC="${TOPIC:-slipways-$(openssl rand -hex 8)}"
[[ "$TOPIC" =~ ^[A-Za-z0-9_-]{1,64}$ ]] || fail "Ungültiges Topic '$TOPIC' (nur Buchstaben, Ziffern, - und _)."

# Alte Werte ersetzen, die übrigen Einstellungen bleiben
touch "$ENV_FILE"
chmod 600 "$ENV_FILE"
sed -i '/^COMPOSE_FILE=/d; /^SLIPWAYS_NTFY_BASE_URL=/d; /^SLIPWAYS_NTFY_TOPIC=/d; /^SLIPWAYS_MODERATION_URL=/d; /^SLIPWAYS_NOTIFY_DAYS=/d; /^SLIPWAYS_NOTIFY_TIMES=/d' "$ENV_FILE"
[[ ! -s "$ENV_FILE" || "$(tail -c1 "$ENV_FILE")" == "" ]] || echo >> "$ENV_FILE"
cat >> "$ENV_FILE" <<ENV
COMPOSE_FILE=$COMPOSE_FILE
SLIPWAYS_NTFY_BASE_URL=$NTFY_URL
SLIPWAYS_NTFY_TOPIC=$TOPIC
SLIPWAYS_MODERATION_URL=$MODERATION_URL
SLIPWAYS_NOTIFY_DAYS=${NOTIFY_DAYS:-}
SLIPWAYS_NOTIFY_TIMES=${NOTIFY_TIMES:-}
ENV
echo "In $ENV_FILE eingetragen: Topic $TOPIC, Server $NTFY_URL"

echo "ntfy starten und API neu starten (einige Sekunden nicht erreichbar) …"
docker compose up -d ntfy api
for _ in $(seq 1 30); do
  curl -fs --max-time 2 "http://127.0.0.1:$NTFY_PORT/v1/health" | grep -q '"healthy":true' && break
  sleep 2
done
curl -fs --max-time 2 "http://127.0.0.1:$NTFY_PORT/v1/health" | grep -q '"healthy":true' || fail "ntfy startet nicht: docker compose logs ntfy"
for _ in $(seq 1 60); do
  curl -fs --max-time 2 "http://127.0.0.1:$API_PORT/health" >/dev/null && break
  sleep 2
done
curl -fs --max-time 2 "http://127.0.0.1:$API_PORT/health" >/dev/null || fail "Die API kommt nicht wieder hoch: docker compose logs api"

# Beides ins Tailnet; die Einstellung überlebt Neustarts, doppelt aufrufen schadet nicht.
tailscale serve --bg --https="$NTFY_PORT" "http://127.0.0.1:$NTFY_PORT" >/dev/null
tailscale serve --bg --https="$MODERATION_PORT" "http://127.0.0.1:$MODERATION_PORT" >/dev/null
curl -fs --max-time 15 "$NTFY_URL/v1/health" >/dev/null \
  || echo "Hinweis: $NTFY_URL antwortet noch nicht. Sind MagicDNS und HTTPS-Zertifikate im Tailnet an? (Das erste Zertifikat kann eine Minute dauern.)"

curl -fs --max-time 5 -o /dev/null "http://127.0.0.1:$NTFY_PORT/" \
  -d "{\"topic\":\"$TOPIC\",\"title\":\"Slipways\",\"message\":\"Testnachricht: Die Benachrichtigungen sind eingerichtet.\",\"click\":\"$MODERATION_URL\",\"tags\":[\"anchor\"]}" \
  || fail "Testnachricht ging nicht raus: docker compose logs ntfy"

if [[ -n "${NOTIFY_TIMES:-}" ]]; then
  mode="gesammelt um $NOTIFY_TIMES${NOTIFY_DAYS:+ an $NOTIFY_DAYS}"
else
  mode="sofort (gesammelt z. B. mit NOTIFY_DAYS=Mo,Do NOTIFY_TIMES=18:00 erneut aufrufen)"
fi
cat <<DONE

Fertig. Neue Vorschläge kommen $mode.

Auf dem Handy (Tailscale-App muss verbunden sein):
  1. ntfy-App installieren (App Store / Google Play).
  2. „+“ bzw. „Thema abonnieren“, Thema: $TOPIC
  3. „Anderen Server verwenden“: $NTFY_URL
Kommt die Testnachricht nach dem Abonnieren nicht an, das Skript noch einmal ausführen; es schickt eine neue.
Moderation: $MODERATION_URL
DONE

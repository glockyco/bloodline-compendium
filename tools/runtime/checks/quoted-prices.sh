#!/usr/bin/env bash
# Buys what a waitress offers, one order per conversation, and prints the price she names beside the gold the order
# takes, for the known issue about sellers whose lines name another price. Run inside `nix develop` with the lab
# launched and the waitress's level open, with enough gold.
#
#   quoted-prices.sh <actor class> <menu reply> <order reply>...
#
# A menu reply is the reply that opens the food or the drink menu, such as "What do you have to eat?". Each order
# reply is a part of the reply that orders one thing, such as "steak".
set -euo pipefail
here=$(dirname "$0")
lab() { bun "$here/../lab.mjs" "$@"; }
talk() { lab eval "$here/design-facts.lua" "$1"; }
gold() { lab player '{"values":["Money"]}' | jq -r '.values.Money'; }
actor=$1 menu=$2
shift 2
for order in "$@"; do
  talk "{\"talk\":\"$actor\",\"handler\":\"Force Conversation\"}" >/dev/null
  talk "$(jq -nc --arg reply "$menu" '{reply: $reply}')" >/dev/null
  talk "$(jq -nc --arg reply "$order" '{reply: $reply}')" >/dev/null
  shown=$(talk '{}')
  before=$(gold)
  # The first reply accepts the price.
  talk "$(jq -c '{reply: .replies[0].text.TextBlock}' <<<"$shown")" >/dev/null
  after=$(gold)
  jq -nc --arg order "$order" --argjson shown "$shown" --argjson before "$before" --argjson after "$after" \
    '{order: $order, speaker: $shown.text.SpeakerName, says: $shown.text.NPCText,
      accepts: $shown.replies[0].text.TextBlock, taken: ($before - $after)}'
  talk '{"reply":"Close"}' >/dev/null 2>&1 || true
done

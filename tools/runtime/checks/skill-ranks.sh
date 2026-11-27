#!/usr/bin/env bash
# Casts every skill whose code asks for its discipline's damage passives at a still enemy, once with those passives at
# rank 0 and once with all of them at their highest rank. The next `catalog` reads the latest run named skill-ranks
# and shows a skill's damage passives only where a hit rose by their full bonus. Run inside `nix develop` with the
# lab launched on a new save, after `begin skill-ranks`.
#
#   skill-ranks.sh <build directory> [pattern]
#
# Each skill is cast in a freshly opened level, because what one skill leaves behind hits a later skill's enemy:
# burning ground, and effects on the player such as an armed Quakefall. Only its own two casts share a level, after
# its cooldown. Skills that did not hit the enemy in both casts are cast again from other distances and angles.
# The pattern limits the sweep to the skill classes it matches.
set -euo pipefail
here=$(dirname "$0")
# A call that the game never answers must not stop the sweep.
lab() { timeout 180 bun "$here/../lab.mjs" "$@"; }
build=${1:?Give the build directory}
only=${2:-.}
level=/Game/Cities/HarbingerStronghold/HarbingerStronghold_Improved
# The distance to the enemy and the pitch of the view, tried in this order.
aims=("300 -8" "150 -8" "600 -8" "300 -25" "150 -25")

# Each skill class whose package calls a discipline's damage helper, with that discipline.
skills=$(jq -r '.functions[] | .package as $package | .calls[]?
    | select(startswith("virtual:Get ") and endswith(" Damage Bonuses"))
    | [$package, .[12:-15]] | @tsv' "$build/records/code/index.json" | sort -u)

# The skills of the current run whose two casts both hit the enemy.
measured() {
  local log
  log=$build/runtime/$(jq -r .run "$build/runtime/session.json")/run.jsonl
  [[ -f $log ]] || return 0
  jq -r 'def landed: [(.result.receives[]?.damage), (.result.hits[]?.loss)] | any(. >= 1);
      select(.ok and .args.skill and (.args.action | not)) | [.args.skill, ([.args.ranks[]] | add > 0), landed] | @tsv' \
    "$log" |
    awk -F'\t' '$2 == "false" { zero[$1] = $3 } $2 == "true" && $3 == "true" && zero[$1] == "true" { print $1 }' |
    sort -u
}

cast() { # class school distance pitch
  local class=$1 school=$2 passives ranks args cooldown
  passives=$(jq -c '[.passiveRows[].passives[]
      | select((.description // "") | test("^\\+[0-9.]+% \\w+ damage per rank"))
      | {name: (.name | gsub(" "; "")), ranks}]' "$build/site/disciplines/$(tr '[:upper:]' '[:lower:]' <<<"$school").json")
  args() {
    jq -nc --arg skill "$class" --arg school "$school" --argjson full "$1" --argjson passives "$passives" \
      --argjson distance "$3" --argjson pitch "$4" --argjson extra "$2" \
      '{skill: $skill, school: $school, seconds: 3, distance: $distance, pitch: $pitch,
        ranks: ($passives | map({key: .name, value: (if $full then .ranks else 0 end)}) | from_entries)} + $extra'
  }
  lab eval "$here/placed-pickups.lua" "{\"open\":\"$level\"}" >/dev/null
  # The level is open once the player is back, which the first call that succeeds shows. It also gives the player
  # the skill, which often does not fire when cast in the call that adds it.
  for _ in $(seq 30); do
    sleep 2
    lab eval "$here/skill-damage.lua" "$(args false '{"action":"inspect"}' "$3" "$4")" >/dev/null 2>&1 && break
  done
  sleep 2
  cooldown=$(lab eval "$here/skill-damage.lua" "$(args false '{"home":"here"}' "$3" "$4")" |
    jq -r '(.result // .).final.cooldown // 0 | if . > 60 then 60 else . end | ceil')
  sleep $((cooldown + 1))
  lab eval "$here/skill-damage.lua" "$(args true '{"home":true}' "$3" "$4")" >/dev/null
}

for aim in "${aims[@]}"; do
  read -r distance pitch <<<"$aim"
  done=$(measured)
  while IFS=$'\t' read -r package school; do
    class=$package.${package##*/}_C
    [[ $class =~ $only ]] || continue
    grep -qlF "\"$class\"" "$build"/site/skills/*.json || continue
    grep -qxF "$class" <<<"$done" && continue
    cast "$class" "$school" "$distance" "$pitch" || echo "failed: $class at $aim" >&2
  done <<<"$skills"
done

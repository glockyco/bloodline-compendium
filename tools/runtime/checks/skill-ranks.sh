#!/usr/bin/env bash
# Casts every skill whose code asks for its discipline's damage passives, once with those passives at rank 0 and once
# with all of them at their highest rank, at a still enemy. The next `catalog` reads the latest run named skill-ranks
# and publishes a skill's scaling only where its hits rose. Run inside `nix develop` with the lab launched on a new
# save, after `begin skill-ranks`. Pass the build's directory, such as artifacts/<build>.
set -euo pipefail
lab() { bun "$(dirname "$0")/../lab.mjs" "$@"; }
build=${1:?Give the build directory}
# Each skill class whose package calls a discipline's damage helper, with that discipline.
skills=$(jq -r '.functions[] | .package as $package | .calls[]?
    | select(startswith("virtual:Get ") and endswith(" Damage Bonuses"))
    | [$package, .[12:-15]] | @tsv' "$build/records/code/index.json" | sort -u)
# Every skill is cast at rank 0 before any is cast at full ranks, so each skill's cooldown ends between its casts.
for full in false true; do
  while IFS=$'\t' read -r package school; do
    class=$package.${package##*/}_C
    grep -qlF "\"$class\"" "$build"/site/skills/*.json || continue
    passives=$(jq -c '[.passiveRows[].passives[]
        | select((.description // "") | test("^\\+[0-9.]+% \\w+ damage per rank"))
        | {name: (.name | gsub(" "; "")), ranks}]' "$build/site/disciplines/$(tr '[:upper:]' '[:lower:]' <<<"$school").json")
    args=$(jq -nc --arg skill "$class" --arg school "$school" --argjson full "$full" --argjson passives "$passives" \
      '{skill: $skill, school: $school, seconds: 3,
        ranks: ($passives | map({key: .name, value: (if $full then .ranks else 0 end)}) | from_entries)}')
    lab eval "$(dirname "$0")/skill-damage.lua" "$args" >/dev/null || echo "failed: $args" >&2
  done <<<"$skills"
done

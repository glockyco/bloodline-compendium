#!/usr/bin/env bash
# Measures what eating heals (tasks 5.2 and 5.4 of establish-reference-architecture). Run it inside `nix develop`
# with the lab game running a fresh save, optionally with the class paths of the foods to eat as arguments. Health
# is brought to 10 before each meal and sampled every frame: the seconds before the meal give the regeneration rate,
# and the frame of the meal shows the heal. Without arguments it eats four foods whose item data and food effect
# table disagree:
#   Apple Pie: PotionInfo 15, food effect table 200 and a regeneration boost, description 200.
#   Fondue: PotionInfo 0, food effect table 300, description 300.
#   Apple Sauce: PotionInfo 0, food effect table 75 and a regeneration boost.
#   Roasted Apple: PotionInfo 75 with a Restore Health potion effect, food effect table 25.
set -euo pipefail
lab() { bun "$(dirname "$0")/../lab.mjs" "$@"; }
foods=(
  /Game/BloodlinesAlpha/Interactables/Crafting_Items/GeneralFoodItems/ApplePie.ApplePie_C
  /Game/BloodlinesAlpha/Interactables/Crafting_Items/GeneralFoodItems/Fondue.Fondue_C
  /Game/BloodlinesAlpha/Interactables/AppleSauce.AppleSauce_C
  /Game/BloodlinesAlpha/Interactables/Crafting_Items/RoastedApple.RoastedApple_C
)
if (($# > 0)); then foods=("$@"); fi
values='["Health","Max Health","Health Regen Speed"]'
items=$(printf '"%s",' "${foods[@]}")
lab give "{\"items\":[${items%,}]}"
lab open-sheet
lab read-tooltips
for food in "${foods[@]}"; do
  lab take-damage '{"toHealth":10}'
  lab sample "{\"seconds\":8,\"values\":$values,\"actions\":[{\"at\":4,\"command\":\"use\",\"args\":{\"item\":\"$food\"}}]}"
done

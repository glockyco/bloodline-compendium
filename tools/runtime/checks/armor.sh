#!/usr/bin/env bash
# Measures which armor values the game applies (task 5.1 of establish-reference-architecture). Run it inside
# `nix develop` with the lab game running a fresh save: each step equips one more item through its inventory
# slot, then reads the armor and speed state, the character sheet and the damage a 20 point hit deals.
set -euo pipefail
lab() { bun "$(dirname "$0")/../lab.mjs" "$@"; }
items=(
  # Feral Maw: class default 5 Medium, armor table 1 Light.
  /Game/BloodlinesAlpha/Interactables/Armor/Head/HeadArmor_Tusks.HeadArmor_Tusks_C
  # Winter Warden Cuirass: class default 2 Light, armor table 13 Heavy.
  /Game/BloodlinesAlpha/Interactables/Armor/Torso/Torso_Armor_WinteWarden.Torso_Armor_WinteWarden_C
  # Leather Pants: class default 4, and its name matches no row of the armor table.
  /Game/BloodlinesAlpha/Interactables/Armor/Legs/Leather_Pants.Leather_Pants_C
)
state='{"values":["Health","Max Health","ArmorTotal","WalkSpeed","RunSpeed","PlayerPassiveLevelStatComponent.Total Armor","PlayerPassiveLevelStatComponent.Actual Armor","PlayerPassiveLevelStatComponent.Effective Armor","PlayerPassiveLevelStatComponent.Movement Speed Mult","PlayerPassiveLevelStatComponent.Heavy Armor Level","PlayerPassiveLevelStatComponent.Medium Armor Level","PlayerPassiveLevelStatComponent.Light Armor Level"]}'
measure() {
  lab player "$state"
  lab read-widget '{"class":"CharacterSheet_C"}'
  lab take-damage '{"amount":20}'
}
lab open-sheet
measure
for item in "${items[@]}"; do
  lab give "{\"items\":[\"$item\"]}"
  lab read-tooltips
  lab equip "{\"item\":\"$item\"}"
  measure
done

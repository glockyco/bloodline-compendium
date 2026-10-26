#!/usr/bin/env bash
# Checks when knee armor starts to count (task 5.1 of establish-reference-architecture). Run it inside
# `nix develop` with the lab game running a fresh save. Knee guards are equipped first, then a sword, which wears
# no armor: if the armor total only rises after the sword, equipping knees does not recalculate armor.
set -euo pipefail
lab() { bun "$(dirname "$0")/../lab.mjs" "$@"; }
knees=/Game/BloodlinesAlpha/Interactables/Armor/Knees/Knee_Armor_01.Knee_Armor_01_C
sword=/Game/BloodlinesAlpha/Interactables/Weapons/1HandedWeapons/1HSword_BloodforgedBlade.1HSword_BloodforgedBlade_C
state='{"values":["ArmorTotal","PlayerPassiveLevelStatComponent.Total Armor","PlayerPassiveLevelStatComponent.Effective Armor"]}'
lab give "{\"items\":[\"$knees\",\"$sword\"]}"
lab open-sheet
lab read-tooltips
lab player "$state"
lab equip "{\"item\":\"$knees\"}"
lab player "$state"
lab take-damage '{"amount":10}'
lab equip "{\"item\":\"$sword\"}"
lab player "$state"
lab take-damage '{"amount":10}'

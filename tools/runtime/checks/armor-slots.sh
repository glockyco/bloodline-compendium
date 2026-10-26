#!/usr/bin/env bash
# Measures which equipment slots add armor (task 5.1 of establish-reference-architecture). Run it inside
# `nix develop` with the lab game running a fresh save. Each step equips one item of another slot whose armor
# table value differs from its class default, then reads the applied total and the damage of a 10 point hit.
set -euo pipefail
lab() { bun "$(dirname "$0")/../lab.mjs" "$@"; }
items=(
  /Game/BloodlinesAlpha/Interactables/Armor/Torso/BlackScholarRobeTop.BlackScholarRobeTop_C
  /Game/BloodlinesAlpha/Interactables/Armor/Head/AllseersJudgement.AllseersJudgement_C
  /Game/BloodlinesAlpha/Interactables/Armor/Arms/Arm_Armor_ClothSleeves.Arm_Armor_ClothSleeves_C
  /Game/BloodlinesAlpha/Interactables/Armor/Arms/Arm_Armor_ArchersStraps.Arm_Armor_ArchersStraps_C
  /Game/BloodlinesAlpha/Interactables/Armor/Legs/AskillianGreaves.AskillianGreaves_C
  /Game/BloodlinesAlpha/Interactables/Armor/Knees/Knee_Armor_01.Knee_Armor_01_C
  /Game/BloodlinesAlpha/Interactables/Armor/Feet/Foot_Armor_ArchersBoots.Foot_Armor_ArchersBoots_C
  /Game/BloodlinesAlpha/Interactables/Armor/Shoulders/AltarlightPauldrons.AltarlightPauldrons_C
  /Game/BloodlinesAlpha/Interactables/Armor/ETC/BonecallersBand.BonecallersBand_C
  /Game/BloodlinesAlpha/Interactables/Armor/ETC/Backpack4.Backpack4_C
)
state='{"values":["ArmorTotal","PlayerPassiveLevelStatComponent.Total Armor","PlayerPassiveLevelStatComponent.Effective Armor","PlayerPassiveLevelStatComponent.Movement Speed Mult"]}'
lab open-sheet
lab player "$state"
for item in "${items[@]}"; do
  lab give "{\"items\":[\"$item\"]}"
  lab equip "{\"item\":\"$item\"}"
  lab player "$state"
  lab take-damage '{"amount":10}'
done

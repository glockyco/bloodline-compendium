#!/usr/bin/env bash
# Recheck on a fresh save. Pass headless to omit screenshots when the renderer is unavailable.
set -euo pipefail
lab() { bun "$(dirname "$0")/../lab.mjs" "$@"; }
armor='{"values":["ArmorTotal","PlayerPassiveLevelStatComponent.Total Armor","PlayerPassiveLevelStatComponent.Effective Armor"]}'
item=/Game/BloodlinesAlpha/Interactables
knees=$item/Armor/Knees/Knee_Armor_01.Knee_Armor_01_C
caps=$item/Armor/Knees/Knee_Armor_StandardCaps.Knee_Armor_StandardCaps_C
sword=$item/Weapons/1HandedWeapons/1HSword_BloodforgedBlade.1HSword_BloodforgedBlade_C
maw=$item/Armor/Head/HeadArmor_Tusks.HeadArmor_Tusks_C
hood=$item/Armor/Head/Head_Armor_ClothHood.Head_Armor_ClothHood_C
pants=$item/Armor/Legs/Leather_Pants.Leather_Pants_C
band=$item/Armor/ETC/BonecallersBand.BonecallersBand_C
route=${1:-player}
observe() {
  lab player "$armor"
  if [[ ${2:-} != headless ]]; then lab capture-screen "armor-$1"; fi
}
step() {
  lab equip "{\"item\":\"$2\"}"
  observe "$1" "${3:-}"
}
if [[ "$route" == original ]]; then
  lab give "{\"items\":[\"$knees\",\"$sword\",\"$maw\",\"$pants\",\"$band\"]}"
else
  lab give "{\"items\":[\"$knees\",\"$caps\",\"$sword\",\"$hood\"]}"
fi
lab open-sheet '{"refresh":true}'
lab read-tooltips
observe none "${2:-}"
step knees "$knees" "${2:-}"
if [[ "$route" == original ]]; then
  step sword "$sword" "${2:-}"
  step maw "$maw" "${2:-}"
  step pants "$pants" "${2:-}"
  step band "$band" "${2:-}"
  lab close-tutorials
  observe after-tutorials "${2:-}"
else
  if [[ "$route" == armed ]]; then
    step sword "$sword" "${2:-}"
    lab close-tutorials
    observe after-tutorials "${2:-}"
  fi
  step hood "$hood" "${2:-}"
  step caps "$caps" "${2:-}"
  lab unequip-all
  observe reset "${2:-}"
  step caps-alone "$caps" "${2:-}"
  step hood-after-caps "$hood" "${2:-}"
fi

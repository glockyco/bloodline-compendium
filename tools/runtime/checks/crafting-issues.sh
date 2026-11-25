#!/usr/bin/env bash
# Recheck the inconsistent damage of a grade D assembled spear. Pass blank to exercise the hidden recipe handler.
set -euo pipefail
checks=$(dirname "$0")
lab() { bun "$checks/../lab.mjs" "$@"; }
craft() { lab eval "$checks/crafting-issues.lua" "{\"action\":\"$1\",\"name\":\"Copper Test Spear\"}"; }
hover() { lab eval "$checks/assembled-weapon-tooltips.lua" "{\"action\":\"$1\",\"name\":\"${2:-}\",\"capture\":true}"; }
close() { lab call "{\"class\":\"$1\",\"function\":\"Close\",\"cases\":[{}],\"outputs\":[]}"; }
close_sheet() { lab press '{"widget":"CharacterSheet_C","button":"WBP_CloseButton"}'; }
dialogue() { lab eval "$checks/design-facts.lua" "$1"; }
item=/Game/BloodlinesAlpha/Interactables/Crafting_Items
if [[ ${1:-trade} == blank ]]; then
  lab give "{\"count\":3,\"items\":[\"$item/Ore_And_Ingots/CopperIngot.CopperIngot_C\",\"$item/Pine_Log.Pine_Log_C\"]}"
  craft open
  craft select
  lab capture-screen copper-claymore-recipe
  craft inventory
  craft auto
  craft inventory
  craft close
  lab open-sheet '{"refresh":true}'
  hover inventory ''
  lab capture-screen empty-weapon-in-inventory
  hover clear
  close_sheet
  exit
fi
lab give "{\"items\":[\"$item/WeaponComponents/WC_PinePolearmShaft.WC_PinePolearmShaft_C\",\"$item/WeaponComponents/WC_SmallCopperSpearhead.WC_SmallCopperSpearhead_C\"]}"
craft open
craft assemble
craft hit
craft hit
craft finish
craft inventory
craft close
lab open-sheet '{"refresh":true}'
hover inventory 'Copper Test Spear'
lab capture-screen assembled-spear-in-inventory
hover clear
close_sheet
dialogue '{"near":"ShopKeeper_Base_C","name":"Shopkeeper_Base_4","offset":{"X":180,"Y":0,"Z":5}}'
dialogue '{"input":"IA_Interact"}'
dialogue '{}'
dialogue '{"reply":"have in stock"}'
lab capture-screen theo-trading
hover trade 'Copper Test Spear'
lab capture-screen assembled-spear-in-trade
hover clear
close Window_Trading_Shop_C
hover hotswap 'Copper Test Spear'
close HotSwapItems_C

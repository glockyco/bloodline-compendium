#!/usr/bin/env bash
# Walks the steps of the entries in issues/known-issues.json as a player would and takes a screenshot after each
# step, next to the game's own values, so a recheck shows what the player sees. Run it inside `nix develop` with the
# lab game launched with `launch render` on a fresh save, twice, in separate runs.
set -euo pipefail
lab() { bun "$(dirname "$0")/../lab.mjs" "$@"; }
item=/Game/BloodlinesAlpha/Interactables
food=$item/Crafting_Items/GeneralFoodItems
knees=$item/Armor/Knees/Knee_Armor_01.Knee_Armor_01_C
sword=$item/Weapons/1HandedWeapons/1HSword_BloodforgedBlade.1HSword_BloodforgedBlade_C
maw=$item/Armor/Head/HeadArmor_Tusks.HeadArmor_Tusks_C
pants=$item/Armor/Legs/Leather_Pants.Leather_Pants_C
band=$item/Armor/ETC/BonecallersBand.BonecallersBand_C
# Foods whose descriptions promise an effect while the food effect table has no row for them.
uneaten=(
  "steak $item/Steak.Steak_C"
  "soup $item/Soup.Soup_C"
  "turkey $item/TukeyLeg.TukeyLeg_C"
  "wrangler $item/Crafting_Items/CookedWrangler.CookedWrangler_C"
  "sandwich $item/WranglerSandwich.WranglerSandwich_C"
  "potato $item/Crafting_Items/PotatoSeeds.PotatoSeeds_C"
  "soggy $item/Crafting_Items/SoggySandwich.SoggySandwich_C"
  "bun $item/SweetBun.SweetBun_C"
)
apple=$item/Crafting_Items/RoastedApple.RoastedApple_C
stew=$food/DruidsStew.DruidsStew_C
cake=$food/CarrotCake.CarrotCake_C
armor='{"values":["ArmorTotal","PlayerPassiveLevelStatComponent.Total Armor"]}'
health='["Health","Max Health","Health Regen Speed"]'
meals=$(printf '"%s",' "${uneaten[@]#* }")
lab give "{\"items\":[\"$knees\",\"$sword\",\"$maw\",\"$pants\",\"$band\",${meals}\"$apple\",\"$stew\",\"$cake\"]}"
lab open-sheet
lab read-tooltips
lab player "$armor"
lab capture-screen armor-none
# knee-armor-not-counted: the knee guards count only once another armor piece is equipped.
for step in "knees $knees" "sword $sword" "maw $maw"; do
  lab equip "{\"item\":\"${step#* }\"}"
  lab player "$armor"
  lab capture-screen "armor-${step%% *}"
done
# armor-without-row and armor-slot-adds-none: each piece leaves Armor where it was.
for step in "pants $pants" "band $band"; do
  lab equip "{\"item\":\"${step#* }\"}"
  lab player "$armor"
  lab capture-screen "armor-${step%% *}"
done
# food-without-row, with Roasted Apple as a food that heals: Health is brought to 10, sampled every frame around the
# meal, and captured before and after, which also shows the status effects. The tutorial popup that equipping the
# sword opens covers the status effects and returns after closing, so it is closed before each screenshot.
for step in "${uneaten[@]}" "apple $apple"; do
  lab take-damage '{"toHealth":10}'
  lab close-tutorials
  lab capture-screen "${step%% *}-before"
  lab sample "{\"seconds\":6,\"values\":$health,\"actions\":[{\"at\":2,\"command\":\"use\",\"args\":{\"item\":\"${step#* }\"}}]}"
  lab close-tutorials
  lab capture-screen "${step%% *}-after"
done
# food-effect-not-applied: the status effects after Druid's Stew, then after Carrot Cake.
for step in "stew $stew" "cake $cake"; do
  lab use "{\"item\":\"${step#* }\"}"
  lab close-tutorials
  lab capture-screen "${step%% *}-eaten"
done

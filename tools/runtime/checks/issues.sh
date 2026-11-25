#!/usr/bin/env bash
# Walks the steps of the entries in issues/known-issues.json as a player would and takes a screenshot after each
# step, next to the game's own values, so a recheck shows what the player sees. Run it inside `nix develop` with the
# lab game launched with `launch render` on a new save.
# For all checks, set BLOODLINE_SITE_DATA to the regenerated catalog's site directory.
# Pass `staff` to repeat only the staff checks.
set -euo pipefail
lab() { bun "$(dirname "$0")/../lab.mjs" "$@"; }
if [[ "${1:-all}" != "staff" ]]; then
  : "${BLOODLINE_SITE_DATA:?Set BLOODLINE_SITE_DATA to the regenerated catalog.}"
item=/Game/BloodlinesAlpha/Interactables
food=$item/Crafting_Items/GeneralFoodItems
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
bread=$food/ButteredBread.ButteredBread_C
spud=$item/Crafting_Items/SpudColossus.SpudColossus_C
potions=(
  "health $item/HealthPotion.HealthPotion_C"
  "mana /Game/ManaPotion.ManaPotion_C"
  "cider $item/CastlesideCider.CastlesideCider_C"
  "flaggon $item/TheDwarfsFlaggonBeer.TheDwarfsFlaggonBeer_C"
)
armor='{"values":["ArmorTotal","PlayerPassiveLevelStatComponent.Total Armor"]}'
health='["Health","Max Health","Health Regen Speed"]'
meals=$(printf '"%s",' "${uneaten[@]#* }")
lab close-tutorials
bash "$(dirname "$0")/crafting-issues.sh" trade
foragers() { lab eval "$(dirname "$0")/foragers-feast.lua" "{\"action\":\"$1\",\"name\":\"${2:-}\"}"; }
flower=$item/Crafting_Items/Herbs_And_Roots/LoveMeNotFlower.LoveMeNotFlower_C
lab give "{\"items\":[\"$item/ChickenMeat.ChickenMeat_C\"]}"
lab give "{\"count\":2,\"items\":[\"$flower\"]}"
foragers open
lab capture-screen foragers-cooking-window
foragers add "Chicken Meat"
foragers add "Love-me-not Flower"
foragers add "Love-me-not Flower"
lab capture-screen foragers-ingredients
foragers cook
foragers wait
lab capture-screen foragers-result
foragers close
lab open-sheet
lab read-tooltips
lab capture-screen foragers-inventory
lab press '{"widget":"CharacterSheet_C","button":"WBP_CloseButton"}'
bash "$(dirname "$0")/knee-issue.sh" player
lab give "{\"items\":[\"$pants\",\"$band\",${meals}\"$apple\",\"$stew\",\"$cake\",\"$bread\",\"$spud\",$(printf '"%s",' "${potions[@]#* }" | sed 's/,$//')]}"
lab open-sheet '{"refresh":true}'
lab read-tooltips
# armor-without-row and armor-slot-adds-none: each piece leaves Armor where it was.
for step in "pants $pants" "band $band"; do
  lab equip "{\"item\":\"${step#* }\"}"
  lab player "$armor"
  lab capture-screen "armor-${step%% *}"
done
# food-without-row, with Roasted Apple as a food that heals: Health is brought to 10, sampled every frame around the
# meal, and captured before and after, which also shows the status effects.
for step in "${uneaten[@]}" "apple $apple"; do
  lab take-damage '{"toHealth":10}'
  lab close-tutorials
  lab capture-screen "${step%% *}-before"
  lab sample "{\"seconds\":6,\"values\":$health,\"actions\":[{\"at\":2,\"command\":\"use\",\"args\":{\"item\":\"${step#* }\"}}]}"
  lab close-tutorials
  lab capture-screen "${step%% *}-after"
done
# food-description-mismatch: Health, Mana and Stamina are brought low and sampled every frame around each meal.
for step in "bread $bread" "spud $spud"; do
  lab take-damage '{"toHealth":10}'
  lab eval "$(dirname "$0")/lower-bars.lua" '{"mana":5,"stamina":5}'
  lab close-tutorials
  lab capture-screen "${step%% *}-before"
  lab sample "{\"seconds\":4,\"values\":[\"Health\",\"Current Mana\",\"Stamina\"],\"actions\":[{\"at\":1,\"command\":\"use\",\"args\":{\"item\":\"${step#* }\"}}]}"
  lab close-tutorials
  lab capture-screen "${step%% *}-after"
done
# potions-without-effect: Health, Mana and Stamina are brought low and sampled every frame around each potion.
for potion in "Health Potion" "Mana Potion"; do
  lab eval "$(dirname "$0")/assembled-weapon-tooltips.lua" "{\"action\":\"inventory\",\"name\":\"$potion\",\"capture\":true}"
  lab capture-screen "${potion%% *}-potion-tooltip"
  lab eval "$(dirname "$0")/assembled-weapon-tooltips.lua" '{"action":"clear"}'
done
for step in "${potions[@]}"; do
  lab take-damage '{"toHealth":10}'
  lab eval "$(dirname "$0")/lower-bars.lua" '{"mana":5,"stamina":5}'
  lab sample "{\"seconds\":4,\"values\":[\"Health\",\"Current Mana\",\"Stamina\"],\"actions\":[{\"at\":0.5,\"command\":\"use\",\"args\":{\"item\":\"${step#* }\"}}]}"
  lab close-tutorials
  lab capture-screen "${step%% *}-used"
done
# food-effect-not-applied: the status effects after Druid's Stew, then after Carrot Cake.
for step in "stew $stew" "cake $cake"; do
  lab use "{\"item\":\"${step#* }\"}"
  lab close-tutorials
  lab capture-screen "${step%% *}-eaten"
done
fi

item=/Game/BloodlinesAlpha/Interactables
staff_check() { lab eval "$(dirname "$0")/staff-issues.lua" "$1"; }
fairy=$item/Weapons/Halberds/FairyStaff.FairyStaff_C
# staves-without-fireballs: Fairy Staff's tooltip, then the HUD before, during and after a normal swing.
staff_check "{\"action\":\"equip\",\"item\":\"$fairy\"}"
staff_check "{\"action\":\"tooltip\",\"item\":\"$fairy\"}"
lab close-tutorials
lab capture-screen fairy-tooltip-visible
staff_check '{"action":"close-tooltip"}'
lab close-tutorials
lab capture-screen fairy-before-swing
staff_check '{"action":"attack","pauseDuring":true}'
lab capture-screen fairy-during-swing
staff_check '{"action":"observe","seconds":2}'
lab capture-screen fairy-after-swing
# The other staves with the same description: each swing's spawned projectiles and mana are in the run's log.
for staff in NecroStaff DeathsGrasp_Soulstaff PriestsSoulStaff; do
  staff_check "{\"action\":\"equip\",\"item\":\"$item/Weapons/Halberds/$staff.${staff}_C\"}"
  staff_check '{"action":"attack","seconds":2}'
done

# staff-point-blank-damage: each fresh Goblin Brute has enough health for both its health bar and the damage
# number to remain visible. The second normal combo is reached with empty-space swings, not a counter edit.
staves=(
  "geomancer $item/Weapons/Staves/AcolyteGeomancerStaff.AcolyteGeomancerStaff_C"
  "walking $item/Weapons/Halberds/WalkingStick.WalkingStick_C"
  "moonstone $item/Weapons/Staves/MoonstoneStaff.MoonstoneStaff_C"
)
for staff in "${staves[@]}"; do
  name=${staff%% *}
  staff_check "{\"action\":\"equip\",\"item\":\"${staff#* }\"}"
  for range in "range 600" "near 130"; do
    staff_check '{"action":"prime"}'
    staff_check "{\"action\":\"target\",\"distance\":${range#* }}"
    staff_check '{"action":"observe","seconds":0.5}'
    lab capture-screen "$name-${range%% *}-before"
    staff_check '{"action":"attack","pauseOnHit":true,"seconds":3}'
    lab capture-screen "$name-${range%% *}-after"
  done
done

# weapon-tooltip-damage: one hit on a fresh Goblin Brute from range, after the tooltip sweep has read each tooltip. The
# swinging staves need melee range, the others range for their projectile.
for staff in "Halberds/WalkingStick 600" "Staves/AdeptCryomancerStaff 600" "Staves/MoonstoneStaff 600" \
  "Staves/ChristmasStaff 600" "Halberds/PintoStaff 130" "Halberds/FairyStaff 130" "Halberds/NecroStaff 130"; do
  path=${staff%% *}
  staff_check "{\"action\":\"equip\",\"item\":\"$item/Weapons/$path.${path##*/}_C\"}"
  staff_check "{\"action\":\"target\",\"distance\":${staff#* }}"
  staff_check '{"action":"attack","pauseOnHit":true,"seconds":3}'
done

# The other melee weapons follow the same overlap path. Derive them from the current issue notes, not a fixed list.
if [[ "${1:-all}" != "staff" ]]; then
  while IFS= read -r weapon; do
    staff_check "{\"action\":\"equip\",\"item\":\"$weapon\"}"
    staff_check '{"action":"target","distance":130}'
    staff_check '{"action":"attack","pauseOnHit":true,"seconds":3}'
  done < <(bun -e '
    const root = process.env.BLOODLINE_SITE_DATA;
    if (!root) throw new Error("Set BLOODLINE_SITE_DATA to the regenerated catalog.");
    for (const file of new Bun.Glob("items/*.json").scanSync(root)) {
      const item = await Bun.file(`${root}/${file}`).json();
      if (item.notes.some(note => note.issue === "weapon-tooltip-damage")
        && !["Staff", "Shield", "Gun", "Pistol", "Crossbow", "Bow"].includes(item.type)) console.log(item.key);
    }
  ')
fi

# halberds-train-no-proficiency: one hit each on a fresh Goblin Brute, then Detailed Stats' passive levels. Bloodforged
# Blade trains One-Handed Weapons and Copper Battleaxe Two-Handed Weapons, while Lonland Halberd and Fairy Staff train
# none. The window stays on screen once open, so it opens last, and each hit's EXP before and after is in the run's log.
for weapon in "sword 1HandedWeapons/1HSword_BloodforgedBlade false" \
  "copper Copper1HWeapons/Copper_Great_Battleaxe true" "halberd Halberds/LonlandHalberd true" \
  "fairy Halberds/FairyStaff true"; do
  read -r name path release <<<"$weapon"
  staff_check "{\"action\":\"equip\",\"item\":\"$item/Weapons/$path.${path##*/}_C\"}"
  staff_check '{"action":"target","distance":130}'
  staff_check "{\"action\":\"attack\",\"pauseOnHit\":true,\"seconds\":3,\"releaseOnly\":$release}"
  lab capture-screen "proficiency-$name-hit"
done
staff_check '{"action":"passive-levels"}'
lab capture-screen proficiency-after

if [[ "${1:-all}" != "staff" ]]; then
# copied-item-names: each tooltip shows another item's name above a description of something else.
copied=(
  "$item/Crafting_Items/Ore_And_Ingots/EarthOrb.EarthOrb_C"
  "$item/Crafting_Items/Ore_And_Ingots/Whisperquartz.Whisperquartz_C"
  "$item/Crafting_Items/WeaponComponents/WC_TerrashardWandCore.WC_TerrashardWandCore_C"
  "$item/Weapons/1HandedWeapons/1HAXE_Woodaxe.1HAXE_Woodaxe_C"
  "$item/Crafting_Items/WeaponComponents/WC_BonemetalBlade.WC_BonemetalBlade_C"
)
lab clear-tooltips
lab give "{\"items\":[$(printf '"%s",' "${copied[@]}" | sed 's/,$//')]}"
lab open-sheet
lab read-tooltips
lab place-tooltips
lab capture-screen copied-names
lab clear-tooltips
# shared-save-tags: one Emberglass of a pair in the Hills of Delvenaugh is taken, the game saves and the area loads
# again. The area removes taken objects about a minute after it loads, and then the untaken Emberglass is gone too.
pickups=$(dirname "$0")/placed-pickups.lua
delvenaugh=/Game/Overworld/OpenWorldAreas/HillsOfDelvenaugh
lab eval "$pickups" "{\"open\":\"$delvenaugh\"}"
sleep 90
lab eval "$pickups" '{}'
lab eval "$pickups" '{"take":"TagBasedInteractionObject12_20"}'
sleep 6
lab eval "$(dirname "$0")/save-game.lua"
lab eval "$pickups" "{\"open\":\"$delvenaugh\"}"
sleep 90
lab eval "$pickups" '{}'
lab capture-screen shared-tags-after
# blood-magic-points-follow-blood-setting: a Blood Magic level after No blood, then after BLOODBATH, then after
# spending points, each followed by the Blood Magic tree, whose header shows the points.
blood=$(dirname "$0")/blood-setting.lua
progression=$(dirname "$0")/progression.lua
blood_tree() {
  lab eval "$progression" '{"action":"open","category":"Magic_BTN","button":"BloodMagic"}'
  lab capture-screen "blood-$1"
  lab eval "$progression" '{"action":"close"}'
}
lab eval "$blood" '{"index":0}'
blood_tree none
lab eval "$blood" '{"index":4}'
blood_tree bloodbath
lab eval "$progression" '{"action":"open","category":"Magic_BTN","button":"BloodMagic"}'
lab eval "$progression" '{"action":"passive","tree":"BloodMagicLBSkillTree","widget":"BloodScent","times":1}'
lab eval "$progression" '{"action":"close"}'
lab eval "$blood" '{"index":4}'
blood_tree spent
# rusty-crossbow-fires-nothing: the Rusty Crossbow that merchants sell and Squalls End holds, then the one crates
# give, each fired at a Goblin Brute at 300 units, with a screenshot after each shot.
ranged=$(dirname "$0")/ranged.lua
crossbows=$item/Weapons/Crossbows
lab give "{\"items\":[\"$item/Crafting_Items/Arrow_Item.Arrow_Item_C\"],\"count\":20}"
for crossbow in RustyCrossbow Rusty_Crossbow; do
  lab eval "$ranged" "{\"action\":\"equip\",\"item\":\"$crossbows/$crossbow.${crossbow}_C\"}"
  lab eval "$ranged" '{"action":"target","distance":300,"pitch":-1}'
  lab eval "$ranged" '{"action":"attack","seconds":3}'
  lab capture-screen "crossbow-$crossbow"
done
fi

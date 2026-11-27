---
name: runtime-checks
description: Run The Bloodline in the lab to observe behaviour, read tooltips, call the game's functions, sample values per frame, take screenshots or render item models. Use when a fact needs an observation in the running game, when a rule needs the game's own result, when checking what a tooltip or the character sheet shows, or when producing model renders.
---

# Runtime checks

`tools/runtime/lab.mjs` runs a modded copy of the game off screen and drives it through the game's own functions. Every command is recorded in a run under `artifacts/<build>/runtime/<run-id>/`. Run it with `bun tools/runtime/lab.mjs <command>` inside `nix develop`. Without a command it lists all of them.

## The lab copy

`prepare` clones the installed Steam build into `BloodlineLab/<build>/` on the bottle's C: drive and installs the pinned UE4SS and the bridge mod. The clone shares the installation's blocks, so it takes no space and seconds. The Steam installation and the player's saves are never touched: `labDirectory` in `bloodline.local.json` must lie outside the Steam library, and every launch passes `-NotInstalled`, which keeps saves and logs in the lab copy.

Run `prepare` again after a game update or after changing `tools/runtime/bridge/main.lua`. It reinstalls UE4SS and the bridge into an existing copy.

When replacing the UE4SS pin, try a stable release before a newer experimental build. The compatibility reason for the current pin and its archive checksums live beside `ue4ss` in `tools/runtime/lab.mjs`.

## Sessions

- `launch` starts the game headless, with `-nullrhi`. Blueprints, widgets, tooltips and per-frame sampling work, and the machine stays free to use.
- `launch render` draws off screen with `-RenderOffScreen` for screenshots and model renders. The game registers as a background app without a window, so it never takes focus.
- `launch window` shows a game window, for checks that need the real cursor. Hovering raises a tooltip only on an operating system mouse-move event, not when the player controller moves the cursor. `swift tools/runtime/desktop.swift` lists the game's windows, brings the game to the front and moves the mouse, and `screencapture -l <window id>` captures only the game's window. The window takes the front and the mouse, so ask before using it while someone works at the machine.
- `quit` ends the session through the game's own `QuitGame` and stops only the process the session started.
- `begin <label>` opens a new run in the same session. Repeated measurements go to separate runs.

`launch` also starts `limit-frame-rate`, which holds the frame rate at 60, because the game resets the limit on every level load.

A fresh session sits in the main menu. `new-save <name>` starts a game and creates a character with the default look. Give every run a new name of at most 20 characters: the game keeps only 20, and starting a save that exists loads it without creating a character, so `new-save` refuses both. `close-tutorials` closes the tutorial popups that pause the game afterwards.

## Driving the game

Commands in `tools/runtime/commands/` change state only through the game's own functions. A command that does what a player could not, such as `give`, `call`, `take-damage`, `set-time` or `set-weather`, says so in its `-- lab setup:` line, and the run records that line with every use.

- Buttons are pressed by calling the function that the widget class binds to the button's click, which `press` reads from the class's `ComponentDelegateBindings`. A CommonUI button's handler takes the button as its argument. Never click by screen position.
- `give` builds each item as an actor, so its construction script fills its item info as for an item lying in the world, and passes that to the player's `AddItemsToInventory`.
- `equip` and `use` click the item's inventory slot, which needs `open-sheet` first. Only slots the sheet shows count: the sheet leaves replaced slots alive, and pressing them does nothing. `unequip-all` clicks every equipment slot of the sheet that holds an item, which takes the item off.
- `read-tooltips` calls each shown slot's own `OnMouseEnter`, which builds the tooltip as hovering does, and reads the text blocks the tooltip shows. Text in a collapsed or hidden box counts as hidden, because tooltips keep unused lines filled in and collapse the box around them. `tooltip-batch` gives a batch of items, reads their tooltips and empties the inventory again, and marks its result `shownOnly`.
- `place-tooltips` shows the tooltips of all inventory slots at once, four to a row, and `clear-tooltips` removes them and empties the inventory. A screenshot then shows the tooltips as the game draws them. Run hover-check compared this placement with hovering by the real cursor in a game window and found them the same.
- `take-damage` deals damage through the player's own `TakeDamage`, by an amount or down to a Health value.
- `player` reads player values by dotted path, such as `PlayerPassiveLevelStatComponent.Total Armor`, with the items held and equipped. `read-widget` reads a widget's text blocks.
- `call` sets inputs on a live object, calls a function and reads outputs, for checking a rule against the game's own result. `params` lists a function's parameters.
- `sample` reads values every frame for a number of game seconds and can run commands at set times, such as eating while Health is sampled.
- `eval <file>` runs a Lua file with the command helpers, for exploring. The run records the code.

Commands that look up objects use the instances the game uses. Class defaults and the templates inside a class, whose outer chain reaches a class, are skipped. Fields of the game's own structs carry GUID suffixes in this build, so commands read them through `field`, which takes plain names. Headless, bound text such as the character sheet's lines keeps its design-time value, so read the values behind it.

## What an observation must show

A published fact or a known issue rests on the running game, because code is easy to misread. What counts depends on the claim:

- What the game shows, such as a tooltip line, rests on what the screen draws. A shown-only tooltip read counts as that, because it leaves out the text that collapsed boxes keep, as the screen does. Other interface text needs a screenshot.
- What the game does, such as damage dealt, armor applied or health restored, rests on the game's own values during the run. A screenshot adds nothing to them.
- Observations already made on the same build count. Name their runs instead of repeating them.
- A second run on a new save is needed only when the result can vary with position, timing, equip order or chance. A value the game reads from its data gives the same result every time.

## Checks

`tools/runtime/checks/` holds the measurements that settle published facts and known issues, each run on a new save. Run them again after every game update:

- `armor.sh` and `armor-slots.sh` measure which armor values and slots reduce damage and speed.
- `knees.sh` shows when knee armor starts to count.
- `food.sh` samples Health every frame around meals, of four foods with known tables or of the foods whose class paths it is given.
- `issues.sh` walks the steps of every entry in `issues/known-issues.json` with a screenshot after each step, in `launch render`. Use `issues.sh staff` to repeat only the staff issues.
- `noted-armor.mjs <build>` equips every armor item that the catalog notes with an armor issue and reads the armor the game applies, with a screenshot each, in `launch render`.
- `eval tools/runtime/checks/placed-pickups.lua` lists the placed pickup objects of the loaded level with their item, amount and hidden state, for comparison with the level's records. `{"open": "<level package>"}` loads a level first, and `{"take": "<actor name>"}` takes one. `eval tools/runtime/checks/completed-tags.lua` lists the Tags of taken objects that the game instance keeps.
- `eval tools/runtime/checks/ranged.lua`, in `launch render`, measures a ranged weapon's hits with the live input bindings. `{"action": "equip", "item": "<class>"}` equips it, `{"action": "target", "distance": 300, "pitch": -8, "enemy": "<class>"}` places a still enemy, and `{"action": "attack", "hold": 0.08, "seconds": 3}` fires and samples the enemy's Health, with `"block": true` to aim while it fires, which charges a crossbow. Give arrows and bullets first, because a crossbow without them fires nothing.
- `eval tools/runtime/checks/hand-frame.lua`, in `launch render`, lists what the player's hands hold with each item's socket and, in the socket's frame, where the player looks and what is up. `capture-model` turns firearms, crossbows, bows and shields by these axes, so equip one of each and compare the result with the comments in `capture-model.lua`.
- `eval tools/runtime/checks/part-sockets.lua` lists the box and the sockets of the weapon part meshes it is given, by which the game joins parts into a weapon. `capture-model` stands every part with its mesh's Z axis up, so pass the `ComponentMesh` paths of `WeaponComponentTable` and check that the sockets lie as the check's comment says.
- `eval tools/runtime/checks/skill-trees.lua` opens the skill window and every tree through their buttons and lists each tree's skill slots, passive buttons and spellsongs with the values the game set on them. The next `catalog` compares the published rows and passives with the latest run and prints the result as "Tree check".
- `eval tools/runtime/checks/skill-tooltips.lua` builds every tree skill's tooltip through its slot's own `HandleTooltip` and lists the texts it shows. The next `catalog` compares each skill's facts with the latest run's tooltip and prints the result as "Skill tooltip check".
- `tools/runtime/checks/skill-ranks.sh <build directory>`, after `begin skill-ranks` on a new save, casts every skill whose code asks for its discipline's damage passives at rank 0 and at full ranks. The next `catalog` shows a skill's damage passives only where the latest sweep saw a hit rise by their full bonus. A skill whose hits stay the same belongs in the `spells-ignore-damage-passives` issue.
- `eval tools/runtime/checks/input-keys.lua` lists the keys of the game's input mapping contexts with their actions. `{"action": "SkillTree"}` narrows to actions whose name contains it, so a page can name the key a player presses.
- `eval tools/runtime/checks/progression.lua` observes progression: character and discipline level grants, passive rank costs, skill unlocks, attribute increases and the hotbar. Its header lists the actions, including `grant`, which calls `Add Proficiency Points` once per name in `names`. Every action reports the numbers that changed, so it needs no list of field names. A discipline with thousands of levels of experience in one call can stop the game from answering, so raise experience with `"grow"` in small steps.
- `eval tools/runtime/checks/crafting-tiers.lua` lists what the crafting window of the anvil, the engineer's workbench or the smelting oven offers at each level, by setting the profession's level on the game instance and calling the window's own function that fills its tier categories. Its header shows the arguments for each profession. The catalog reads the same tier lists from the window's class, so run it again when they change.
- `eval tools/runtime/checks/harvest-census.lua` lists the loaded level's harvestables in play, placed or spawned, with the preset and loot each applied, and its spawner boxes' tables. `{"clear": true}` first empties the saved harvestables that loading a save spawns again near the player in any level. `bun tools/runtime/harvest-sweep.mjs <build>` runs both for every level that places harvestables or their spawners, and the catalog compares the latest census of each level with what it says the level gives and fails on a difference. Run the sweep on a new save, because a save that has gathered can restore a spot the census does not mark as restored: run census3 found an Oak in the Rocky Hills and Rocky Tundra camps that run oak1 did not find on a new save.
- `eval tools/runtime/checks/harvest-presets.lua` lists the loot, the gathering level and the level checked for each harvest preset, by giving one loaded harvestable each preset name and calling its own `NewFunction_0` and `Check Player Level`. `{"gate": ["Iron", "Pine"]}` instead asks real harvestables of those presets whether the character may harvest them, and `{"draw": 3000}` counts what a table-drawn harvestable's own `Choose Preset` picks. Update `src/Bloodline/Content/harvest-presets.json` from it, because the catalog fails when the game's preset lists or the presets that placements name change.
- `eval tools/runtime/checks/harvest-yield.lua` lists each harvest preset's experience and its bonus chance at its required level and above, by giving one loaded harvestable each preset and setting the profession's level on the game instance. `{"pick": "Pine", "times": 3}` instead calls `Spawn Harvest FX`, the function a harvest swing calls, on one harvestable and lists its stage and the loot in the inventory after each call. `"name"` picks the harvestable by actor name so calls a few seconds apart swing at the same one.
- `eval tools/runtime/checks/level-triggers.lua` lists where the loaded level's trigger volumes stand, with the nearest place name, so a run can say where a level event happens. Open the level first with `placed-pickups.lua`'s `open`.
- `eval tools/runtime/checks/live-maps.lua '{"class":"CustomEnemySpawner_C","property":"Spawns"}'` lists one map property of every live actor of a class as the game holds it. Compare it with the level records after an extraction change, because a placed actor's map is a delta of its class's map.
- `eval tools/runtime/checks/outcrops.lua '{"times": 10, "draws": 2000}'` calls the loaded dungeon populator's own Spawn Mining Outcrops and Spawn Trees and lists what they spawn, then counts the ore types its Choose Outcrop returns.
- `eval tools/runtime/checks/populators.lua` lists the live dungeon populators' ore and tree tables with their spawn chances and the open-world populators' resource counts.
- `eval tools/runtime/checks/world-map-events.lua {"seconds": 90}` samples live world map events each game second, including their names, nearest terrain, chosen duration, remaining countdown and distance from the pawn. Pass `"move": true` to walk a small loop while sampling. After a game update, compare the event names and terrains with the World Map Events page in `src/Bloodline/Content/mechanics.json`, and compare the live event readings with the functions pinned in `src/Bloodline/Catalog/GameEvents.cs`.
- `eval tools/runtime/checks/camp-map-events.lua` lists the active level, remaining event tag and counts of event spawners and creatures. `{"enter": "Bear"}` enters a live Bear event, while `{"tag": "Bear"}` writes the event tag directly and opens a pine forest camp. After a game update, compare the creatures each event brings with the World Map Events page in `src/Bloodline/Content/mechanics.json`, and compare the camp spawner and army readings with the functions pinned in `src/Bloodline/Catalog/GameEvents.cs`.
- `eval tools/runtime/checks/calendar-events.lua` lists the generated calendar dates, event names and areas, the available areas and the foresight range. `{"holidays": true}` lists holiday preset rows, and `{"day": 16, "open": "<level package>"}` sets the saved day and opens a level. After a game update, compare the events and areas with the Calendar Events page in `src/Bloodline/Content/mechanics.json`, and compare the live calendar readings with the functions pinned in `src/Bloodline/Catalog/GameEvents.cs`.

`bun tools/runtime/tooltip-sweep.mjs <build> [--capture]` reads the tooltip of every published item into the current run, and with `--capture`, in `launch render`, also takes a screenshot of every batch of eight. It resumes where earlier sweeps of the build stopped, and retries a batch that stops the game in halves on a restarted game. The next `catalog` compares each item's facts with the latest shown-only tooltip a run read for it. A difference without a known cause in `TooltipCheck.cs` appears in the catalog's `report.json` and needs an owner: a fix in the catalog, a known issue once a run reproduces it, or a new cause.

## Captures

- `capture-screen <name>` keeps an engine screenshot with the interface. Add `noui` for the scene alone.
- `capture-model {"item": "<class path>", "name": "<name>"}` renders an item alone, with a transparent background, at a clear noon, in the light of the character sheet's own preview. Armor shows as the preview character wears it, weapons and other items as their own model, and a weapon part needs `"part": {"mesh", "materials", "scale"}` from its `WeaponComponentTable` row. `openspec/changes/archive/2026-11-01-establish-reference-architecture/design.md` records the presentation and `openspec/changes/add-item-renders/design.md` what each kind of item shows.
- `new-save <name> female` creates the character with the female body, so worn armor renders its female meshes.
- `render-sweep.mjs <build> [female]` renders every published item and `publish-renders.mjs <build>` checks and publishes the renders, as the README's "Extract and publish" describes.
- `eval tools/runtime/checks/item-meshes.lua {"items": [...]}` lists the meshes each item's own actor draws, which tells items without a model apart from failed renders.

Captures need `launch render`.

## Troubleshooting

- A request that times out usually hit an error before the bridge loaded. The game's `ue4ss/UE4SS.log` in the lab copy shows it.
- `No live <class>` means the menu or level that holds the widget is not loaded yet. `probe` reports the world, the pawn and whether given classes exist.
- After a game update, `prepare` refuses a build that Steam has not finished installing.

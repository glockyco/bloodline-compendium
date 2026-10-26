---
name: runtime-checks
description: Run The Bloodline in the lab to observe behaviour, read tooltips, call the game's functions, sample values per frame, take screenshots or render item models. Use when a fact needs an observation in the running game, when a rule needs the game's own result, when checking what a tooltip or the character sheet shows, or when producing model renders.
---

# Runtime checks

`tools/runtime/lab.mjs` runs a modded copy of the game off screen and drives it through the game's own functions. Every command is recorded in a run under `artifacts/<build>/runtime/<run-id>/`. Run it with `bun tools/runtime/lab.mjs <command>` inside `nix develop`. Without a command it lists all of them.

## The lab copy

`prepare` clones the installed Steam build into `BloodlineLab/<build>/` on the bottle's C: drive and installs the pinned UE4SS and the bridge mod. The clone shares the installation's blocks, so it takes no space and seconds. The Steam installation and the player's saves are never touched: `labDirectory` in `bloodline.local.json` must lie outside the Steam library, and every launch passes `-NotInstalled`, which keeps saves and logs in the lab copy.

Run `prepare` again after a game update or after changing `tools/runtime/bridge/main.lua`. It reinstalls UE4SS and the bridge into an existing copy.

## Sessions

- `launch` starts the game headless, with `-nullrhi`. Blueprints, widgets, tooltips and per-frame sampling work, and the machine stays free to use.
- `launch render` draws off screen with `-RenderOffScreen` for screenshots and model renders. The game registers as a background app without a window, so it never takes focus.
- `quit` ends the session through the game's own `QuitGame` and stops only the process the session started.
- `begin <label>` opens a new run in the same session. Repeated measurements go to separate runs.

`launch` also starts `limit-frame-rate`, which holds the frame rate at 60, because the game resets the limit on every level load.

A fresh session sits in the main menu. `new-save <name>` starts a game and creates a character with the default look. `close-tutorials` closes the tutorial popups that pause the game afterwards.

## Driving the game

Commands in `tools/runtime/commands/` change state only through the game's own functions. A command that does what a player could not, such as `give`, `call`, `take-damage`, `set-time` or `set-weather`, says so in its `-- lab setup:` line, and the run records that line with every use.

- Buttons are pressed by calling the function that the widget class binds to the button's click, which `press` reads from the class's `ComponentDelegateBindings`. A CommonUI button's handler takes the button as its argument. Never click by screen position.
- `give` builds each item as an actor, so its construction script fills its item info as for an item lying in the world, and passes that to the player's `AddItemsToInventory`.
- `equip` and `use` click the item's inventory slot, which needs `open-sheet` first. Only slots the sheet shows count: the sheet leaves replaced slots alive, and pressing them does nothing.
- `read-tooltips` calls each shown slot's own `OnMouseEnter`, which builds the tooltip as hovering does, and reads its text blocks. `tooltip-batch` gives a batch of items, reads their tooltips and empties the inventory again.
- `take-damage` deals damage through the player's own `TakeDamage`, by an amount or down to a Health value.
- `player` reads player values by dotted path, such as `PlayerPassiveLevelStatComponent.Total Armor`, with the items held and equipped. `read-widget` reads a widget's text blocks.
- `call` sets inputs on a live object, calls a function and reads outputs, for checking a rule against the game's own result. `params` lists a function's parameters.
- `sample` reads values every frame for a number of game seconds and can run commands at set times, such as eating while Health is sampled.
- `eval <file>` runs a Lua file with the command helpers, for exploring. The run records the code.

Commands that look up objects use the instances the game uses. Class defaults and the templates inside a class, whose outer chain reaches a class, are skipped. Fields of the game's own structs carry GUID suffixes in this build, so commands read them through `field`, which takes plain names. Headless, bound text such as the character sheet's lines keeps its design-time value, so read the values behind it.

## Checks

`tools/runtime/checks/` holds the measurements that settle published facts and known issues, each run on a fresh save with `bash tools/runtime/checks/<name>.sh`. Run each twice, in separate runs, and again after every game update:

- `armor.sh` and `armor-slots.sh` measure which armor values and slots reduce damage and speed.
- `knees.sh` shows when knee armor starts to count.
- `food.sh` samples Health every frame around meals, of four foods with known tables or of the foods whose class paths it is given.

`bun tools/runtime/tooltip-sweep.mjs <build>` reads the tooltip of every published item into the current run. It resumes where earlier sweeps of the build stopped, and retries a batch that stops the game in halves on a restarted game. The next `catalog` compares each item's facts with the latest tooltip a run read for it. A difference without a known cause in `TooltipCheck.cs` appears in Coverage and needs an owner: a fix in the catalog, a known issue once a run reproduces it, or a new cause.

## Captures

- `capture-screen <name>` keeps an engine screenshot with the interface. Add `noui` for the scene alone.
- `capture-model {"item": "<class path>", "name": "<name>"}` renders an item alone, with a transparent background, at a clear noon, in the light of the character sheet's own preview. `openspec/changes/establish-reference-architecture/design.md` records the presentation and why.

Both need `launch render`.

## Troubleshooting

- A request that times out usually hit an error before the bridge loaded. The game's `ue4ss/UE4SS.log` in the lab copy shows it.
- `No live <class>` means the menu or level that holds the widget is not loaded yet. `probe` reports the world, the pawn and whether given classes exist.
- After a game update, `prepare` refuses a build that Steam has not finished installing.

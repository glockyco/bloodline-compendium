# Design

## Context

See proposal.md for motivation. Measurements below come from spikes on Steam build 24464547.

- The game is Unreal Engine 4.27 with one IoStore container of 42,277 packages, Zlib-compressed and unencrypted. Packages use tagged properties, so no AES key or `.usmap` mappings are needed.
- Gameplay is Blueprint. Item classes descend from `BP_BaseItemAsset_C`. Of its 2,161 descendants, 69 are workbenches, containers, fishing spots and other placed interactables under `InteractableObject_C`. Class default objects store only overrides of their parents.
- The game gives items through about 30 mechanisms. Most of them are data: DataTable rows, class defaults, placed actors and dialogue events. Data references reach 1,904 of the 2,092 classes outside `InteractableObject_C`. 33 more are referenced only from Blueprint bytecode, and 155 from nothing found so far, including plausible real items such as `HeadArmor_CelestialKnightHelm_C`.
- The 109 levels include test maps. `DevTestMap` alone places 395 item classes.
- Steam keeps every build. Its app manifest names the installed build and depot manifest, and DepotDownloader downloads any earlier manifest of an owned app.
- Contributors may use macOS, Windows or Linux. CI never has game files.

## Goals / Non-Goals

**Goals:**

- One path from the installed game to a deployed page, rerun after each game update.
- Every named inventory item on the site, with what the game data says about it and where it comes from.
- Uncertain and missing information shown on the site, never guessed and never hidden by withholding the item.

**Non-Goals:**

- Running the game. Map capture and checking formulas in the running game come in later changes.
- Storing extracted data or the game files of earlier builds.
- Deciding which levels players can reach, and sources that exist only in Blueprint bytecode. Both come in later changes.
- Content other than items, and change history pages.
- A shared code base with earlier compendium projects. Their lessons inform this design, their code does not carry over.

## Decisions

### Regenerate extracted data instead of storing it

Extracted data depends only on the extractor's code and the game build. Git keeps the code and Steam keeps every build, so the data can be regenerated at any time and is not versioned. Extraction writes to a Git-ignored directory named by the Steam build ID. After a game update, the previous build's output is still on disk for comparison. The Afallon, Ardenfall and Ancient Kingdoms compendiums and the Erenshor data mining project also keep extracted data out of Git.

Alternatives:

- A private data repository. It versions a cache of reproducible data and adds a second repository to keep in step with the code. It would only pay off for history pages, which can download an earlier build when they are built.
- A public data repository, as data mirrors such as Obelisk and mcmeta keep. Those projects publish data for other tools. This project publishes only its site.

### Read the installed build directly and read-only

The extractor reads the Steam installation named in machine-local configuration. It takes the app ID, build ID and depot manifest IDs from Steam's app manifest, and it fails if the build changes while it runs. It never writes to the installation. A modded copy for running the game comes with the runtime work in a later change.

Alternative: a pinned copy of each build. It protected against updates during work and kept the previous build for comparison. The build check and the previous build's output cover both.

### Read the packages offline with CUE4Parse

CUE4Parse reads every package except one on macOS arm64. The exception is a 2.2 GB level that overflows its loader. Items and all DataTables read in 22 seconds with 1.4 GB of memory.

Alternatives: a runtime scanner needs the game running, under Wine on macOS, for data that is readable from files. A generic dump of every package took 35 minutes, 10 to 18 GB of memory and 20 GB of output.

### One C# program from game files to site data

The extractor and the catalog are one C# program with two steps. `extract` reads packages into records: merged class defaults, DataTable rows, enums, placed actors per level, dialogues and icons. `catalog` reads the records and writes site data: items, sources, coverage gaps and the build label. Records are an internal cache in the output directory, so catalog work reruns in seconds without reading packages again.

Site data is the only contract with the TypeScript site. JSON Schema is exported from the C# site-data types with `JsonSchemaExporter`, TypeScript types are generated from the schema with json-schema-to-typescript, and a check fails when the generated files are stale.

Alternative: a TypeScript catalog over the records. Without stored records, it only adds a second cross-language contract over loosely typed property values.

### Merge inherited defaults explicitly

For each class, walk the parent chain and overlay class default objects from the oldest ancestor to the class itself. Nested structs merge field by field. Arrays, object references and texts replace as a whole. Explicit zero, false, null and empty arrays override. User-defined struct defaults fill the fields that no class sets. Fields that only a native C++ class declares have defaults that the files do not contain, so they stay unknown instead of being guessed.

Placed actors store only overrides of their class defaults. The extractor merges each overridden struct field by field, so a placed value replaces the class value as a whole.

### Leave out objects that do not parse completely

The extractor enables CUE4Parse's fatal serialization errors. An object that fails to parse is left out entirely, and its package and error are recorded as a gap that the coverage page lists. A package that cannot be loaded at all, such as the 2.2 GB level, is a gap in the same way. This change reads no Blueprint bytecode, so script data reading stays off.

### Publish every named inventory item and show what is uncertain

An inventory item is a class under `BP_BaseItemAsset_C`, outside `InteractableObject_C`, with a display name. Every inventory item is published. A missing source is not a reason to withhold an item, because source capture is incomplete. Item pages state when no source, icon or value is known, and a coverage page lists every gap with links, as the Afallon Compendium does.

Alternative: publish only the items that a graph of sources reaches. The graph needs about 30 mechanisms, Blueprint value flow for travel between levels and a review pass after every update. It still misses real items, and then it hides them.

### Read item sources from data

The catalog attaches sources from these mechanisms:

| Mechanism | Data |
|---|---|
| Shops | `DT_AllShops` rows, vendor class stock and placed `Shop Contents`, dialogue shop events, settlement building stock |
| Currency shops | `OpenCustomCurrencyShop` dialogue events with item prices in a currency item |
| Smithing and crafting | `ItemRecipes` rows, joined to `WeaponComponentTable.Class` by row name |
| Cooking | `AllFoodRecipes` rows |
| Container loot | `LootableObject` pools in class defaults and placed overrides, `PresetLootTables_DT` rows |
| Random loot | `AllLootSpawnRates` rows |
| Enemy drops | `Loot Table` and `Rare Pickup Loot` in NPC class defaults and placed overrides |
| Pickups | placed inventory item actors and the item class of pickup objects |
| Dialogue grants | `AddItemDialogue` events |
| Fishing | `Fishing_Minigame` pools and placed `FishingSpot` overrides |
| Gathering | `HarvestableInteractionObject` resource pools and gather point tables |
| Farming | `SeedDataTable` harvest pools |
| Bounty notes | bounty note classes in `DT_UQC_Quests` |
| Settlement deliveries | loot tables in `BuildingResourceAndIncomeValues` |

Each source keeps consumed inputs apart from the item it gives, and it shows quantities, prices and raw weights as the data states them. Weights become percentages only where the selection rule is known. Some data looks like a source and is not one. `AllLootPresets` only describes how selected loot looks, the dialogue event `GiveItems` removes items, and a shop's currency, a recipe's ingredients and a planted seed are inputs. A null entry in a loot pool is a chance of no drop.

### Reviewed exclusions for known non-content

A short list in the repository names content that is not part of the game, each entry with its reason and evidence. Placements in an excluded level are not sources. The first entries are the test levels `DevTestMap`, `OverworldTest1` and `Overworld_working`. The catalog fails when an entry names a level or class that the build lacks, so stale entries do not linger. Class and level paths may appear in the repository. Records, icons and other extracted files may not.

### Icons

The extractor decodes the first mip of each item's icon texture (DXT1, DXT5 or uncompressed BGRA) in managed code and writes it as PNG named by a hash of its decoded pixels. Compressed PNG bytes can differ between platforms, and the pixel hash keeps records identical anyway. The site build converts icons to WebP thumbnails. A spike decoded 30 of 30 sampled icons. The extractor does not use CUE4Parse-Conversion, because its pinned dependencies carry security advisories that fail an audited restore.

### Static site on Cloudflare Workers static assets

SvelteKit 3 prerenders every page with the static adapter, and server-only loads keep site data out of browser JavaScript. Pagefind indexes the output for search without a server. It matches the start of words, not arbitrary substrings. Wrangler deploys the output as an assets-only Worker with a prerendered 404 page. The paid plan allows 100,000 files and 25 MiB per file, and the build fails before either limit. A spike built and indexed 2,000 item pages in under ten seconds, producing about 6,000 files.

### Tooling

- A Nix flake supplies one development shell for macOS and Linux. It overrides nixpkgs' .NET SDK 10.0.401 with 10.0.403 and runtime 10.0.14, because runtimes up to 10.0.12 are affected by CVE-2026-96231 and SDK 10.0.401 by CVE-2026-70165 on Windows. Windows contributors install the versions named in `global.json` and `package.json`.
- C#: central package management with locked restore, nullable references, SDK analyzers with warnings as errors, CSharpier, and xUnit v3 on Microsoft.Testing.Platform.
- Site: ESLint with eslint-plugin-svelte, Prettier, svelte-check, Vitest and Playwright.
- GitHub Actions runs every check inside the Nix shell on invented fixtures, with SHA-pinned actions and no secrets. Renovate opens grouped updates. CUE4Parse updates are reviewed by hand, because a parser change can change site data.
- A check fails when a game file or extracted output is tracked by Git.

### Minimal agent instructions

`AGENTS.md` holds only boundaries that an agent cannot infer from the code. Procedures become skills only once a workflow exists and has been repeated. Measured studies of repository instruction files show no reliable benefit from long, generated briefings and about 20% higher cost.

## Risks / Trade-offs

- CUE4Parse can change its output between versions. Pin it, and compare site data before and after a parser update.
- Items that only Blueprint logic grants show no source. The coverage page lists them until a later change reads bytecode.
- Sources can name levels that players cannot reach. Known test levels are excluded, and a later change checks reachability through the game's travel data.
- Steam may stop serving an old build, so an earlier publication could not be rebuilt exactly. The site describes only the current build.
- Steam can update the game during extraction. The build check fails the run.
- Extracted files could be committed by accident. The output directory is ignored, and a check fails on tracked game files and output.
- Contributors on Windows see different line endings and path separators. Output uses LF and forward slashes on every platform.
- Early Access updates rename or remove item classes. Item identity is the class path, so a removed class disappears from the next deployment.

## Migration Plan

The first deployment goes to a preview URL. The custom domain `bloodline.compendiums.org` is attached after review. Rollback redeploys the previous version with Wrangler.

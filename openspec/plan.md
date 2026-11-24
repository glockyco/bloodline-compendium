# Plan

Every feature the compendium will grow, in the order it is built, with what each needs from the others. Each item becomes its own OpenSpec change with its own specs when it is next or when it constrains work in progress. A change's `design.md` holds its findings and decisions. This file holds only the order and what crosses changes.

## Order

1. Ship the item reference: `curate-item-reference` and `publish-item-reference`.
   - Every published known issue is reproduced in the rendered game, by what the player sees and by its effect.
   - The site moves to `the-bloodline.compendiums.org`, and `bloodline.compendiums.org` redirects to it.
   - The footer links to a GitHub issue form for wrong facts.
2. Item model renders on item pages and in link previews, from the `capture-model` command and the presentation in `establish-reference-architecture`'s "Presenting item models".
   - Renders on both bodies where the female meshes differ.
   - Bows and shields, whose views are not settled.
   - A review step before renders are published.
   - Creatures and people get renders with their own pages.
3. Disciplines, skills and the character planner: `add-skill-reference`, then `add-skill-planner`.
   - The reference first moves the catalog and the site from items to kinds: one producer per kind, sources for any entity, and lists, pages, tooltips, claims and coverage that work across kinds. Item output stays unchanged through that move.
   - It then publishes the 21 disciplines with their trees laid out as the skill window shows them, every skill the game defines with where it comes from, and passives and spellsongs on their discipline's page.
   - The planner is the first part of a character planner built from sections: a summary column that stays in view with the totals, points left and broken rules, and a tab per section with its own address. Step 3 ships the Attributes, Skills and Skill Bars sections, with a discipline's skills and passives side by side as the game's tree window shows them. Saved characters and shared links hold one part per section, so later sections join old links. Builds are shared as links that fit a Discord message and as text, and characters are kept in the browser.
   - Gathering resources, such as Birch Tree and Coal Deposit, get pages with their loot, the harvests a spot gives, experience, the goblin ambush and their areas with how many spots each has and how likely they are. Item pages keep one short row per resource that links to its page.
4. Crafting and equipment: `add-crafting-reference`, then `add-equipment-planner`.
   - The crafting reference comes first, because a crafted weapon's damage, speed and value depend on the quality each copy was made at, so the planner can hold crafted gear only once those rules are verified in the lab.
   - The reference publishes weapon assembly from parts and quality, armorsmithing, runes, attunement and enchanting, how recipes are learned and what ingredient treatments do in meals and potions, with a weapon builder and armor arithmetic on armor pages. Crafting stations stay named where recipes and professions use them, without pages of their own.
   - The planner then adds Equipment with the 13 worn and held slots, found and crafted gear and the Light, Medium and Heavy Armor levels walking speed needs, and the Item Bar, adding armor and walking speed to the summary's totals. A build saves the chosen pieces, not the order they went on.
5. Places and people: towns, regions, dungeons and settlement buildings, and the merchants, trainers, quest givers and conversation partners in them.
   - Places also list what is placed in them for progression: the 34 skill books, the 11 spellsong note sheets, and the blood pool ritual sites and necromancer shrines that grant Blood Magic and Necromancy points. Skill and spellsong sources name these places as unlinked text until then.
   - The settlement Blacksmith's stock drawn from its item pool as an item source, with its chance per restock, verified by the Knight's Broadsword.
   - The default stock of faction currency shops as an item source, verified by the Lionsmane Cuirass for 350 Iuba.
   - A respect control for city prices, remembered by the browser, verified by respect 750 in Elderglen.
6. Creatures and loot, with drop tables and spawn odds.
   - No player skill from the skills table is referenced by an enemy, so creature abilities need their own reading.
   - The weapon each enemy sets as its rare drop on construction and the finds Fenraen digs up as item sources, verified by a Bandit Cutter's weapon drop.
7. Quests, achievements and guilds with their ranks, and the standing systems players ask about: settlement levels, buildings and income, guild ranks and what they unlock, town respect, crimes and bounties.
   - Console commands and the IDs they take, which players ask for on Discord, such as the console name of Atacamite Ore or "CompleteQuestUnveiled1" to finish a quest stage. The game's console (`Console_Custom`) passes the text to `FirstPersonGameMode:Recieve Console Command`, which reads `AllLootSpawnRates` and `DT_UQC_Quests` and can add items, accept quests, advance quest stages, add skill points and experience, start calendar events and give diseases. A lab run lists every command with its syntax first. A Console Commands page then lists them all, since commands such as calendar events have no detail page, and detail pages show their ID and command in a quiet row that links to it, as ARK's and UESP's wikis do. Quest stage commands sit behind a closed disclosure, because stage names reveal what a quest asks next. An item the console's item command cannot give says so on its page, so players stop guessing names, because the command knows only the items in `AllLootSpawnRates` and the developer confirmed that many items are missing from it. Item IDs may come earlier if the run finds them simple.
8. The interactive map from level captures and placements, linked both ways with the pages of places, people, creatures and resources. Skill books, note sheets and point shrines are map markers.
9. Mounts, companions, diseases, calendar events and the arena.
   - The planner's companion and mount choices, which the character sheet picks through its own companion and mount controls.
   - The planner's Effects section: food and potion effects, crafted potions, and how stat crystals and vampirism change a character, once these pages explain how each works.
   - Vampirism as a disease grants Feed and opens the Vampirism tree's purpose, which the skill reference publishes without explaining.
   - Stables, contract scroll pools, seasonal gifts, archaeology rewards and level reward variables as item sources, verified by Brown Horse, Meteor Storm, Holly Harpoon, a fossil and Feral Maw.
   - Seasonal state from the reader's date, verified by a Hallowed Eve item in October and in March.
10. Mechanics guides with calculators on the shared rules: experience, armor, crafting quality, settlement income, farming, and the attribute and discipline curves the planner measures.
11. Reading a save in the browser, to show progress and to start a planned character from it. The player keeps unlocked skills in `UnlockedSkills` and passive ranks on each passive button, which a save reader needs to map.

Renders come first because they make the item pages and their link previews show what each item looks like, which no other source offers. Skills and the planner come before places and people, because the planner offers what the official wiki cannot and exercises the rules module early, and because moving the catalog to kinds with the second kind shapes the structure every later kind uses. Item, skill and spellsong sources keep naming merchants and places as unlinked references until places and people are published.

Later, without a place in the order yet:
- A page per game update listing the facts it changed, linked to the patch notes. The project context line "The site describes only the current game build" needs revising first.
- Visitor analytics. The tool and what it may record are decided when this is scheduled.
- Link previews for each shared build. A build link keeps the build in the query so that a Worker in front of the static site can later write each build's title and summary into the planner's preview tags. A generated image per build needs a decision on its cost, because rendering images in a Worker is likely to exceed the free plan's CPU time. Until then the planner has one fixed preview.

Not planned:
- A combat simulator.
- Mods for players, because they need UE4SS and break with Early Access updates.
- A Discord bot, because link previews need none and a bot would need the developer's agreement.
- An automated watch for game updates. Updates are noticed by hand and then run through the update workflow in `README.md`.

## What crosses changes

### Kinds with pages

When a kind gets pages, references to it already in the graph become links and sitemap entries without changes to existing pages. `curate-item-reference` built one tooltip presenter, search entries per kind and the review of entities that share a name. `add-skill-reference` moves the catalog and the site onto kinds, as its design describes. Every later kind adds a producer, its sources, its page and its tooltip to that structure, and changes the structure when a kind does not fit it instead of working around it.

Place names come first among the later kinds, because places, people, the map and the sources of items, skills and spellsongs all show them.

### Reading the game

- Data that the game defines in its interface lives in widget trees, such as the skill slots and passives of the skill trees. Extraction records the Blueprint widgets of every widget tree. Look there when class defaults leave a value empty.
- The game ships assets that no player reaches, such as 21 older skill trees beside the 21 the skill window opens. Whether something is reachable follows from the code that opens or grants it, never from its name or folder.
- Some sources are events in code, such as becoming a vampire granting Feed. Their reader text is written once per cited function and checked against the function's fingerprint, as facts are.

### Navigation

The header lists Items alone. `add-skill-reference` adds a Browse menu after Afallon's, with entries read from the kind registry.

### Sharing and link previews

- Every page has a title, a description and an image for link previews. Item renders replace the category art in previews once they exist.
- A shared build travels in the query, which a server receives, so that per-build previews can be added later without changing the link format. Until then the planner has one fixed preview, as "Later" describes.
- A build link fits in a Discord message of 2,000 characters with room for a comment.

### Delivery budgets

Each route has a payload budget that its change sets and a check enforces. Planner data loads only on the planner route.

### Game updates and languages

- A game update reruns extraction, the catalog, the rules checks and every known issue's check, `tools/runtime/checks/issues.sh`, as the README describes.
- The developer plans translations. The update that ships them may move item names and descriptions into string tables, and the catalog then needs to read the English table.

### Known issues

A defect is published or reported only after the running game shows it, observed as the runtime-checks skill describes under "What an observation must show". Reporting to the developer needs a request.

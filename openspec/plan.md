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
3. Disciplines, skills and the skill planner: `add-skills-and-planner`.
   - Pages for disciplines, skills, passives and spellsongs.
   - The planner's first part: levels, unlocked skills, passive ranks, points, characters kept in the browser.
   - Sharing builds: build links that fit a Discord message, a preview card for each shared build, and a copyable text summary in the form players already post.
4. Crafting and equipment: `add-crafting-and-equipment`.
   - Smithing with weapon parts and quality, and a weapon builder.
   - Armorsmithing, runes, attunement and enchanting.
   - Crafting stations, cooking with ingredient treatments, farming and recipe learning.
   - The planner's second part: equipment, crafted weapons, runes and enchantments with armor and mobility totals.
5. Places and people: towns, regions, dungeons and settlement buildings, and the merchants, trainers, quest givers and conversation partners in them.
   - The settlement Blacksmith's stock drawn from its item pool as an item source, with its chance per restock, verified by the Knight's Broadsword.
   - The default stock of faction currency shops as an item source, verified by the Lionsmane Cuirass for 350 Iuba.
   - A respect control for city prices, remembered by the browser, verified by respect 750 in Elderglen.
6. Creatures and loot, with drop tables and spawn odds.
   - The weapon each enemy sets as its rare drop on construction and the finds Fenraen digs up as item sources, verified by a Bandit Cutter's weapon drop.
7. Quests, achievements and guilds with their ranks.
8. The interactive map from level captures and placements, linked both ways with the pages of places, people, creatures and resources.
9. Mounts, companions, diseases, calendar events and the arena.
   - Stables, contract scroll pools, seasonal gifts, archaeology rewards and level reward variables as item sources, verified by Brown Horse, Meteor Storm, Holly Harpoon, a fossil and Feral Maw.
   - Seasonal state from the reader's date, verified by a Hallowed Eve item in October and in March.
10. Mechanics guides with calculators on the shared rules: experience, armor, crafting quality, settlement income and farming.
11. Reading a save in the browser, to show progress and to start a planned character from it.

Renders come first because they make the item pages and their link previews show what each item looks like, which no other source offers. Skills and the planner come before places and people, because the planner offers what the official wiki cannot and exercises the rules module early. Item sources keep naming merchants and places as unlinked references until places and people are published.

Later, without a place in the order yet:
- A page per game update listing the facts it changed, linked to the patch notes. The project context line "The site describes only the current game build" needs revising first.
- Visitor analytics. The tool and what it may record are decided when this is scheduled.

Not planned:
- A combat simulator.
- Mods for players, because they need UE4SS and break with Early Access updates.
- A Discord bot, because link previews need none and a bot would need the developer's agreement.
- An automated watch for game updates. Updates are noticed by hand and then run through the update workflow in `README.md`.

## What crosses changes

### Kinds with pages

When a kind gets pages, references to it already in the graph become links and sitemap entries without changes to existing pages. Three things must exist before the first new kind ships, and `curate-item-reference` builds them:
- one tooltip presenter that picks each kind's tooltip, with the item tooltip as its first kind,
- search entries for every kind with pages,
- a review of entities that share a name, such as the 12 merchants named Shopkeeper or the levels that make up one town, so that one thing a player meets gets one page.

Place names come first among these, because places, people, the map and item sources all show them.

### Navigation

The header lists Items alone. The change that adds the second kind with pages adds a Browse menu after Afallon's.

### Sharing and link previews

- Every page has a title, a description and an image for link previews. Item renders replace the category art in previews once they exist.
- A shared build travels in the part of the address that a server receives, because Discord builds its preview from the HTML the server returns. A Worker in front of the static site writes each build's preview tags and image. The planner change decides the address form.
- A build link fits in a Discord message of 2,000 characters with room for a comment.

### Delivery budgets

Each route has a payload budget that its change sets and a check enforces. Planner data loads only on the planner route.

### Game updates and languages

- A game update reruns extraction, the catalog, the rules checks and every known issue's check, `tools/runtime/checks/issues.sh`, as the README describes.
- The developer plans translations. The update that ships them may move item names and descriptions into string tables, and the catalog then needs to read the English table.

### Known issues

A defect is published or reported only after the running game shows it, observed as the runtime-checks skill describes under "What an observation must show". Reporting to the developer needs a request.

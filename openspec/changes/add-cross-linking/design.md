# Design

## Context

See proposal.md for why. Today `LinkedText.Unlinked` checks known issue and mechanic text against discipline, profession and mechanic names only. `EntityLink` renders a reference to a kind without pages as a `span`, and `previewKinds` in `site/src/lib/entity/previews.ts` lists the five kinds with previews by hand. The publication gate already reads every built page for relation IDs, so it can read their text too.

## Goals / Non-Goals

Goals: a check that sees every name a reader sees, counts that can only fall, and a page and preview for every kind readers meet.

Non-goals: linking names inside the game's own words, such as item descriptions and quoted dialogue, because the game does not link them and a link would change the quotation. Every mention need not be linked: one link per row or paragraph is enough.

## Decisions

### The unlinked-name check reads the built pages

The check runs over the built HTML of a real-data build, not over catalog strings. It sees text from every producer, including Svelte templates, section intros and labels, which a catalog-side check cannot reach. The names are those of every entity any page shows: `EntityLink` marks a name without a page with its kind, and a link's kind comes from its address. It skips text inside a link, inside game text (`[data-game]`) and the page's own name. A name matches as a whole phrase, longest names first, so Iron Ingot does not also match Iron. Only the kind of a name without a page is an attribute, because the attribute on every link pushed the largest item page over its payload budget.

Alternative: extend `LinkedText.Unlinked` to every catalog text. It misses site-written text, which is where the section intros and labels live.

### Counts live in a recorded file

`coverage/link-coverage.json` holds the plain-text count per kind and the unlinked mention count per kind. Names stay out of the repository, because they come from the game's files: each build writes them to `artifacts/link-coverage.json`, and a rise names what is new against the previous build's list there. A rise fails, and a fall fails with the instruction to lower the recorded value. Fixture builds skip the comparison, because their invented data does not match the record.

Alternative: a report without a gate. That is how the gaps grew.

### Wanted names are marked in text

Text marks a name that has no entity yet as `[Divine Bond](wanted:divine-bond)`. The site renders it as plain text, and the catalog lists it with the pages that use it. When an entity with that name lands, the catalog fails on the `wanted:` target until the text links the real kind, so no wanted mark outlives its page.

### Previews come from the kind registry

The kind registry gains whether a kind has a preview, and `previewKinds` reads it instead of its own list. The entity graph gate fails for a kind with pages and no preview. A new kind's preview is its tooltip component and its preview data, as `TooltipPresenter.svelte` and `server/previews.ts` define them today.

### One entity name style

`EntityLink` drops the rule that removes the underline from links with an icon. A name without a page renders with a class that takes its place's text colour. Row names that are links keep the row's weight. The layout check in `site/tests/layout.ts` gains the style rule from the presentation spec, so the real-data review pages enforce it.

### Order of page kinds

Creatures first, because they are the most named kind after recipes and their pages give places and effects their first links. Then places, effects, people and merchants, quests, containers and settlement buildings. Recipes come last and may become anchored entries on the pages of what they make, as passives are on discipline pages, which the recipe comparison decides.

### Comparisons check links and previews

`.omp/agents/comparison-check.md` gains the rule that every name in a comparison's screenshots that has a page or a wanted mark is linked, and that a new kind shows a hover screenshot of its preview.

### Place pages describe the place

A place page lists what is in the place by kind of thing, as Ancient Kingdoms and Afallon list a zone's creatures, people, chests, events and services, instead of one table per way to get an item. Each thing is one card of a name, what it does there and its items as icons, so later kinds such as services and quests join as sections in the same form. A finder narrows the cards to what gives an item. Calendar events come from the calendar's area names, matched to places without a leading "The", apostrophes or case.

Alternative: the item-source tables of the first place pages. They answered where to get an item but not what is in a place.

## Risks / Trade-offs

- [A common word matches an entity name, such as Stone in a sentence] → a one-word name at the start of a sentence and a name joined to another capitalized word are skipped. The first real-data run reported no such case, so no list of skipped names exists. If one appears, a reviewed list beside the record is the next step.
- [The first gate run reports thousands of names] → the recorded file starts at today's counts, so the first run passes and each later change only answers for what it adds.
- [Pages for eight new kinds take many changes] → each kind is a group of tasks with its own comparison, and the ratchet keeps the gains of each.

## Where findings live

- Decisions on each page kind: the reader-text spec of this change, as requirements, and the comparison files under `artifacts/design/process/trial/`.
- Names the check reports: `artifacts/link-coverage.json` after each real-data build.
- Open work: `tasks.md` of this change.

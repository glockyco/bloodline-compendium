---
name: page-design
description: Design or change what a page, section, card, tooltip or preview of The Bloodline Compendium shows and how it is laid out. Use before writing site code for any visible change, to decide whether it applies an approved pattern, chooses within one or needs research and options first, and again when an answer rejects a design.
---

# Page design

How much process a visible change needs depends on how much of it is already decided. Work that out first from what you can check in the repository, and say it in one line before you start, with what the case means and the fact it rests on, such as "Explore, research and options first: the site has no approved way to picture a creature." The line lets the owner correct the amount of work before any of it is spent.

| Case | It holds when | What to do |
| --- | --- | --- |
| Apply | Nothing is left to choose. The owner named the exact change, such as "put this section above that one", or an approved pattern covers this exact thing and you can name it: a requirement in `openspec/`, an accepted comparison in `artifacts/design/process/trial/`, or a component that other pages use for the same job. | Build it with the existing parts. No board and no comparison. Inspect the page at both widths, have `visual-review` compare it with its siblings, and commit. |
| Choose | The way to show the thing is settled and a choice inside it is open: a wording, a label, an order, a count, or which of two forms the site already uses. A goal from the owner, such as "these two should look like they belong together", is Choose, because it leaves open how. | One comparison with two or three options built from what the site already has. No board. See "Choose" below. |
| Explore | The site shows this kind of thing for the first time, such as a new kind of page, section, card, picture, chart or control, or an answer rejected a design for it. | All four stages below. |

When you cannot name the pattern or the earlier decision, the case is not Apply. When an option needs a form the site does not have yet, the case is not Choose. When two cases still fit, take the heavier one for something players see. A defect, such as a short last line or a wrong alignment, is Apply when a sibling page or a shared component already does it right.

## Choose: one comparison

A choice of wording alone follows `compare-options` and nothing else here. It has no evidence file, and `scripts/check-comparison.mjs` then checks only its question card and screenshots. For a choice that changes layout, build each option as stage 3 describes and write the evidence file in this short form:

```markdown
# Evidence for <decision>

Case: Choose. <the settled way to show the thing, and where it was decided or is used>

## Player questions

- <what a player wants to know or do here, in their words>

## Options

- A: <the form it uses, with the path of the component or page that already has it, such as `site/src/lib/ui/CardGrid.svelte`>
- B: <the same>

## Earlier decisions

- <a decision in `openspec/` or an earlier comparison that applies, and what transfers>
```

Then follow stage 4.

## Explore: four stages

A new design goes through four stages in this order. Each stage ends in a file, and the next stage starts from that file. No site code for the design exists before stage 3, because a design that is already built gets asked as "ship this?" and then patched answer by answer.

The `compare-options` skill gives the rules of a comparison. This skill adds what this project does before and around it. A change of wording alone needs only `compare-options`.

## 1. Board and census

Start the `design-research` agent with the topic, such as "creature pages", and the questions a player brings to the page. It writes `artifacts/design/references/<topic>/board.md`: how this site, the sibling compendiums, large wikis and the game itself show the thing, each with a screenshot, then the assets the catalog has and the patterns worth an option. A topic keeps one board. When it exists, the agent extends it.

While it runs, survey what the game holds for this kind of thing, so the design starts from all of it and not from the fields someone remembered. Draft nothing for the design yet.

1. Take the census of the kind's classes:

   ```sh
   bun scripts/census.mjs artifacts/<build>/records --descendants-of <class path> > artifacts/design/references/<topic>/census.md
   ```

   The class is the ancestor of every thing of this kind, which the kind's producer under `src/Bloodline/Catalog/` names. The census lists every field that differs between the classes, every setting of the game's components on them, and every function they define. Read it whole.

2. Write `artifacts/design/references/<topic>/data.md` beside it, with one row for each thing a player could learn:

   ```markdown
   # Data: <topic>

   [Census](census.md)

   | A player learns | Census names | Known from | On the page |
   | --- | --- | --- | --- |
   | <such as how hard its melee hits> | <fields and functions, separated by commas> | <Read: a function, Seen: a lab run, or Name only> | <Yes, Later: a task, or No: a reason> |
   ```

   Every function of the census and every field that a function reads goes into one row. Group names that tell a player the same thing. Looks, sounds and animation share a row with "No". A field that no function reads still gets a row when its values mean something to a player, such as a family tag.

3. Fill "Known from" honestly. `Read: <function>` holds after you read the function with `nix develop -c python3 tools/script-view.py <package> [function]`, which the README describes. `Seen: <lab run>` holds after a lab run showed it. Everything else is `Name only`, and a row known by name only never goes on the page, because a name is intent and not behaviour.

4. Fill "On the page". `Yes` means the page of this kind shows it. `Later: <task number>` is for what needs work the design does not depend on, such as renders, and names its task in the active change's `tasks.md`. `No: <reason>` gives a reason a player would accept. Unread code is never a reason for `Later` or `No`: read or check now whatever a player question asks about, such as attacks, abilities and defences on a creature page.

5. Run `bun scripts/check-comparison.mjs --data <data.md>` and fill every gap it prints.

A topic keeps one census and one data file. Take the census again after a game update. A kind whose records are not classes, such as dialogues, has no census: start `data.md` with a line "No census: <reason>" and fill its rows from ten of its records read whole.

When the agent returns, read the board and open every screenshot yourself.

## 2. Brief

Write the evidence file `artifacts/design/process/trial/<number>-<topic>-evidence.md` with these sections, which `scripts/check-comparison.mjs` reads:

```markdown
# Evidence for <decision>

Case: Explore. <why nothing approved covers this, or the answer that rejected the last design>

## Player questions

- <what a player wants to know or do on this page, in their words>

## Board

[Board](../../references/<topic>/board.md)

## Data

[Data](../../references/<topic>/data.md)

## Inventory

| Pages of this kind show | Who shows it | Catalog has it | In this change |
| --- | --- | --- | --- |
| <a thing, such as a portrait> | <board entries> | <Yes, No, or what it takes> | <Yes, or No with the reason> |

## In the game

<How the game shows the thing, with a screenshot or a lab run. When it shows nothing of the kind, a line that starts "The game does not show this" with what you checked.>

## Options

- A: <what it borrows, naming board entries by their titles, and what it changes>
- B: <the same>

## Earlier decisions

- <a decision in `openspec/` or an earlier comparison that applies, and what transfers>
```

The inventory lists everything the board's pages show for this kind of page, not only what the catalog has today. A row with "No" in the last column needs a reason a player would accept. When the reason is missing data or a missing asset, add the work to the active change's `tasks.md`, so the page gets it later. Compare it with `data.md`: what other pages show and the game holds is "Yes" in both.

## 3. Options

Draft two to four options that differ in structure, such as a table by level, a calculator for the player's own level and a card of fixed figures. Each starts from a pattern of the board. Draft them yourself, as `compare-options` says.

Every option lays out the whole page: each row that `data.md` or the inventory marks "Yes" has its place in every option, and the screenshots show it there, because a section designed alone leaves the rest without a place. Name what is marked "Later" in one line of the review file, with its tasks, so the choice is made knowing what is still to come.

Build each option only as far as its screenshots need, as its own component beside the page, such as `CreatureStats.option-a.svelte`, and let the page pick one with `?option=a` while the comparison is open. The catalog data the options share may be produced first, because facts do not depend on the choice. Previews, links from other pages, tests and fixtures wait for the answer. Every option renders the same real data on the real page at 1440 and 390 pixels, on at least two pages that differ, such as a short and a long one.

Write the review file as `compare-options` describes, with each option's letter at the start of its line and of its screenshot rows. A question asks which option to use. It never asks whether one design should ship.

## 4. Check, ask, build

1. Run `bun scripts/check-comparison.mjs <review file>` and fill every gap it prints.
2. Start the `comparison-check` agent with the review file's path and nothing else. Fix what it reports and run both again.
3. Open the review file with `plannotator annotate <review file>`, without `--gate`, and stop.
4. After the answer, finish the chosen option in the page, delete the other option files and the `?option=` switch, and have the `visual-review` agent compare the page with its siblings.
5. Record the decision as `compare-options` says, then commit.

To show finished work for approval, such as the built page after step 4, open its screenshots with `plannotator annotate --gate`. That is a sign-off, not a comparison, and it needs no question card. Never open a comparison with `--gate`, because its answer is a choice and not an approval.

## After a rejection

An answer that rejects every option, or that calls the design plain, uninspired, narrow or its own thing, says the board or the inventory was too thin. An answer that asks where something is, or that misses information, says `data.md` was too thin or its rows were dismissed too easily. Do not adjust the rejected design. Go back to stage 1 with the owner's words as questions for the board and the data file, then write new options. Quote those words in the evidence file under Earlier decisions and say how each new option answers them.

An answer that names one defect in an otherwise accepted option is different: fix it, look for the same defect elsewhere on the page, and keep the rest.

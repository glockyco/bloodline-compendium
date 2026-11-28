---
name: page-design
description: Design or change what a page, section, card, tooltip or preview of The Bloodline Compendium shows and how it is laid out. Use before writing site code for a new kind of page, a new section, a new card form or a changed layout, and again when an answer rejects a design.
---

# Page design

A visible design goes through four stages in this order. Each stage ends in a file, and the next stage starts from that file. No site code for the design exists before stage 3, because a design that is already built gets asked as "ship this?" and then patched answer by answer.

The `compare-options` skill gives the rules of a comparison. This skill adds what this project does before and around it. A change of wording alone needs only `compare-options`.

## 1. Board

Start the `design-research` agent with the topic, such as "creature pages", and the questions a player brings to the page. It writes `artifacts/design/references/<topic>/board.md`: how this site, the sibling compendiums, large wikis and the game itself show the thing, each with a screenshot, then the data and assets the catalog has and the patterns worth an option. A topic keeps one board. When it exists, the agent extends it.

Do other work while it runs, but draft nothing for this design. When it returns, read the board and open every screenshot yourself.

## 2. Brief

Write the evidence file `artifacts/design/process/trial/<number>-<topic>-evidence.md` with these sections, which `scripts/check-comparison.mjs` reads:

```markdown
# Evidence for <decision>

## Player questions

- <what a player wants to know or do on this page, in their words>

## Board

[Board](../../references/<topic>/board.md)

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

The inventory lists everything the board's pages show for this kind of page, not only what the catalog has today. A row with "No" in the last column needs a reason a player would accept. When the reason is missing data or a missing asset, add the work to the active change's `tasks.md`, so the page gets it later.

## 3. Options

Draft two to four options that differ in structure, such as a table by level, a calculator for the player's own level and a card of fixed figures. Each starts from a pattern of the board. Draft them yourself, as `compare-options` says.

Build each option only as far as its screenshots need, as its own component beside the page, such as `CreatureStats.option-a.svelte`, and let the page pick one with `?option=a` while the comparison is open. Every option renders the same real data on the real page at 1440 and 390 pixels, on at least two pages that differ, such as a short and a long one.

Write the review file as `compare-options` describes, with each option's letter at the start of its line and of its screenshot rows. A question asks which option to use. It never asks whether one design should ship.

## 4. Check, ask, build

1. Run `bun scripts/check-comparison.mjs <review file>` and fill every gap it prints.
2. Start the `comparison-check` agent with the review file's path and nothing else. Fix what it reports and run both again.
3. Open the review file in Plannotator and stop.
4. After the answer, finish the chosen option in the page, delete the other option files and the `?option=` switch, and have the `visual-review` agent compare the page with its siblings.
5. Record the decision as `compare-options` says, then commit.

To show finished work for approval, such as the built page after step 4, open its screenshots with `plannotator annotate --gate`. That is a sign-off, not a comparison, and it needs no question card.

## After a rejection

An answer that rejects every option, or that calls the design plain, uninspired, narrow or its own thing, says the board or the inventory was too thin. Do not adjust the rejected design. Go back to stage 1 with the owner's words as questions for the board, then write new options. Quote those words in the evidence file under Earlier decisions and say how each new option answers them.

An answer that names one defect in an otherwise accepted option is different: fix it, look for the same defect elsewhere on the page, and keep the rest.

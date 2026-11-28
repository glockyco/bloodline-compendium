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

## 1. Board

Start the `design-research` agent with the topic, such as "creature pages", and the questions a player brings to the page. It writes `artifacts/design/references/<topic>/board.md`: how this site, the sibling compendiums, large wikis and the game itself show the thing, each with a screenshot, then the data and assets the catalog has and the patterns worth an option. A topic keeps one board. When it exists, the agent extends it.

Do other work while it runs, but draft nothing for this design. When it returns, read the board and open every screenshot yourself.

## 2. Brief

Write the evidence file `artifacts/design/process/trial/<number>-<topic>-evidence.md` with these sections, which `scripts/check-comparison.mjs` reads:

```markdown
# Evidence for <decision>

Case: Explore. <why nothing approved covers this, or the answer that rejected the last design>

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

An answer that rejects every option, or that calls the design plain, uninspired, narrow or its own thing, says the board or the inventory was too thin. Do not adjust the rejected design. Go back to stage 1 with the owner's words as questions for the board, then write new options. Quote those words in the evidence file under Earlier decisions and say how each new option answers them.

An answer that names one defect in an otherwise accepted option is different: fix it, look for the same defect elsewhere on the page, and keep the rest.

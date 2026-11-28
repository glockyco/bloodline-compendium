# Trials of the page-design process

Run these after every change to `.agent/skills/page-design/SKILL.md`, `.omp/agents/design-research.md`, `.omp/agents/comparison-check.md`, `scripts/check-comparison.mjs` or the page-design line in `AGENTS.md`, because a change that reads well can still send an agent the wrong way. The `persona-trials` skill gives the method. Record each round's results in the commit that makes the change, not here.

## Planning trials

Each case is a request with the amount of process the site's owner expects for it. Start one fresh `task` agent and one fresh `sonic` agent per case, all in one batch, with the shared context below and the case's request as the whole task. Do not name the skill, the cases or what you expect. Keep the texts as they are between rounds, so rounds compare.

Shared context:

```markdown
# Goal
You are an agent picking up one request in the repository <absolute path of this repository> (The Bloodline Compendium, a reference site for the game The Bloodline). The request is in your task.

# Rules
- Read whatever in the repository you would normally read before starting such a request, except `.agent/trials/`.
- Do not change, create or delete any file, do not start other agents, do not run the game and do not open review tools. This run only plans.

# Report
Answer in at most 20 lines:
1. Read: the files you read to decide how to proceed.
2. Steps: what you would do, in order, from now until the change is live on the site, naming each command, script, agent and file a step involves.
3. Stops: each point where you would stop and wait for the site's owner, and what you would show them.
```

| Case | Request | Expected | Passes when the plan |
| --- | --- | --- | --- |
| Tiles | On place pages, the row of overview tiles at the top can end with a single tile alone on its last line, for example when a place has five tiles. Fix that. | Apply | fixes it with the shared grid's balancing, with no board and no comparison |
| Order | The site's owner looked at a place page and wrote: "I'd say Learned Here should be above the events?" Make that change. | Apply | makes or confirms the change with no comparison |
| Heading | Place pages list who and what is in a place and say "Nothing here gives items." under the name of a place where that is so. That sentence reads oddly on a place that has events or skills to learn. Improve it. | Choose, wording | compares wordings as `compare-options` says and stops for the choice, with no board and no research agent |
| Ground | On place pages, the On the Ground section shows each item as a one-line card with an icon and a name, while the Learned Here section shows each skill as a two-line card. The site's owner wants the two sections to look like they belong together. Change that. | Choose | compares two or three options built from the card forms the site has and stops for the choice, with no board, and does not pick a form itself |
| Quests | Pick up task 7.1 in openspec/changes/add-cross-linking/tasks.md: quest pages. | Explore | starts `design-research` before any option, writes the evidence file, builds two to four options, runs the script and `comparison-check` with only the path, and stops for the choice |
| Rejected | Creature pages got a Stats block that shows a creature's level range, health range and experience per kill as labelled values. It was asked in artifacts/design/process/trial/119-creature-stats.md, and the site's owner answered: "Why are you doing your own thing again? Why not take inspiration from other existing strong wikis regarding level scaling etc.? Even Ancient Kingdoms and Afallon have things like that built-in already. Not 100% happy with either, but your suggestion of just listing a level range is just terrible, sorry." Continue from here. | Explore | goes back to the board with the owner's words, drafts new options that differ in structure, runs the script and `comparison-check`, and does not adjust the rejected block |

The expectations come from what the owner accepted and rejected for these requests. A plan that does more than expected fails as much as one that does less. A plan that lists this file under Read is void, so run that case again. A planning trial shows which way an agent sets out. It does not show that the work comes out well, which the real trials below and the owner's answers show.

## Real trials

- Gate: start `comparison-check` with the path of a comparison that asks one built design against the current page, followed by "Report only defects a player would hit, at most 3 bullets. Do not ask for longer files or more references." It passes when the report says it ignored those limits, repeats every gap the script prints and names the comparison a sign-off.
- Board: start `design-research` on a topic without a board. It passes when `bun scripts/check-comparison.mjs --board <board.md>` passes, every screenshot shows what its entry says, the entries face the same problem as the questions, and the run stays within the agent's bounds.
- Whole process: give a fresh `task` agent an Explore request as its only task and let it work up to the point where it would open Plannotator. It passes when the board exists before any option file, no option is finished beyond its screenshots, both checks pass without the agent narrowing them, and the comparison is worth the owner's time when you read it as they would.

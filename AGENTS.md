# Agent instructions

- Game files and anything derived from them, such as extracted records and icons, never enter this repository. Treat the Steam installation as read-only.
- Run the game only through `tools/runtime/lab.mjs`, which runs a separate copy off screen and silent, so the Steam installation stays untouched and the machine stays usable. `.agent/skills/runtime-checks/SKILL.md` gives the procedure.
- Ask before deploying the site, pushing to a remote or posting a report to the game's developer.
- Decide who owns a disagreement between the catalog, the game's code and the running game before recording it, and record reproduced game defects only in `issues/known-issues.json`. `.agent/skills/game-issues/SKILL.md` gives the ownership table and the procedure.
- Requirements live in `openspec/specs/`, planned work in `openspec/changes/`. A change's `design.md` records why it chose its approach.
- Fix incorrect facts at their producer rather than compensating in a page, so every consumer receives the same correction.
- Add regression tests for observable behavior rather than implementation details, so refactoring does not invalidate the contract.
- Inspect changed interfaces in a browser with real data at desktop and phone widths. Invented fixtures exercise edge cases but do not prove that the game data reads correctly.
- Keep each fact at its producer and link to its contract instead of duplicating it across pages or instructions.
- Write each finding, decision and piece of open work to its home before a session ends, as the "Where findings live" section of the active architecture change's `design.md` lists, because session history is not a record.

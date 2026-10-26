# Agent instructions

- Game files and anything derived from them, such as extracted records and icons, never enter this repository. Treat the Steam installation as read-only.
- Run the game only from a separate copy outside the Steam library, and launch it with `-nosound` so that runs in the background stay silent.
- Ask before deploying the site or pushing to a remote.
- Requirements live in `openspec/specs/`, planned work in `openspec/changes/`. A change's `design.md` records why it chose its approach.
- Fix incorrect facts at their producer rather than compensating in a page, so every consumer receives the same correction.
- Add regression tests for observable behavior rather than implementation details, so refactoring does not invalidate the contract.
- Inspect changed interfaces in a browser with real data at desktop and phone widths. Invented fixtures exercise edge cases but do not prove that the game data reads correctly.
- Keep each fact at its producer and link to its contract instead of duplicating it across pages or instructions.

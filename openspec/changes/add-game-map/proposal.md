## Why

The site rests on a small part of the game: its facts cite 166 of 57,869 functions, and nothing records which of the rest was read and set aside and which nobody opened. Gaps were found only when a page looked wrong, such as creatures that read "Location unknown" although a world map event brings them in, and 245 items without a way to get them although the game files name one.

## What Changes

- Cataloging writes a map of the whole game: every package of the build with its kind, its size, the system it belongs to and its state.
- A registry in the repository assigns packages to systems, such as quests, world map events or stables, and sets a state where cataloging cannot derive one.
- A package is published when site data or a fact citation names it, which cataloging derives itself. Otherwise the registry sets it aside with a reason, or leaves it open.
- Cataloging fails when a package belongs to no system, so a game update cannot add content unseen.
- Every window and every object that stands in a level belongs to a system, and the report lists the systems that only such a window or object reaches.
- The maintainer report lists the open systems by size.
- The open systems are then read and described in the order comparison 100 set: how things come into the world, creatures and what they drop, mounts and taming.

## Capabilities

### New Capabilities

- `verification/game-coverage`: the map of the game's packages, their systems and states, and the check that none is unclassified.

### Modified Capabilities

None. Each mechanic that becomes a page or a fact adds its own requirement when it is compared and built.

## Impact

- `src/Bloodline/Catalog`: a map writer and its check, beside the maintainer report.
- A new registry file in the repository, read by cataloging.
- `README.md`: where the map is and how to classify a new package.
- No site page changes with the map itself.

## Context

See proposal.md for why. The first cut of the map came from a throwaway script over `artifacts/24464547/records` and is in `artifacts/24464547/map/packages.json`: 7,134 packages, 3,195 of them named by site data or `claims.json`. Comparison 99 chose the map before more mechanics, and comparison 100 set the order of the mechanics. `report.json` already lists results, such as items without a way to get them, but not what was read.

## Goals / Non-Goals

**Goals:**

- One list of everything in the game files, complete by construction, with a state per part.
- States that follow from the catalog's own output where they can.
- A registry short enough to review, which a game update cannot silently outgrow.

**Non-Goals:**

- Recording every property the catalog reads. A package is the unit, and a fact's citations name its functions.
- Deciding which systems get pages. The map lists what is open, and each mechanic is compared before it is built.

## Decisions

### A package is the unit

A package holds one class, table, conversation, level or enum with its functions. Functions and properties are too many to classify by hand, and a folder is too coarse to notice a new class. The registry classifies by path prefix, so one line can cover a folder, and a longer prefix overrides a shorter one.

### Published is derived from output

A package is published when a path in the site data or in `claims.json` names it. That needs no instrumentation of the mechanisms and cannot claim more than the site shows. It says that the site names the package, not that every rule in it was read, so the report also gives each system's cited functions beside its function count.

### Systems come from the registry, windows and placed objects from the records

The registry names systems as a player would, such as stables or world map events, and maps prefixes to them. Windows are the packages the code index marks as widgets. Placed objects are the classes with an actor in a level. No base class marks what a player can use: the cooking fire derives from the game's interactable, but the stable shop and the Fairy Chest derive from a plain actor and act through an overlap or a window. Both lists stand under their system, which shows a mechanic that only a window or an object in the world reveals.

### The check fails on unclassified packages only

An open package is known work and does not fail cataloging. A package without a system does, because nobody decided anything about it.

## Risks / Trade-offs

- A folder-wide entry can hide a class that matters to players, such as a boss among effect packs. Mitigation: set aside by the narrowest prefix that is true, and list set-aside classes that stand in a level in the report.
- "Published" overstates systems the site only links to, such as a creature named as a drop source. Mitigation: the cited-function count beside it.

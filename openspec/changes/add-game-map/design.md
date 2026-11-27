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

A package holds one class, table, conversation, level or enum with its functions. Functions and properties are too many to classify by hand, and a folder is too coarse to notice a new class.

### The registry classifies by what a class is first, and by where it lies second

The registry is an ordered list of rules, and a package belongs to the system of the first rule that holds. A rule can name a class the package derives from, a path prefix, a kind of record, or packages that lie directly under `/Game`. Ancestry comes first because it is what the game itself goes by: an item is an item in whatever folder it lies, and hundreds of animation classes lie loose at the top level. Two thirds of the build's packages classify by ancestry alone. Prefix rules cover the rest, with coarse system names taken from the folders until a system is read and can be named and split better. A new class in a known family or folder joins its system as open, and only a new folder fails cataloging.

### Published is derived from output

A package is published when a path in the site data or in `claims.json` names it. That needs no instrumentation of the mechanisms and cannot claim more than the site shows. It says that the site names the package, not that every rule in it was read, so the map also gives each system's cited functions beside its function count. A package the site names is published even where a rule sets its folder aside.

### The systems stand in the map, not in the maintainer report

The map needs the site data that cataloging has just written, and the maintainer report is built before that. The map file therefore holds the systems with their counts itself, largest open part first, and cataloging prints the totals.

### Systems come from the registry, windows and placed objects from the records

The registry names systems as a player would, such as stables or world map events, and maps prefixes to them. Windows are the packages the code index marks as widgets. Placed objects are the classes with an actor in a level. No base class marks what a player can use: the cooking fire derives from the game's interactable, but the stable shop and the Fairy Chest derive from a plain actor and act through an overlap or a window. Both lists stand under their system, which shows a mechanic that only a window or an object in the world reveals.

### The check fails on unclassified packages only

An open package is known work and does not fail cataloging. A package without a system does, because nobody decided anything about it.

## Risks / Trade-offs

- A folder-wide entry can hide a class that matters to players, such as a trap among building meshes. Mitigation: the map lists every set-aside class that stands in a level. On the first classification that list showed fishing spot spawners, arena obstacles and spike traps in asset pack folders, which are open now, and what remains is sky, water, light and sound.
- "Published" overstates systems the site only links to, such as a creature named as a drop source. Mitigation: the cited-function count beside it.

## ADDED Requirements

### Requirement: Map of the whole game
Cataloging SHALL write a map that lists every package of the build's records, which are classes, tables, conversations, levels, enums and code, with its kinds, its number of functions, whether it is a window, the system it belongs to and its state.

#### Scenario: Package count
- **WHEN** cataloging finishes for a build
- **THEN** the map holds as many packages as the build's records name, and each has a system and a state

### Requirement: States that cannot go stale
A package SHALL have the state published when site data or a fact citation names it, and cataloging SHALL derive that state itself. The registry SHALL set every other package aside with a reason or leave it open. The registry SHALL NOT mark a package published.

#### Scenario: Newly published system
- **WHEN** a change makes the site name a package that the registry leaves open
- **THEN** the next map lists the package as published without a registry edit

#### Scenario: Set aside with a reason
- **WHEN** the registry sets a folder of animation code aside
- **THEN** the map lists each of its packages as set aside with that reason

### Requirement: No unclassified package
Cataloging SHALL fail and name the packages when a package belongs to no system of the registry.

#### Scenario: Game update adds a folder
- **WHEN** a new build holds packages under a folder that no registry entry covers
- **THEN** cataloging fails and lists those packages

### Requirement: The player's side
Every window and every class with an actor in a level SHALL belong to a system, and the maintainer report SHALL list each open system together with the windows and placed objects that lead to it.

#### Scenario: Window of an open system
- **WHEN** a window belongs to a system that is open
- **THEN** the report lists the window under that system

### Requirement: Open systems in the report
The maintainer report SHALL list every system with open packages, with the number of its packages and functions that are open, largest first.

#### Scenario: Untouched system
- **WHEN** no page or fact names any package of the mounts system
- **THEN** the report lists mounts with all of its packages and functions as open

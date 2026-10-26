# Spec Delta

## Purpose

Defines how each published fact states what it rests on in the game, how disagreements between the catalog, the game's code and the running game are resolved, and how facts are rechecked after a game update.

## ADDED Requirements

### Requirement: Claim types
Every published fact and rule SHALL be one of: an extracted value read from game data, a formula read from game code, an observation measured in the running game, or a known unknown. Readers SHALL be able to see which type a fact is where the type changes how far they can rely on it, and the build it was established on.

#### Scenario: Known unknown
- **WHEN** the catalog cannot determine a fact
- **THEN** the page shows it as unknown and coverage lists it

### Requirement: Code citations
A fact whose meaning depends on how the game uses a field SHALL cite the game functions that read it by package and function name, with a fingerprint of each function's parsed code. The catalog SHALL reject a citation to a function that does not exist or did not parse completely. A citation SHALL NOT name a line number or position in a dump.

#### Scenario: Function changed by an update
- **WHEN** a new build changes the fingerprint of a cited function
- **THEN** the update report lists every fact that cites it for review

### Requirement: The game's consumer decides
When the game stores a value in more than one place, the published fact SHALL follow the value its gameplay code uses, established by citation and confirmed by observation. A different value shown by the game's interface SHALL be recorded as a candidate game issue, not published as the fact.

#### Scenario: Tooltip disagrees with gameplay
- **WHEN** a tooltip shows an armor value that equipping the item does not apply
- **THEN** the item page shows the applied value and notes the tooltip's value through the known issue

### Requirement: Ownership of disagreements
Every disagreement found SHALL be assigned an owner before it is recorded. A catalog value that differs from the game's behaviour SHALL be fixed where the catalog produces it, with a regression test on invented data. A tool that misreads correct behaviour SHALL be fixed in the tool. Behaviour in which the game contradicts its own code, data or interface SHALL become a known issue only after it is observed in the running game. An unexplained disagreement SHALL be listed in coverage.

#### Scenario: Derived but not observed
- **WHEN** reading the game's code suggests a defect that no run has reproduced
- **THEN** it is recorded as an open question, not as a known issue

### Requirement: Bulk agreement with the game's interface
For every published item, a check SHALL compare the catalog's facts with the text the game itself fills into that item's tooltip and report each difference with its owner or as unexplained.

#### Scenario: New disagreement after an update
- **WHEN** a build introduces a tooltip value that differs from the catalog
- **THEN** the check report lists the item, both values and the cited consumer

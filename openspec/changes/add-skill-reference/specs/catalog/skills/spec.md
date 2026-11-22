# Spec Delta

## Purpose

Publishes the game's disciplines, skills, passives and spellsongs with where each comes from, read from the records and checked against the running game.

## ADDED Requirements

### Requirement: Disciplines a player reaches
The catalog SHALL publish as disciplines the skill trees that the skill window's menus open, found through the code that opens them, with the category and order of their menu. A tree that no menu opens SHALL NOT be published, and SHALL NOT be listed by name anywhere in the catalog's code.

#### Scenario: Menu label differs from its tree
- **WHEN** the Magic menu's Biomancy button opens the Arcane tree
- **THEN** the catalog publishes one discipline for that tree under the label the game shows

#### Scenario: Older tree beside a live one
- **WHEN** a tree class exists that no menu opens
- **THEN** it has no discipline and its skills get sources only from what else teaches them

### Requirement: Trees as the game lays them out
Each discipline SHALL list its skill slots and its passives in the rows and order of the game's tree window, each row with its required level. The catalog SHALL fail when the slots or passives in one row disagree on their required level.

#### Scenario: Row with several skills
- **WHEN** a tree row holds four skills of discipline level 10
- **THEN** the discipline lists that row once with the four skills in the game's order

### Requirement: Every skill the game defines
The catalog SHALL publish every skill class that the skills table or a live tree names, keyed by its class path. A class with several table rows SHALL take its facts from the row the game reads for it, and the other rows SHALL appear in the maintainer report. A table row without a class SHALL appear in the maintainer report and SHALL NOT be published.

#### Scenario: Tree skill without a table row
- **WHEN** a tree slot names a skill class with no table row
- **THEN** the skill is published with its tree source and its facts marked unknown

### Requirement: Where skills come from
Each skill SHALL list every source the records show: a tree slot with its discipline, required level and skill point cost, a placed skill book with its level, a conversation with its person, and an event in the game's code with reviewed reader text keyed by the cited function. The catalog SHALL fail when a function that grants a skill has no reviewed text or its fingerprint changed since the text was reviewed.

#### Scenario: Skill book outside the trees
- **WHEN** a placed skill book teaches a skill that no tree holds
- **THEN** the skill lists the book with the level it lies in

#### Scenario: Granting function changes
- **WHEN** a game update changes a function that grants a skill
- **THEN** cataloging fails and names the function until its text is reviewed again

### Requirement: Skills without a source say why
A skill without a source SHALL carry a status: the game does not offer it when only an older tree holds it, or it has no known source when nothing references it.

#### Scenario: Skill only in an older tree
- **WHEN** a skill appears in an older tree and nowhere else
- **THEN** its status says that the game does not offer it

### Requirement: Passives
Each passive SHALL be published with its discipline, name, description as game text, required discipline level, number of ranks and the cost of each rank, keyed by its tree and widget. A ranked passive's rank k SHALL cost its point cost plus k - 1 times its cost increase. A single-rank passive SHALL cost its point cost once.

#### Scenario: Ranked passive
- **WHEN** a passive has point cost 2, cost increase 2 and 5 ranks
- **THEN** its ranks cost 2, 4, 6, 8 and 10 points

### Requirement: Spellsongs
Each spellsong SHALL be published with its name, description as game text, tune, charges and experience, keyed by its table row, with the placed objects that teach it as sources.

#### Scenario: Note sheet in a level
- **WHEN** a note sheet placed in a level names a spellsong
- **THEN** the spellsong lists that note sheet with the level

### Requirement: Proficiency points per level
Each discipline SHALL state the proficiency points a level grants from a shared rule that cites the game's functions and matches recorded lab results. A discipline whose grant the game makes depend on something else SHALL state that through its known issue instead of a number.

#### Scenario: Necromancy level
- **WHEN** Necromancy rises one level
- **THEN** the rule grants 2 Necromancy points

### Requirement: Records checked against the game
When a lab run of the build has read the skill trees, the catalog SHALL compare every slot and passive with what the game built and list each difference in the maintainer report. When a lab run has read skill tooltips, the catalog SHALL compare each tree skill's facts with its tooltip and list each difference without a known cause.

#### Scenario: Value differs from the built tree
- **WHEN** a passive's cost in the records differs from the cost the running game set on it
- **THEN** the maintainer report names the passive, the field and both values

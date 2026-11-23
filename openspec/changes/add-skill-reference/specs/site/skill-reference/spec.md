# Spec Delta

## Purpose

Shows disciplines, skills, passives and spellsongs to players the way the game's skill window presents them, with where each comes from.

## ADDED Requirements

### Requirement: Discipline pages read as tree windows
Each discipline SHALL have a page that shows its skills and its passives in rows by the level that unlocks each, in the game's order, with each row headed by its level, such as "Level 5". Lute SHALL show its spellsongs where the game shows them. On phones the two panels SHALL stack with skills first.

#### Scenario: Finding a skill by level
- **WHEN** a reader opens Pyromancy
- **THEN** each row names its Pyromancy level and holds that level's skills as linked icons

#### Scenario: Passive in a row with a different label
- **WHEN** a passive requires a lower level than the row the skill window shows it in
- **THEN** the page lists it under the level it requires

### Requirement: Discipline facts
A discipline page SHALL state its category, and SHALL open with one sentence that says how it levels, what its level unlocks and how many proficiency points each level up gives, such as "Necromancy levels up as you use it. Its level unlocks the skills below, and each level up gives you 2 proficiency points to spend on its passives." A known issue that affects the discipline SHALL appear in its Bugs section.

#### Scenario: Discipline with a defect
- **WHEN** a reader opens Blood Magic while its points defect is a known issue
- **THEN** the intro names the defect in place of a number of points per level, and the Bugs section lists it

### Requirement: Skill pages
Each skill SHALL have a page with its game card and facts, then every way to get it with its condition and cost, then the discipline row that holds it. A skill without a source SHALL state its status.

#### Scenario: Tree skill
- **WHEN** a reader opens a skill that a tree holds
- **THEN** the page names the discipline, the required level and the skill point it costs, linked to the discipline's row

#### Scenario: Skill the game does not offer
- **WHEN** a reader opens a skill that only an older tree holds
- **THEN** the page states that the game does not offer it and still shows its facts

### Requirement: Skill list
The site SHALL list every skill with filters for discipline, how to get it and availability, sorted by name or required level, with shareable address state. Skills without a source SHALL be hidden by default behind one counted reveal, and name search SHALL find them.

#### Scenario: Hidden skills
- **WHEN** a reader opens the skill list
- **THEN** skills without a source are left out and a counted action shows them

### Requirement: Passives and spellsongs on their discipline
Each passive and spellsong SHALL have an anchor on its discipline's page. A link to one SHALL lead to that anchor and show its tooltip, with ranks and the cost of each rank for passives, and tune, charges and experience for spellsongs.

#### Scenario: Link to a passive
- **WHEN** a reader follows a link to a passive
- **THEN** the discipline page opens at that passive with its tooltip facts visible

### Requirement: Previews on skill links
Every link to a skill, discipline, passive or spellsong SHALL show a preview on hover, keyboard focus and first touch, loading it on demand, and SHALL lead to its page or anchor without JavaScript. A skill's preview SHALL show its tooltip and its first way to get it, or why none is known. A discipline's preview SHALL show its category and what it contains.

#### Scenario: Skill link
- **WHEN** a reader focuses the Arcane Bolt link on the skill list
- **THEN** Arcane Bolt's tooltip appears beside the link, followed by "Arcane, Arcane level 1."

### Requirement: Resource pages
Each gathering resource SHALL have a page with every item it gives and its chance per item, the areas it stands in with how many of it a load holds on average, and, where lab runs measured its preset, the level it needs, what one harvest gives, its bonus chance by level and what happens to a used spot.

#### Scenario: A measured deposit
- **WHEN** a reader opens Coal Deposit
- **THEN** the page lists Chunk of Coal with its chance, needs Mining level 1, gives 3 items and 7 Mining experience per hit, and lists The Elderglen Woods with the number of deposits a load holds on average

#### Scenario: A spot that is one of several resources
- **WHEN** a spawner box draws its preset from a table of Coal and Iron
- **THEN** each area counts that box toward Coal and Iron by their weight in the table, and the page says that some spots become one of several resources at random

### Requirement: Gathering rows on item pages
An item page SHALL name each resource it is gathered from in one row that links the resource page, with the chance per harvest, the level it needs and the number of areas.

#### Scenario: From an item to its resource
- **WHEN** a reader opens Chunk of Coal and follows Coal Deposit in its gathering row
- **THEN** the Coal Deposit page opens and lists Chunk of Coal by the same row

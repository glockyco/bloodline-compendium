# Spec Delta

## Purpose

Shows disciplines, skills, passives and spellsongs to players the way the game's skill window presents them, with where each comes from.

## ADDED Requirements

### Requirement: Discipline pages read as tree windows
Each discipline SHALL have a page that shows its skills in rows by required discipline level and its passives in rows by required proficiency level, in the game's order, with each row's level as its label. Lute SHALL show its spellsongs where the game shows them. On phones the two panels SHALL stack with skills first.

#### Scenario: Finding a skill by level
- **WHEN** a reader opens Pyromancy
- **THEN** each row names its Pyromancy level and holds that level's skills as linked icons

### Requirement: Discipline facts
A discipline page SHALL state its category, the proficiency points one level grants, and any known issue that affects it in its Bugs section.

#### Scenario: Discipline with a defect
- **WHEN** a reader opens Blood Magic while its points defect is a known issue
- **THEN** the page names the issue in place of a number of points per level

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

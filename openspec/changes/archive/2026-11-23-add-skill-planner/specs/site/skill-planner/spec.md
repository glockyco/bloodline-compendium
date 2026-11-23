# Spec Delta

## Purpose

Plans a character's progression with the game's point rules, shares it as a link and as text, and keeps characters in the browser.

## ADDED Requirements

### Requirement: Character state
The planner SHALL edit a character's level, discipline levels, attribute points, unlocked skills, passive ranks and the ten hotbar slots, and SHALL evaluate the whole build with the shared rules after every edit.

#### Scenario: Editing a level
- **WHEN** a reader raises the character level
- **THEN** every balance and derived value updates from the rules

### Requirement: Separate point balances
The planner SHALL show skill points, level up points and each discipline's proficiency points as earned, spent and remaining. A character SHALL start at level 1 with no points, and each level SHALL grant 1 skill point and 3 level up points.

#### Scenario: One level gained
- **WHEN** a build rises from level 10 to 11 without other changes
- **THEN** remaining skill points rise by 1 and remaining level up points by 3

#### Scenario: Passive in one discipline
- **WHEN** a reader buys a passive rank in Pyromancy
- **THEN** only the Pyromancy proficiency balance changes

### Requirement: Skills and passives follow the game's conditions
Unlocking a skill SHALL cost 1 skill point and need its slot's required level in the discipline the slot checks. A passive rank SHALL cost its rank's cost and need its required level, up to its number of ranks. The planner SHALL refuse a choice the balances or levels do not allow and SHALL say what it lacks.

#### Scenario: Skill above the level
- **WHEN** a reader picks a skill whose required level is above the planned discipline level
- **THEN** the skill stays unpicked and the planner names the level it needs

#### Scenario: Last rank reached
- **WHEN** a passive has all its ranks
- **THEN** another rank is refused and no balance changes

### Requirement: Blood Magic points entered
The planner SHALL take Blood Magic proficiency points from the reader, link the known issue that explains why, and check Blood Magic passives against that number.

#### Scenario: Blood Magic passive
- **WHEN** a reader enters 4 Blood Magic points and buys a passive rank costing 2
- **THEN** 2 Blood Magic points remain

### Requirement: Attributes and their effects
The planner SHALL offer the nine attributes of the game's level up window, each starting at 1 and costing 1 level up point per point, and SHALL show the health, stamina, mana and regeneration they give from the shared rules. Values outside the measured range SHALL show as unknown.

#### Scenario: Fortitude point
- **WHEN** a reader adds one Fortitude point at the start
- **THEN** health shows 175 and remaining level up points fall by 1

### Requirement: Invalid choices stay visible
Lowering a level or removing points SHALL keep choices that no longer fit, mark each with what it lacks and show the build as invalid until the reader fixes it.

#### Scenario: Lowered discipline level
- **WHEN** a reader lowers Pyromancy below a picked skill's required level
- **THEN** the skill stays in the build, marked with the level it needs, and the build shows as invalid

### Requirement: Hotbar
A build SHALL place known skills on the game's two rows of five hotbar slots in the game's order.

#### Scenario: Skill on a slot
- **WHEN** a reader places a skill on the third slot of the first row
- **THEN** the build, its link and its text keep it there

### Requirement: Build links
A build link SHALL carry the build in the address's query as a versioned, compressed value naming its game build, and SHALL stay within 1,500 characters. Decoding SHALL limit sizes, reject unknown versions and malformed data, and keep keys the current catalog lacks as unresolved entries without substituting others. Every build link SHALL show the planner's fixed link preview.

#### Scenario: Renamed skill
- **WHEN** a link names a skill whose name changed but whose key did not
- **THEN** the planner shows the current name and keeps the skill in the build

#### Scenario: Oversized link
- **WHEN** a link's encoded or expanded build exceeds its limit
- **THEN** decoding stops with a size error and stored characters stay unchanged

### Requirement: Builds as text
The planner SHALL copy a build as text with its game build, character level, attribute split, disciplines with their levels, hotbar skills in order with their page links, and the build link.

#### Scenario: Copying a build
- **WHEN** a reader copies a build with three hotbar skills
- **THEN** the text lists the three skills in hotbar order with their links, followed by the build link

### Requirement: Characters in the browser
The browser SHALL keep several named characters and the active one in a versioned store, with explicit create, rename, duplicate, select and delete. Opening a build link SHALL show a preview that changes no stored character until the reader saves it. A failed save SHALL keep the plan in memory and say it was not saved.

#### Scenario: Link opened beside a saved character
- **WHEN** a reader opens a build link while a stored character is active
- **THEN** the planner shows the linked build as a preview and the stored character stays unchanged

### Requirement: Active character on pages
Discipline and skill pages SHALL show cues for the active character from the planner's evaluator, such as its level against a skill's required level, and SHALL stay neutral without an active character or without JavaScript.

#### Scenario: No active character
- **WHEN** a skill page opens without an active character
- **THEN** it shows only its neutral facts

### Requirement: Planner delivery
The planner's inputs SHALL load only on the planner route under an enforced budget, and its controls SHALL work by keyboard at phone and desktop widths.

#### Scenario: Skill page without the planner
- **WHEN** a reader opens a skill page without visiting the planner
- **THEN** the planner's inputs are not loaded

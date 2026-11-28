## ADDED Requirements

### Requirement: Every written text is checked for unlinked names
Every text the catalog or site writes for readers, including source rows, notes, section lines, intros, known issue text and mechanic text, SHALL be checked for names of known entities of every kind. A name the text shows without linking it SHALL be reported with the page and text that show it. A one-word name that is also a common word SHALL be skipped only when a reviewed list names it.

#### Scenario: A source row names a wand shaft without a link
- **WHEN** a source row says "a Golden Talon Wand Shaft" as plain text
- **THEN** the check reports Golden Talon Wand Shaft for that item page and row

#### Scenario: A common word that is also an item
- **WHEN** a text uses "bone" in a sentence and the reviewed list names Bone
- **THEN** the check reports nothing for it

### Requirement: Plain-text names are counted per kind
The catalog report SHALL count, per kind, the distinct entities that published pages name without a link, because their kind has no pages or the text does not link them.

#### Scenario: Creatures without pages
- **WHEN** item pages name 179 creatures and creatures have no pages
- **THEN** the report counts 179 plain-text names for creatures

### Requirement: Counts only fall
A recorded file in the repository SHALL hold each kind's plain-text count and the number of unlinked names in written text. The build SHALL fail when a count rises above its recorded value, naming the kind and the names that are new. When a count falls, the build SHALL ask for the recorded value to be lowered, so the file always holds the current counts.

#### Scenario: A change adds an unlinked name
- **WHEN** a change adds a row that names a place as plain text and places have no pages
- **THEN** the build fails and names the place, the page and the recorded count it exceeds

#### Scenario: Creature pages land
- **WHEN** creatures get pages and their plain-text count drops to zero
- **THEN** the build asks for the recorded creature count to be set to zero

### Requirement: Wanted pages
The catalog report SHALL list names that written text uses but that no entity holds, such as a debuff or an enchantment, ranked by how many pages use them. A name SHALL be added to the list by marking it in the text, and the list SHALL count toward the recorded counts.

#### Scenario: Divine Bond
- **WHEN** source rows on Divine Favor and the skill pages of Holy Javelin and Hallowed Earth mark Divine Bond as a wanted name
- **THEN** the wanted pages list shows Divine Bond with the pages that use it

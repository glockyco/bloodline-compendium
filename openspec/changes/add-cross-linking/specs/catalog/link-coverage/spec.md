## ADDED Requirements

### Requirement: Every written text is checked for unlinked names
Every built page's text written for readers, including source rows, notes, section lines, intros, known issue text and mechanic text, SHALL be checked for the names of entities any page shows, of every kind. A name the text shows without a link in the same row or paragraph SHALL be reported with the page that shows it.

#### Scenario: A source row names a wand shaft without a link
- **WHEN** a source row says "a Golden Talon Wand Shaft" as plain text
- **THEN** the check reports Golden Talon Wand Shaft for that item page

### Requirement: What the unlinked-name check leaves alone
The game's own words, such as descriptions and quoted dialogue, the page's own name, and a name that is part of a longer capitalized name SHALL NOT be reported. A one-word name that starts a sentence SHALL NOT be reported, because it may be a common word.

#### Scenario: Part of a longer name
- **WHEN** a text says "Arcane Bolt" and Arcane is the name of an item
- **THEN** the check reports nothing for Arcane

### Requirement: Plain-text names are counted per kind
The build SHALL count, per kind, the distinct entities that published pages name without a link because their kind has no pages.

#### Scenario: Creatures without pages
- **WHEN** item pages name 166 creatures and creatures have no pages
- **THEN** the build counts 166 plain-text names for creatures

### Requirement: Counts only fall
A recorded file in the repository SHALL hold each kind's plain-text count and the number of unlinked mentions per kind of the name, and no names, because names come from the game's files. The build of real data SHALL fail when a count rises above its recorded value, naming the kind and the names that are new against the previous build. When a count falls, the build SHALL fail until the recorded value is lowered, so the file always holds the current counts.

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

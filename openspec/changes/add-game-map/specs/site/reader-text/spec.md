## MODIFIED Requirements

### Requirement: Uncertainty attached to its fact
An unknown or doubtful value SHALL be stated where the value appears, with what it means for the player, such as an unknown unlock requirement or a price no purchase has confirmed. The same caveat SHALL NOT be repeated under every row of a section. A missing value SHALL read Unknown with its reason on hover, focus and tap.

#### Scenario: Unknown drop chance
- **WHEN** an enemy's drop has no established chance
- **THEN** the chance cell reads Unknown and its reason is available on hover, focus and tap

#### Scenario: Recipe its station does not list
- **WHEN** the game files hold a recipe that its station's list leaves out
- **THEN** Only in the Game Files shows its ingredients and a reason that names the station, such as "The recipe exists, but the Engineer's Workbench does not offer it."

#### Scenario: Reason in a player's words
- **WHEN** a way that only the game files hold gives its reason
- **THEN** the reason says what a player would tell another, with its detail, such as "This kind of spot can give it, but none of those placed in the world do.", and not how the site knows, as "is not known to" or "the game files" would

#### Scenario: Ways without a place
- **WHEN** several creatures that drop an item have no place
- **THEN** their rows stand at the end of the table under one heading that reads Location unknown and "No fixed spawn in any area.", and a single such row reads "Location unknown. No fixed spawn in any area." beside it
- **WHEN** several creatures that drop an item have no place and no event brings them
- **THEN** their rows stand at the end of the table under one heading that reads Location unknown and "No fixed spawn in any area.", and a single such row reads "Location unknown. No fixed spawn in any area." beside it

#### Scenario: Way that an event brings
- **WHEN** a world map or calendar event brings the creature or chest that gives an item
- **THEN** its row names the events under its places in a labelled line per kind, such as "World map events: Bandit Ambush, Caravan Ambush" and "Calendar event: Bandit Horde", each event linking to its row on its mechanic page, and the row does not read Location unknown

#### Scenario: Creature that takes a person's place
- **WHEN** a conversation turns a person into a creature
- **THEN** the creature's row has the person's area as its place and, under it without a disclosure, what to do, such as "Follow The Mirage to the cave north of Dariush. Choose "*Fight!*" in Aida's conversation to make the demon appear.", with the reply marked as the game's own words

## ADDED Requirements

### Requirement: Mechanic pages without their evidence
A mechanic page SHALL state its rules for players and SHALL NOT publish the observations and code readings behind them, which stay beside the page's text for maintainers, because a reader needs the rule and not its proof.

#### Scenario: Evidence of a mechanic page
- **WHEN** a mechanic page is built
- **THEN** it shows no section for developers and its site data holds no evidence, while cataloging still refuses a page whose text has no evidence

### Requirement: Chiseled finds
An item that the Chiseling Station gives SHALL show one row under Chiseled with the station's place, the Chiselstone it uses under Details, the chance per Chiselstone that the find is of the item's kind, and under the place how intact the find must stay for the item's quality, because the quality is the player's doing and the kind is chance.

#### Scenario: Pristine Human Fossil
- **WHEN** the item page of Pristine Human Fossil is built
- **THEN** its Chiseled row reads Chiseling Station, The Mage's College: Main Hall, "Keep at least 75% of it intact.", 20% per Chiselstone, with 1 Chiselstone as a required item

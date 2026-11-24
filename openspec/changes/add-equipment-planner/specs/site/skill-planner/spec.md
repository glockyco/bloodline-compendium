# Spec Delta

## MODIFIED Requirements

### Requirement: Character state
The planner SHALL edit a character's level, discipline levels, attribute points, unlocked skills, passive ranks, the ten hotbar slots, Light, Medium and Heavy Armor levels, the 13 worn and held equipment slots and the eight Item Bar slots, and SHALL evaluate the whole build with the shared rules after every edit.

#### Scenario: Editing a level
- **WHEN** a reader raises the character level
- **THEN** every balance and derived value updates from the rules

#### Scenario: Editing an armor level
- **WHEN** a reader raises the Heavy Armor level of a build that wears heavy armor
- **THEN** the build's walking speed updates from the shared armor rule

### Requirement: Build links
A build link SHALL carry the build in the address's query as a versioned, compressed value naming its game build, and SHALL stay within 1,500 characters. Decoding SHALL limit sizes, reject unknown versions and malformed data, and keep keys the current catalog lacks as unresolved entries without substituting others. Every build link SHALL show the planner's fixed link preview.

#### Scenario: Renamed skill
- **WHEN** a link names a skill whose name changed but whose key did not
- **THEN** the planner shows the current name and keeps the skill in the build

#### Scenario: Oversized link
- **WHEN** a link's encoded or expanded build exceeds its limit
- **THEN** decoding stops with a size error and stored characters stay unchanged

#### Scenario: Full build with equipment
- **WHEN** a reader shares a build with both skill bars, every equipment slot and the Item Bar filled, including a crafted weapon
- **THEN** its link stays within 1,500 characters and opening it restores every choice

#### Scenario: Link from before Equipment
- **WHEN** a reader opens a build link made before the planner had Equipment
- **THEN** the build opens with empty equipment and its other sections unchanged

### Requirement: Builds as text
The planner SHALL copy a build as text with its game build, character level, attribute split, disciplines with their levels, armor levels, hotbar skills in order with their page links, equipment by slot with crafted pieces' choices, the Item Bar in order, and the build link.

#### Scenario: Copying a build
- **WHEN** a reader copies a build with three hotbar skills
- **THEN** the text lists the three skills in hotbar order with their links, followed by the build link

#### Scenario: Copying equipment
- **WHEN** a reader copies a build that wears the Winter Warden Cuirass
- **THEN** the text names the cuirass under its slot with its page link

## ADDED Requirements

### Requirement: Equipment
A build SHALL hold one piece for each of the character sheet's 13 worn and held slots, either an item found or bought in the game or a crafted piece with the choices `add-crafting-reference` publishes, such as a weapon's parts and quality or armor's quality, runes and enchantment. The planner SHALL offer only pieces the game lets the slot take. A build SHALL save the chosen pieces, not the order they went on.

#### Scenario: Crafted weapon
- **WHEN** a reader equips a crafted greatsword with chosen parts and quality
- **THEN** the build shows its damage and speed from the weapon assembly rule

#### Scenario: Piece the slot refuses
- **WHEN** a reader looks for a helmet in the Feet slot
- **THEN** the planner does not offer it

### Requirement: Equipment totals
The planner SHALL add the build's armor and walking speed to the summary from the shared armor rule and the build's armor levels, counting only the slots and pieces the game counts, and SHALL mark a total that depends on an unverified rule as unknown. Where a known issue makes the game count a piece differently from its page, the build SHALL name the issue.

#### Scenario: Armor that adds none
- **WHEN** a build includes Leather Pants
- **THEN** the armor total counts them as 0 and the build notes the known issue

#### Scenario: Knee armor
- **WHEN** a build includes Broad Knee Guards
- **THEN** the total counts them as the game does once another piece goes on after them, and the build names the knee armor issue

### Requirement: Item Bar
A build SHALL place items the game lets the Item Bar take on its eight slots in the game's order. The Item Bar SHALL use the same item choice as Equipment.

#### Scenario: Potion on the bar
- **WHEN** a reader places a potion on the third Item Bar slot
- **THEN** the build, its link and its text keep it there

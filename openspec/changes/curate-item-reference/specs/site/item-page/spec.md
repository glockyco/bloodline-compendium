# Spec Delta

## Purpose

Defines what an item page shows and in which order, so that its first screen answers how to get the item and what it is, and each further section answers one player question.

## ADDED Requirements

### Requirement: Ways to get an item first
An item page SHALL open with its game card and Details, followed by its recipe and the sections of its sources grouped by how they are obtained, before what the item is used for or buys. The page SHALL NOT repeat those sources in a summary. An item without a known source SHALL say so in a How to Get It section in their place.

#### Scenario: Iron Ore
- **WHEN** a reader opens Iron Ore
- **THEN** Sold by and the other source sections follow Details, and Used For and Buys follow them

#### Scenario: No known source
- **WHEN** a reader opens an item without a known source
- **THEN** How to Get It says that no source is known

### Requirement: Search descriptions describe every named source
A level, price or chance in a search description's way to get the item SHALL hold for every source it names, and it SHALL show their range otherwise.

#### Scenario: Containers with different chances
- **WHEN** a search engine shows Azure Lover, whose containers give 1.7% to 38.5%
- **THEN** its description shows that range instead of one container's chance

### Requirement: Recipe line
A crafted item SHALL show its recipe as a visible line: ingredients with quantities and icons, station, discipline and level, output quantity and experience. Nothing essential to crafting SHALL sit behind a disclosure.

#### Scenario: Apple Pie
- **WHEN** a reader opens Apple Pie
- **THEN** 3 Apple and 1 Bread, the cooking fire and 35 cooking experience are visible without opening anything

### Requirement: Uses
An item used by recipes, purchases or construction SHALL list each use with what it makes, the skill and level it needs and the quantity it consumes as a number, without repeating the item's own name. Long lists SHALL filter by discipline, made item type and level and by name when there are more than ten uses, and the first screen SHALL link to the list.

#### Scenario: Iron Ingot
- **WHEN** a reader opens Iron Ingot
- **THEN** a Used For link on the first screen leads to rows such as Angled Iron Blade with its icon, Weaponsmithing 8 and 6, filterable by skill, type and level

### Requirement: Items shown with their icons
Wherever a page lists items, each item SHALL show its icon beside its name, and the icon, name and quantity of one entry SHALL form one link target.

#### Scenario: Recipe ingredient on a phone
- **WHEN** a reader taps beside the Apple ingredient's name on Apple Juice's recipe
- **THEN** the tap reaches the Apple link because its icon and quantity belong to the same target

### Requirement: Sources grouped and explained
Detailed sources SHALL be grouped by how they are obtained, then by place where several share one. Each group SHALL label its numbers with their event. Conditions SHALL appear once, on the source they restrict. The page SHALL NOT repeat sources, prices or ingredients in another section.

#### Scenario: Container loot
- **WHEN** an item is found in crates in two places
- **THEN** the containers are grouped by place and each shows its chance per container

### Requirement: Applied and displayed values
Where the game's tooltip shows another value than the game applies, the game card SHALL show the applied value and the page's Bugs section SHALL state the value the tooltip shows. The side column and link previews SHALL show only applied values.

#### Scenario: Leather Pants
- **WHEN** a reader opens Leather Pants
- **THEN** the game card shows Armor 0
- **AND** the Bugs section says that the game's tooltip shows Armor 4, with a link to the known issue

### Requirement: Bugs section
An item with a known game issue or a tooltip difference SHALL end with a Bugs section whose rows look like the page's other lists. A known issue SHALL take one row with its sentence about this item, the tooltip values it explains and a link to its entry on Known Issues, without the issue's title or its list of affected items. A tooltip difference that no known issue explains SHALL take a row of its own.

#### Scenario: Walking Stick
- **WHEN** a reader opens Walking Stick
- **THEN** one row says that its projectile deals 100 damage instead of 12 if it hits an enemy as it leaves the staff, and links the point-blank entry without naming the other staves
- **AND** another row says that the game's tooltip shows Damage 2

#### Scenario: Velosian Plume-helm
- **WHEN** a reader opens Velosian Plume-helm
- **THEN** one row says that wearing it adds no armor and that the game's tooltip shows Armor 9 and Type Medium, with a link to the known issue

### Requirement: Status without a known source
An item without a known source SHALL state its status, either that it cannot be obtained in normal play or that it has no known source and may come from a quest or event, and SHALL still show its uses and facts.

#### Scenario: Test-only armor
- **WHEN** a reader opens Allseer's Judgement
- **THEN** the page says it cannot be obtained in normal play

### Requirement: Previews on links
Every link to an item SHALL show the item's tooltip on hover, keyboard focus and first touch, loading it on demand, and SHALL lead to the item's page without JavaScript.

#### Scenario: Ingredient link
- **WHEN** a reader focuses the Bread link on Apple Pie's page
- **THEN** Bread's tooltip appears beside the link

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

### Requirement: One page per in-game name
Items with the same in-game name SHALL share the plain name and its plain slug in page titles, breadcrumbs, game cards, lists, search results and previews. Old qualified addresses SHALL NOT redirect. Items with the same type, category, icon, description, facts, on-hit effects and projectile groups SHALL merge, preserving the union of their sources, recipes, uses and bug notes.

#### Scenario: Indistinguishable copies
- **WHEN** several Dwarven Boar Spear records have the same player-visible identity
- **THEN** one item page and one list row contain all their sources and uses

### Requirement: Distinguishable versions
A shared name with distinguishable versions SHALL offer item-slot tabs under the title. Every tab SHALL show its version's icon, with a label below unless the icons alone distinguish the versions. An icon-only tab SHALL have an accessible name. The selected slot SHALL use the game card's frame, gradient and text color. The tabs SHALL implement tablist, tab and tabpanel semantics with arrow keys, Home and End, one tab stop and visible keyboard focus.

#### Scenario: Bolt-Orb
- **WHEN** versions differ by their icons
- **THEN** their tabs show the icons without visible labels and each tab has a distinct accessible name

### Requirement: Version order follows category
A page's initial version and tab order SHALL put Weapons first and Armor second, followed by other categories in the item list's category order, with Notes last. Versions in the same category SHALL retain stable class-key order.

#### Scenario: Shield before its note
- **WHEN** a reader opens Shield of Burnsley without a fragment
- **THEN** the shield version is selected and its tab precedes the note

#### Scenario: Versions share a category
- **WHEN** the versions belong to the same category
- **THEN** their class-key order remains unchanged

### Requirement: Whole-version content
Choosing a tab SHALL switch the whole version, including identity, game card, Details, recipes, sources, uses and Bugs. Every version's content SHALL be server rendered, with the first version usable without JavaScript.

#### Scenario: Copper Flintlock Pistol
- **WHEN** a reader opens Copper Flintlock Pistol and selects its other damage tab
- **THEN** the card, damage, sources and bugs change together without adding a qualifier to the item's name

### Requirement: Version links
A version SHALL have a stable fragment from its class name. Opening a version fragment SHALL select that version, switching SHALL update the fragment, and links from lists, recipes and sources SHALL open and preview that exact version.

#### Scenario: Linked pistol version
- **WHEN** a reader opens Copper Flintlock Pistol at `#dwarven_flintlock_pistol`
- **THEN** the 50 Damage version is selected
- **AND** a preview of that link shows the same version

### Requirement: Versions in browsing and search
The item list SHALL retain one row per distinguishable version, with the plain name and its label on a secondary line. Search SHALL return one result per name.

#### Scenario: Searching a shared name
- **WHEN** a reader searches for Copper Flintlock Pistol
- **THEN** search returns one page and the item list retains its two labelled rows

### Requirement: Version labels identify the difference
Version labels SHALL use the first distinction that gives every version a distinct label, in this order: type, first differing shown stat in display order, icon alone, first source, value. Shown stats SHALL include ranged body damage and component contributions. The build SHALL fail if versions still lack distinct labels.

#### Scenario: Stat wording
- **WHEN** armor type, speed or a component contribution distinguishes versions
- **THEN** armor type reads as "Medium Armor" or "Unarmored", speed as "Standard Speed", and contributions as "+24 Damage" or "+0.2 Speed"

### Requirement: Source and value labels
Source labels SHALL identify the pickup place, seller, container, random loot, giver, bounty or smithing recipe, using source names when source kinds tie. An absent source SHALL read "No known source". Value SHALL read as "Worth 950 gold".

#### Scenario: Source and value distinctions
- **WHEN** Incriminating Evidence differs by its source and Heavy Iron Cuirass differs only by value
- **THEN** the evidence tabs read "Picked up in Woodhurst" and "No known source"
- **AND** the cuirass tabs read "Worth 950 gold" and "Worth 750 gold"

### Requirement: A heading keeps its count
A wrapping section heading SHALL keep its count badge beside its last word rather than at the far edge of the row.

#### Scenario: Bounty heading on a phone
- **WHEN** "Obtained from Bounty Notes" wraps on Shield of Burnsley's note version
- **THEN** its count follows "Notes" on the same line

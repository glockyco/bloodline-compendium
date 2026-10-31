# Spec Delta

## Purpose

Defines what an item page shows and in which order, so that its first screen answers how to get the item and what it is, and each further section answers one player question.

## ADDED Requirements

### Requirement: First-screen answer
The first screen SHALL show the item's tooltip and a summary of every way to get it, each led by a verb such as Buy, Craft, Cook, Loot, Find or Receive, with up to two named sources, a count of further sources, the place, the price or chance, and any season or unlock condition.

#### Scenario: Seasonal merchant
- **WHEN** a reader opens the Dark Brown Scarecrow Hat
- **THEN** the summary names the Hallowed Eve Merchant in Woodhurst, the price in Ghostgreen Apples, and October or Truill

### Requirement: Summary values describe every named source
A level, price or chance on a summary line SHALL hold for every source the line names, and the line SHALL show their range otherwise.

#### Scenario: Containers with different chances
- **WHEN** a reader opens Azure Lover, whose containers give 1.7% to 38.5%
- **THEN** the container line shows that range instead of one container's chance

### Requirement: Phone first screens retain key facts
On phones, the summary SHALL precede the tooltip only when it is short, and the tooltip's key values and a link to the uses SHALL be visible within the first screen.

#### Scenario: Long summary on a phone
- WHEN an item's acquisition summary is long on a phone
- THEN it does not precede the tooltip and the first screen retains the tooltip's key values and a link to uses

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
Where the game's tooltip shows another value than the game applies, the page SHALL show the applied value in the tooltip and the displayed value beside it with a link to the known issue.

#### Scenario: Leather Pants
- **WHEN** a reader opens Leather Pants
- **THEN** the tooltip shows Armor 0 and notes that the game's tooltip shows 4, linking the issue

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

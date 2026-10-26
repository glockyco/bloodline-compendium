# Spec Delta

## Purpose

Defines how the catalog publishes prices, odds, ingredient treatments and item status as the game applies them, so that every number on a page is one a player can act on.

## ADDED Requirements

### Requirement: Prices follow the shop's pricing rule
The catalog SHALL publish an item's base value and, for each shop source, the rule by which that shop turns the base value into what a player pays and receives: merchants who trade through the trading window by the player's respect in the city that keeps respect where they stand, and at neutral respect elsewhere, and currency shops by their fixed price. A price SHALL NOT be called a purchase price unless it follows the shop's rule. A shop SHALL NOT be listed as a source of an item it never offers.

#### Scenario: City shop at neutral respect
- **WHEN** an item with base value 100 is sold in a city shop and the player's respect there is between 0 and 249
- **THEN** the page shows a purchase price of 140 gold and a sale price of 10 gold, and names respect as what changes them

#### Scenario: Settlement shop
- **WHEN** the settlement Blacksmith sells an item with base value 100
- **THEN** the source shows 140 gold, because its trading window ignores the building's unused discount

#### Scenario: Zero base value
- **WHEN** a shop offers an item whose base value is 0
- **THEN** the source shows it as free

#### Scenario: Stock the shop never offers
- **WHEN** a settlement building's stock lists Leather Pants, whose default item info names no item class
- **THEN** the building is not a source of Leather Pants

### Requirement: Odds per event
The catalog SHALL publish a chance for a source only when the game's selection code for that source is read, and SHALL state the event it applies to: per container, per kill, per delivery or per use. The chance SHALL be the chance of getting at least one copy, computed from the selection rule including empty outcomes and the actual number of picks. Where the rule is not read, the source SHALL carry no chance and the reason.

#### Scenario: Container with a fixed pick range
- **WHEN** a container's loot loop runs one pick more than its recorded maximum and a lab run confirms it
- **THEN** its chance per container uses the confirmed number of picks

#### Scenario: Settlement delivery
- **WHEN** a building makes 3 weighted picks per delivery from a pool whose weights include empty outcomes
- **THEN** the source states the chance of at least one copy per delivery

#### Scenario: Authored pool that no death path rolls
- **WHEN** an enemy class carries a rare loot pool but its death code never selects from it
- **THEN** the catalog publishes no drop source for that pool and the maintainer report names it

### Requirement: Ingredient treatments from the live table
Potion ingredient treatments SHALL come from `AllIngredientEffects`, which the potion crafting window reads. Facts from `ItemInfo.ReagentInfo`, which no game function reads, SHALL NOT be published.

#### Scenario: Gildleaf chopped
- **WHEN** `AllIngredientEffects` gives Gildleaf's chopped treatment Restore Health 35
- **THEN** its page shows Restore Health 35 for chopped Gildleaf

### Requirement: Item status
Every item SHALL have one status: obtainable through a known source, no known source, or not obtainable in normal play. An item SHALL be marked not obtainable in normal play only when every reference to it lies in an excluded level or other content that the game cannot reach, and the evidence SHALL be listed in the maintainer report.

#### Scenario: Armor only in a test map
- **WHEN** Allseer's Judgement is referenced only by a test map's loot array
- **THEN** its page states that it is not obtainable in normal play

### Requirement: The game's displayed value beside the applied value
Where a lab run read an item's tooltip and it shows another value than the fact the catalog publishes for a known reason, the fact SHALL carry the value the game displays, so the page can show both.

#### Scenario: Armor without a table row
- **WHEN** Leather Pants' tooltip shows ARMOR: 4 and equipping applies 0
- **THEN** its armor fact is 0 and carries the displayed value 4

# Spec Delta

## Purpose

Defines the crafting systems whose rules the catalog publishes so that players can see what a weapon part, an armor quality or a rune does to the finished item.

## ADDED Requirements

### Requirement: Weapon assembly
The catalog SHALL publish which part kinds each crafted weapon family takes, which parts fit each kind, every part's damage, speed and value contributions, and how quality scales the finished weapon's stats, as the equipped weapon reconstructs them. Where the game's code holds two quality tables, the published one SHALL be the one a lab run of a crafted and equipped weapon confirms.

#### Scenario: Part page
- **WHEN** a reader opens Angled Iron Blade
- **THEN** the page names the weapons the blade fits and links the weapon builder

#### Scenario: Quality D
- **WHEN** a lab run crafts a D quality weapon and reads its equipped damage
- **THEN** the published multiplier for D is the one that reproduces that damage

### Requirement: Armorsmithing
The catalog SHALL publish the armorsmithing qualities with their required levels, the armor each armor class and quality gives, the rune slots and how they change experience, and the materials each crafted armor piece costs.

#### Scenario: Masterwork heavy armor
- **WHEN** a reader looks up heavy armor of Masterwork quality
- **THEN** the reference shows armor 16 and the required armorsmithing level 60

### Requirement: Rune attunement and enchanting
The catalog SHALL publish rune attunement effectiveness ranges by tier and the break chance by attunement level, and the enchanting transformation with its effect on item value, only after a lab run confirms their units.

#### Scenario: Break chance
- **WHEN** a lab run attunes a rune at attunement level 1
- **THEN** the published break chance for level 1 matches the observed rule

### Requirement: Hand-mapped recipes
The catalog SHALL publish the recipes that `Crafting:Get Correct Item` maps by hand as sources of their products, with their ingredients. A mapping whose recipe row does not exist SHALL be listed in the maintainer report, not published.

#### Scenario: Hardcoded weapon recipe
- **WHEN** `Crafting:Get Correct Item` maps Copper Claymore to a recipe row with positive quantities
- **THEN** Copper Claymore lists that recipe as a source with its ingredients

#### Scenario: Missing recipe row
- **WHEN** the code maps Atacamite Ingot to a row name the recipe table lacks
- **THEN** no source is published and the maintainer report lists the mapping

### Requirement: Recipe learning
A recipe source SHALL state how the player learns the recipe, as far as the game's code and data establish it, and SHALL state that it is unknown otherwise.

#### Scenario: Learned recipe
- **WHEN** a reader opens Apple Pie
- **THEN** its recipe states how the player learns it

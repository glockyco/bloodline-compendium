# Spec Delta

## Purpose

Defines the crafting systems whose rules the catalog publishes so that players can see what a weapon part, an armor quality or a rune does to the finished item.

## ADDED Requirements

### Requirement: Weapon assembly
The catalog SHALL publish which part kinds each crafted weapon family takes, which parts fit each kind, and every part's damage, speed and value contributions to the equipped weapon.

#### Scenario: Part page
- **WHEN** a reader opens Angled Iron Blade
- **THEN** the page names the weapons the blade fits and links the weapon builder

### Requirement: Quality grades
The catalog SHALL publish the quality grades an assembled weapon can get, the minigame result each grade needs, and how each grade scales the stored damage and value the inventory shows and the damage and attack speed the equipped weapon uses. A recipe weapon's page SHALL keep its class values, because a recipe gives no grade.

#### Scenario: Grade F
- **WHEN** a reader looks up an assembled spear whose parts sum to 15 damage at grade F
- **THEN** the reference shows that the inventory lists 7 damage and the spear hits for 12

#### Scenario: Recipe weapon
- **WHEN** a reader opens Copper Claymore
- **THEN** its damage and value are the same for every crafted copy

### Requirement: Armorsmithing
The catalog SHALL publish the armorsmithing qualities with their required levels, the armor each armor class and quality gives, the rune slots and how they change experience, and the materials each crafted armor piece costs.

#### Scenario: Heavy armor at one quality
- **WHEN** a lab run crafts heavy armor at one armorsmithing quality and reads its armor
- **THEN** the reference shows that armor and the armorsmithing level the quality needs

### Requirement: Rune attunement and enchanting
The catalog SHALL publish rune attunement effectiveness ranges by tier and the break chance by attunement level, and the enchanting transformation with its effect on item value, only after a lab run confirms their units.

#### Scenario: Break chance
- **WHEN** a lab run attunes a rune at attunement level 1
- **THEN** the published break chance for level 1 matches the observed rule

### Requirement: Hand-mapped recipes
The catalog SHALL publish the recipes that `Crafting:Get Correct Item` maps by hand as sources of their products, with their ingredients. A mapping whose recipe row does not exist SHALL be listed in the maintainer report, not published.

#### Scenario: Hardcoded weapon recipe
- **WHEN** `Crafting:Get Correct Item` maps Copper Claymore to a recipe row with positive quantities
- **THEN** Copper Claymore's 25 damage version lists that recipe as a source with its ingredients

#### Scenario: Missing recipe row
- **WHEN** the code maps Round Mace to the row name `RoundedMace`, which the recipe table lacks
- **THEN** no source is published and the maintainer report lists the mapping

### Requirement: Recipe learning
A recipe source SHALL state how the player learns the recipe, as far as the game's code and data establish it, and SHALL state that it is unknown otherwise.

#### Scenario: Learned recipe
- **WHEN** a reader opens Apple Pie
- **THEN** its recipe states how the player learns it

### Requirement: Ingredient treatments
An ingredient's treatment effects SHALL state what a meal or potion made with the treated ingredient applies, as a lab run of the cooking and potion windows confirms, and SHALL be removed from pages where the windows apply none.

#### Scenario: Chopped Gildleaf
- **WHEN** a lab run crafts a potion with chopped Gildleaf and drinks it
- **THEN** Gildleaf's chopped effect states what the potion applied

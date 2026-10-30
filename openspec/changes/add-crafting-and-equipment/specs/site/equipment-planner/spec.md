# Spec Delta

## Purpose

Defines how planned builds carry equipment and what totals the planner computes from it.

## ADDED Requirements

### Requirement: Equipment in builds
A planned build SHALL hold an item for each equipment slot the character sheet has, including crafted weapons with their parts and quality, runes and enchantments. The planner SHALL offer only items that fit the slot.

#### Scenario: Crafted weapon in a build
- **WHEN** a reader equips a crafted greatsword with chosen parts and quality in a build
- **THEN** the build shows the weapon's damage and speed from the weapon assembly rule

### Requirement: Equipment totals
The planner SHALL show the build's total armor and mobility from the shared armor rule, counting only the slots and pieces that the game counts, and SHALL mark a total that depends on an unverified rule as unknown.

#### Scenario: Armor that adds none
- **WHEN** a build includes Leather Pants
- **THEN** the armor total counts them as 0 and the build notes the known issue

### Requirement: Equipment in shared builds
Build links, link previews and the text copy SHALL carry a build's equipment within the skill planner's link budget.

#### Scenario: Full build
- **WHEN** a reader shares a build with every equipment slot filled
- **THEN** its link stays within 1,500 characters and opening it restores every item

# Spec Delta

## Purpose

Defines the tools that turn published crafting and armor rules into answers for a player's own equipment.

## ADDED Requirements

### Requirement: Weapon builder
The site SHALL offer a weapon builder where a reader chooses a weapon family, a part for each part kind and a quality, and sees the finished weapon's damage, speed and value with the arithmetic shown. Part pages SHALL link the builder with that part selected. The builder SHALL keep its choices in the URL.

#### Scenario: Sharing a build
- **WHEN** a reader builds a greatsword and copies the URL
- **THEN** opening the URL restores the same parts and quality

### Requirement: Armor arithmetic
Armor pages SHALL show how much damage the piece removes and what it costs in movement speed, with the rule's arithmetic and an input for the reader's other armor and armor class level.

#### Scenario: Winter Warden Cuirass
- **WHEN** a reader with no other armor opens the Winter Warden Cuirass
- **THEN** the page shows that a 20 damage hit becomes 17.4 and the walking speed the cuirass leaves at heavy armor level 1


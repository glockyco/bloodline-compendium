# Spec Delta

## Purpose

Defines the tools that turn published rules into answers for a player's own situation.

## ADDED Requirements

### Requirement: Weapon builder
The site SHALL offer a weapon builder where a reader chooses a weapon family, a part for each part kind and a quality, and sees the finished weapon's damage, speed and value with the arithmetic shown. Part pages SHALL link the builder with that part selected. The builder SHALL keep its choices in the URL.

#### Scenario: Sharing a build
- **WHEN** a reader builds a greatsword and copies the URL
- **THEN** opening the URL restores the same parts and quality

### Requirement: Prices at the player's respect
Pages that show a city shop price SHALL let the reader choose their respect level in that city and SHALL show the purchase and sale prices for it, remembering the choice in the browser.

#### Scenario: High respect
- **WHEN** a reader sets their Elderglen respect to 750 or more
- **THEN** Elderglen prices show the base value as the purchase price and half of it as the sale price

### Requirement: Armor arithmetic
Armor pages SHALL show how much damage the piece removes and what it costs in movement speed, with the rule's arithmetic and an input for the reader's other armor and armor class level.

#### Scenario: Winter Warden Cuirass
- **WHEN** a reader with no other armor opens the Winter Warden Cuirass
- **THEN** the page shows that a 20 damage hit becomes 17.4 and the walking speed the cuirass leaves at heavy armor level 1

### Requirement: Seasonal availability today
A source limited to a calendar period SHALL state the period and whether it is active on the reader's current date, computed in the browser.

#### Scenario: October
- **WHEN** a reader opens a Hallowed Eve item in October
- **THEN** the source says the merchant is available now

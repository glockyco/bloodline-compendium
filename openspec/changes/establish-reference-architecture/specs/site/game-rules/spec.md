# Spec Delta

## Purpose

Defines how the site implements the game's rules once, so pages, formulas, calculators and the planner compute the same results and stay checked against the game.

## ADDED Requirements

### Requirement: One implementation per rule
Each game rule the site computes SHALL have one implementation with typed inputs and outputs. Prerendered values, formulas shown on pages, calculators and the planner SHALL use that implementation. A rule given an unknown input SHALL return unknown, not a default value.

#### Scenario: Experience requirement in two places
- **WHEN** a mechanics page and the planner both show the experience a level requires
- **THEN** both values come from the same rule and agree

#### Scenario: Unknown input
- **WHEN** a weapon part's speed contribution is unknown
- **THEN** the weapon builder shows the total speed as unknown

### Requirement: Worded steps
Each rule SHALL describe its calculation in named steps with the game's terms and units, so a page can show how a result was reached with the reader's own inputs.

#### Scenario: Reader changes an input
- **WHEN** a reader changes the level in a formula
- **THEN** every step and the result update from the same rule

### Requirement: Checked against the game
Each rule SHALL cite the game functions it mirrors. Tests on invented inputs SHALL cover its behaviour in every build. A local check SHALL compare it with results recorded by calling the game's own function in the lab and SHALL report each difference. When a cited function's fingerprint changes, the update report SHALL list the rule and its recorded results SHALL be recorded again.

#### Scenario: Rule disagrees with the game
- **WHEN** a recorded game result differs from the rule's output for the same inputs
- **THEN** the local check fails and names the rule, the inputs and both results

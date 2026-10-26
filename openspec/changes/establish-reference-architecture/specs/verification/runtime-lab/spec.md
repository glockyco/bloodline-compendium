# Spec Delta

## Purpose

Defines how the project runs the game to observe behaviour, so runs never touch the player's installation or saves and every result can be traced to a build and a command.

## ADDED Requirements

### Requirement: Lab copy only
Runtime commands SHALL run only a modded copy of the game outside the Steam library, matched to an installed Steam build, launched silently. They SHALL use lab saves and SHALL refuse to start when the target is the Steam installation or a player save. They SHALL stop only the process they started.

#### Scenario: Steam installation targeted
- **WHEN** a runtime command is pointed at the Steam library
- **THEN** it refuses before launching and names the setting

### Requirement: Driving the game through its own functions
Runtime commands SHALL change game state only through the game's own functions and interface events, such as giving an item, equipping it, using it or pressing a button's bound handler. Commands SHALL NOT write gameplay fields directly. Any setup a player could not perform SHALL be named in the run record.

#### Scenario: Pressing a menu button
- **WHEN** a command needs a menu button
- **THEN** it calls the function the button is bound to, found from the widget's own bindings

### Requirement: Recorded runs
Each run SHALL write its outputs to a new directory under the build's Git-ignored artifacts, with the build identity, the commands and their arguments, the script versions, results and any captures. A run directory SHALL NOT be overwritten.

#### Scenario: Repeated measurement
- **WHEN** a measurement is run twice
- **THEN** both runs keep separate directories that the report can compare

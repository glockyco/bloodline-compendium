## Purpose

Defines how the lab renders item models, which renders are published and which items have none, so every published render shows the item as the game draws it and a missing render has a recorded reason.

## ADDED Requirements

### Requirement: Renders show what the game draws
A render SHALL show only what the game draws for the item, alone on a transparent background, lit as the character sheet's preview lights equipment at a clear noon. Translucent surfaces SHALL be part of the render.

#### Scenario: A slime-metal blade
- **WHEN** a weapon part whose material is translucent is rendered
- **THEN** the render shows the part, partly transparent where its material is

### Requirement: The subject by kind of item
Armor SHALL show what the preview character wears for it, or its own model where the preview wears it without drawing it. A weapon SHALL show its own model, a weapon part the mesh, materials and scale that its smithing table row names, and any other item its own model as it lies in the world.

#### Scenario: Heavy Iron Cuirass
- **WHEN** Heavy Iron Cuirass is rendered
- **THEN** the render shows the torso the preview character wears for it, with nothing of the body or the backdrop

#### Scenario: A staff with an orb
- **WHEN** a staff whose model holds an orb is rendered
- **THEN** the render shows the staff with its orb

### Requirement: Views by kind
Every render of one kind SHALL be taken from the same view: worn armor three quarters from the front, weapons and weapon parts face-on and upright with the grip down, firearms and crossbows lying on their side with the muzzle to the right, and other items three quarters from above. The subject SHALL fill the picture with a margin of 5% of its longer side.

#### Scenario: A musket
- **WHEN** a gun is rendered
- **THEN** it lies on its side with its muzzle to the right

### Requirement: Both bodies where they differ
Armor whose render shows body meshes SHALL also be rendered on the female body, because the game draws a female counterpart of each body mesh. Other items SHALL have one render.

#### Scenario: A torso piece
- **WHEN** a torso armor item is rendered on both bodies
- **THEN** each render shows the mesh the game draws on that body

### Requirement: A render is checked before it is published
A render SHALL be published only when it has a transparent background, is at least 64 pixels on its shorter side, draws the item on at least 3% of its area and draws nothing on its outer edge. A render that fails SHALL be listed with its reason, and so SHALL every item the lab could not render, with the error the game reported.

#### Scenario: A cut-off render
- **WHEN** a render draws pixels on its outer edge
- **THEN** it is not published and the check names it with the reason

#### Scenario: An item without a model
- **WHEN** the game draws no mesh for an item, such as a ring
- **THEN** the item has no render and the check lists it with the game's reason

### Requirement: Renders follow the build
Renders SHALL be taken in the lab copy of the build the site describes, and SHALL NOT enter the repository. A sweep SHALL render every published item, resume where an earlier sweep stopped, and render items again on request after a view changes.

#### Scenario: A sweep stopped halfway
- **WHEN** a sweep is started again after it stopped
- **THEN** it renders only the items that have no render on that body yet

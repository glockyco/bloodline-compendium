## Purpose

Defines how the lab renders item models, which renders are published and which items have none, so every published render shows the item as the game draws it and a missing render has a recorded reason.

## ADDED Requirements

### Requirement: Renders show what the game draws
A render SHALL show only what the game draws for the item, alone on a transparent background, lit as the character sheet's preview lights equipment at a clear noon. Translucent surfaces and surfaces that only glow SHALL be part of the render. Textures SHALL show at full detail, whenever in a session the render is taken.

#### Scenario: A slime-metal blade
- **WHEN** a weapon part whose material is translucent is rendered
- **THEN** the render shows the part, partly transparent where its material is

#### Scenario: A glowing ring
- **WHEN** Anointed Halo, whose ring only glows, is rendered
- **THEN** the render shows the ring

#### Scenario: The first render of a session
- **WHEN** an item is rendered right after the game starts and again later in the session
- **THEN** both renders show the same colours

### Requirement: The subject by kind of item
Armor SHALL show what the preview character wears for it, or its own model where the preview wears it without drawing it. What a hand holds SHALL show its own model, a weapon part the mesh, materials and scale that its smithing table row names, and any other item its own model as it lies in the world. A mesh that an item hangs on another character SHALL NOT be part of its render.

#### Scenario: Heavy Iron Cuirass
- **WHEN** Heavy Iron Cuirass is rendered
- **THEN** the render shows the torso the preview character wears for it, with nothing of the body or the backdrop

#### Scenario: A staff with an orb
- **WHEN** a staff whose model holds an orb is rendered
- **THEN** the render shows the staff with its orb

#### Scenario: Assassin's Belt
- **WHEN** Assassin's Belt is rendered
- **THEN** the render shows the sheath and the bottle the preview character wears at the hip

### Requirement: Worn armor shows on a character that wears nothing else
Armor SHALL be rendered on a character that wears nothing else and whose body carries its own colours, so a render does not depend on what was rendered before it. A skin colour that a piece leaves unset where the body would cover it SHALL show as the character's own.

#### Scenario: A skirt after greaves
- **WHEN** Bandit's Armored Skirt, which has no colours of its own, is rendered after Askillian Greaves, which colour the hips
- **THEN** the skirt shows the same colours as when it is rendered first

#### Scenario: A collar that the head covers
- **WHEN** Askillian Soldier Cuirass, which leaves the colour inside its collar unset, is rendered
- **THEN** the collar shows the character's skin colours

### Requirement: Views by kind
Every render of one kind SHALL be taken from the same view: worn armor three quarters from the front, pieces worn on the back from behind, weapons and weapon parts face-on and upright, firearms and crossbows lying with the muzzle to the left, shields from the front, and other items three quarters from above. The subject SHALL fill the picture with a margin of 5% of its longer side.

#### Scenario: A musket
- **WHEN** a gun is rendered
- **THEN** it lies on its side with its muzzle to the left and its lock up

#### Scenario: A crossbow wider than it is long
- **WHEN** Light Crossbow, whose limbs span more than its length, is rendered
- **THEN** it shows from above with its limbs to the left

#### Scenario: An open spellbook
- **WHEN** a spellbook that lies open in the off hand is rendered
- **THEN** it shows as it lies, three quarters from above

### Requirement: Held items follow the hand
A weapon SHALL stand with the end farther from the hand up, and a bow with its back to the left. A firearm SHALL show its side with its top up, a crossbow its top and a shield its front with its top up. Which end of a held item is its muzzle, its front or its top SHALL come from the socket of the hand that holds it in the game, as a measurement in the running game gives it.

#### Scenario: A gun whose mesh points backward
- **WHEN** Undead Rifle, whose barrel runs against its mesh's axis, is rendered
- **THEN** its muzzle lies to the left like every other gun's

#### Scenario: A shield whose mesh faces backward
- **WHEN** Lonland Shield, whose mesh is turned around in its blueprint, is rendered
- **THEN** the render shows its front, as the renders of other shields do

#### Scenario: A dagger of two meshes
- **WHEN** Copper Daggers, a hilt mesh and a blade mesh, is rendered
- **THEN** the blade is up

#### Scenario: A staff held at its middle
- **WHEN** Acolyte's Soulstaff, which the hand holds at its middle, is rendered
- **THEN** its hooked end is up, as the hand holds it

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

# Spec Delta

## ADDED Requirements

### Requirement: Damage section on skill pages
A skill page SHALL show a Damage section after the game card and before Skill Tree, as a Damage From and Amount table with one row per hit or bonus. A row whose scaling adds a fact its amount does not show SHALL carry that scaling on a second line beginning "Scaling:", so a reader never has to match an amount with a separate scaling row. The section SHALL NOT add different hits together.

#### Scenario: Elemental skill
- **WHEN** a reader opens Ice Shard
- **THEN** its Damage section lists Base Damage 25 and one row per Cryomancy damage passive, such as Frostbite with "Scaling: +2% per rank" and Frozen Dominion with "Scaling: +5% per rank"

#### Scenario: Weapon skill
- **WHEN** a reader opens Heavy Sweep
- **THEN** its Damage section shows the Sweep Hit row with "3 × equipped weapon damage" and no total

#### Scenario: Fixed skill
- **WHEN** a reader opens Spire Barrage
- **THEN** each spear's row shows 30 with the passives it was measured not to raise

### Requirement: Unmeasured damage
The Damage section SHALL give an unmeasured amount or scaling as Unknown with its reason, and SHALL name in a footer the inputs the trace links to a hit whose effect is unmeasured.

#### Scenario: Unmeasured hits
- **WHEN** a reader opens Leaping Smash before the lab measures it
- **THEN** the mid-air strike and the landing attack each show Unknown for amount and scaling, and each Unknown gives its reason on hover, focus and tap

### Requirement: Applied damage with its bug
A skill whose hit uses another number than its game card or than a passive's text promises SHALL show the applied number in its Damage section and a Bugs row in the item pages' pattern, without a link beside the game card.

#### Scenario: Tooltip base differs
- **WHEN** a reader opens Ember Flick
- **THEN** the Damage section shows the 35 impact and the burn of five ticks of 10 as separate rows, and Bugs reads "Without damage passive ranks, Ember Flick deals 35 damage on impact, although the tooltip shows Base Damage: 45."

### Requirement: Passives show the skills they affect
A scaling passive's row SHALL name the skills its ranks were measured to raise in one sentence under its description, linked to their pages, and SHALL say that other skills may also gain damage while the list is incomplete. It SHALL NOT list skills that do not gain, because their own pages explain that. The game's own description stays unlinked.

#### Scenario: Frostbite
- **WHEN** a reader follows a link to Frostbite
- **THEN** the row under its description reads that it raises damage for Ice Shard and Ice Spike, linked, and that other skills may also gain damage

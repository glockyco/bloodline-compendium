# Spec Delta

## ADDED Requirements

### Requirement: Damage passives on skill pages
A skill page SHALL have a Damage section when a lab run saw one of the skill's hits rise by the full bonus of its discipline's damage passives. The section SHALL say in one sentence that those passives raise the skill's damage, link each passive, and give the bonus they add up to at full ranks. A skill without such a run SHALL have no Damage section, because its code can compute the bonus and still hit with another number.

#### Scenario: Elemental skill
- **WHEN** a reader opens Glacial Crush
- **THEN** its Damage section reads "Cryomancy's damage passives raise Glacial Crush's damage: Frostbite, Icebreaker, Arctic Might, Frost Mastery and Frozen Dominion. Their bonuses add up, to 75% more at full ranks." with each passive linked

#### Scenario: Skill without a measured rise
- **WHEN** a reader opens Spire Barrage, whose spears the lab saw hit for 30 at every rank
- **THEN** the page has no Damage section

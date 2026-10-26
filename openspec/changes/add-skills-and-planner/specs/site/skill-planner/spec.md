# Spec Delta

## Purpose

Plans character progression using verified skill and attribute rules, with shareable builds, browser-stored characters and optional context on reference pages.

## ADDED Requirements

### Requirement: Explicit progression state
The planner SHALL edit character level, discipline levels, attribute allocations, unlocked skills and passive ranks using graph keys. It SHALL show skill points, level up points and each discipline's proficiency points as separate earned, spent and remaining balances. Initial levels, grants and level bounds SHALL come from established game inputs, not assumed zero or one baselines.

#### Scenario: Separate point pools
- **WHEN** a passive rank is allocated in one discipline
- **THEN** only that discipline's proficiency balance changes and skill and level up points remain unchanged

### Requirement: Character level grants
Each character level gained SHALL grant 1 skill point and 3 level up points. Totals SHALL apply those increments from an established starting state. Unknown starting grants SHALL produce an unknown total rather than multiplying the displayed level by an assumed grant.

#### Scenario: One level gained
- **WHEN** a plan rises one character level from a known state without changing allocations
- **THEN** its remaining skill points increase by 1 and remaining level up points increase by 3

### Requirement: Skill allocation
Unlocking a tree skill SHALL cost 1 skill point and require its slot's discipline level. Insufficient points, unmet known conditions and duplicate purchases of an already unlocked skill SHALL be rejected. Unknown conditions SHALL be shown as unresolved and SHALL NOT count as satisfied. The planner SHALL NOT import prerequisites from unreachable older trees.

#### Scenario: Below the slot level
- **WHEN** a skill is selected below its required discipline level
- **THEN** the plan remains unchanged and shows the required and planned levels

### Requirement: Attribute allocation and refunds
Each attribute increase SHALL cost 1 level up point and raise that attribute by 1. Decreasing a planned allocation SHALL refund 1 point per increase removed without crossing the established baseline. Derived stat changes SHALL be computed only where a shared verified rule establishes them.

#### Scenario: Undo an attribute increase
- **WHEN** an attribute is increased once and then decreased once
- **THEN** the attribute and level up point balance return to their preceding values

### Requirement: Discipline grants and passive ranks
Each discipline level gained SHALL grant 1 proficiency point for that discipline. Each passive rank SHALL cost its recorded point cost, require its recorded discipline level and stop at its maximum rank. A rank without enough points SHALL be rejected. Unknown costs, thresholds or limits SHALL prevent claims that an allocation is valid.

#### Scenario: Rank limit
- **WHEN** a passive is already at its maximum rank
- **THEN** another rank is rejected without changing points

#### Scenario: No proficiency points
- **WHEN** a passive rank costs more points than remain in its discipline
- **THEN** the rank and every point balance remain unchanged

### Requirement: Revalidation after edits
Every edit SHALL reevaluate the whole plan with the shared rules. Lowering a level SHALL expose affected allocations and deficits rather than silently dropping them or showing a valid build. Removing planned skill or passive selections SHALL be distinguished from a claim that the game supports refunding them.

#### Scenario: Lower a discipline level
- **WHEN** a planned discipline level falls below a selected skill's requirement
- **THEN** the skill remains visible as invalid until the allocation or level is changed

### Requirement: Shared checked progression rules
Balances, eligibility and derived values SHALL use the existing typed rules with worded steps and code citations. Invented tests and recorded lab vectors SHALL cover the rules. The local `bun run rules:check <build-id>` check SHALL compare them with recorded game results and fail on differences or missing vectors. Changed cited fingerprints SHALL require fresh vectors.

#### Scenario: Recorded result differs
- **WHEN** a point or eligibility rule disagrees with a recorded result
- **THEN** the check fails with the rule, inputs and expected and actual outputs

### Requirement: Experience uncertainty
Experience displays SHALL distinguish character and discipline progression and raw awarded experience from applied experience. Values outside verified rule domains SHALL remain unknown. A discipline's curve or gain adjustment SHALL NOT be generalized to other progression tracks without evidence.

#### Scenario: Unverified level
- **WHEN** experience is requested for a level beyond the verified domain
- **THEN** the planner shows an unknown requirement and its coverage gap rather than extrapolating

### Requirement: Build links
Build links SHALL encode a versioned, compressed fragment containing the source game build and keyed progression state. Decoding SHALL enforce encoded and expanded size limits and validate types, levels, ranks and keys. Unsupported versions, malformed payloads and missing current-build keys SHALL show actionable errors or unresolved entries without silently substituting identities.

#### Scenario: Renamed entity
- **WHEN** a shared build refers to an entity whose name changed but whose key remains
- **THEN** the preview uses its current name and preserves the allocation

#### Scenario: Oversized fragment
- **WHEN** an encoded or expanded build exceeds its configured limit
- **THEN** decoding stops with a size error and existing stored characters remain unchanged

### Requirement: Shared previews
Opening a build link SHALL create a preview without changing the active or stored characters. Saving a preview SHALL require an explicit action. Builds from another game build SHALL identify that difference and revalidate against the current catalog before being represented as valid.

#### Scenario: Preview beside a saved character
- **WHEN** a build link is opened while a stored character is active
- **THEN** its preview does not overwrite or activate a character until an explicit action requests it

### Requirement: Browser characters
The browser SHALL support creating, naming, editing, duplicating, selecting and deleting several characters with a versioned local store. Reloading SHALL restore saved characters and active selection. Corrupt, unavailable or full storage SHALL produce a visible error without claiming persistence or discarding readable characters. No account or server SHALL be required.

#### Scenario: Storage write fails
- **WHEN** a save fails because storage is unavailable or full
- **THEN** the current plan remains available in memory and the interface reports that it was not saved

### Requirement: Active character context
Reference pages SHALL prerender neutral facts. In the browser an explicitly active character SHALL add labeled level, eligibility and point cues using the same evaluator as the planner. Switching or clearing the character SHALL update or remove cues. Cues SHALL NOT rewrite base facts, static metadata or search content.

#### Scenario: No active character
- **WHEN** a reference page opens without an active character or without JavaScript
- **THEN** its neutral facts and relation links remain complete and no character-specific eligibility is asserted

### Requirement: Bounded planner delivery
The planner payload SHALL load only on the planner route, separately from reference documents, under an enforced byte budget. Reference context SHALL load only the inputs it needs, not the complete planner payload. Planner controls SHALL be keyboard accessible and remain usable at phone and desktop widths using the shared presentation and voice contracts.

#### Scenario: Reference-only navigation
- **WHEN** a neutral skill page opens without visiting the planner
- **THEN** the full planner payload is not requested

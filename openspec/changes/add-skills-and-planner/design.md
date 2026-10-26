# Design

## Context

See proposal.md for motivation and scope. This is item 2 of the architecture change's [Order of later changes](../establish-reference-architecture/design.md#order-of-later-changes). Implementation consumes that change's graph, claims, rules, presentation and lab contracts and the item reference's publication gates. It does not replace them.

The [Character progression in the game data](../establish-reference-architecture/design.md#character-progression-in-the-game-data) findings are the evidence baseline. They describe build 24464547 and identify progression-1 where observations confirmed the extracted widget and struct data. Recorded outputs belong under `artifacts/<build>/runtime/<run-id>/`, not in this change or Git.

### Established progression evidence

- The skill window reaches level-based `*LBSkillTree` widgets for 21 disciplines through Magic, Melee, Ranged and Tech. Biomancy opens Arcane. Older trees with skill point prerequisites are not reachable through that window.
- A character level grants 1 skill point and 3 level up points. The game instance stores the points while the passive stat component stores the levels.
- A skill unlock costs 1 skill point and needs the slot's discipline level. Ember Flick unlocked at Pyromancy 1.
- Attribute increases cost 1 level up point each and the level up window's decrease buttons refund them. Fortitude raised Max Health from 150 to 175 for one increase. This does not establish every attribute's effect or unrestricted refunds below starting values.
- A discipline level grants 1 proficiency point of that discipline. A passive rank costs its `Point Cost`, requires its discipline level and stops at `Max MultiLevel`. Blazing Fire cost 1 at Pyromancy 2. A second click without points did nothing.
- Pyromancy required 152 experience at level 1 and 156 at level 2. A grant of 50 counted as 51. The reason for that adjustment and its scope remain open.
- An earlier spike called `Set Required EXP` at 14 levels and matched the formula read from bytecode. The existing `experience-requirement` rule in `site/src/lib/rules/experience.ts` cites that character-level function. This is not evidence that discipline progression follows the same curve.
- Tree slots supply skill classes and required levels. Passive buttons supply their texts, requirements, costs and maximum ranks. `SkillsDataTable` supplies skill facts and a tab classification that differs from the skill window's categories. `All_Spellsongs` supplies spellsong records, not proof of their acquisition or applied effects.

## Goals / Non-Goals

### Goals

- Keep reference facts and planner inputs projections of the same catalog entities and conditions.
- Make allocation validity and unknown requirements visible without simulating combat.
- Separate character-level, attribute and discipline progression tracks and their point pools.
- Preserve neutral static pages and add personal context only in the browser.

### Non-Goals

- Crafting, equipment, crafted weapons, armor totals, mobility, runes, enchantments and the planner's second part.
- Reading or writing game saves. Investigating in-memory unlock persistence through the lab does not add a save import feature.
- Armor proficiency training simulation, combat simulation, damage optimization or an automatic build recommendation system.
- Treating every extracted skill as reachable from a tree, or turning passive descriptions into verified numerical effects.

## Decisions

### Extend the graph at its producer

Add discipline, skill, passive and spellsong kinds to `EntityKinds` and the site-data model. Use routes `disciplines`, `skills`, `passives` and `spellsongs` through the existing registry. Retain class-path keys for skills. Use the game's discipline discriminator if it is stable and explicitly connected to its tracking function. Otherwise use the owning tree asset identifier until that association is established. Passive identity uses its owning tree asset and serialized button or node identifier unless the game provides a stronger canonical identifier. Spellsongs use the table-and-row identifier from their source. The identity investigation must establish those associations before payloads or links ship.

Display names are never join keys. Distinct slot identities belong to unlock relation rows, not duplicate skill pages. Several slots can unlock one skill. A canonical passive shared across trees is merged only when source identity or consuming code establishes that it is the same object. Missing or ambiguous associations become coverage gaps.

Extend `CatalogGraph`, its reverse projections and graph checks instead of adding page-only relation arrays. Store discipline membership, slot unlocks and passive availability with typed discipline-level conditions and costs. Establish spellsong connections by following their consumers rather than guessing from names. Extend the existing generated schema and TypeScript type workflow.

### Extract the reachable interface, not obsolete rules

Trace skill-window button bindings to their destination trees and read the level-based slot and passive fields from those assets. Preserve menu categories separately from `SkillsDataTable` tabs. The Biomancy label is an interface label with an Arcane destination, not proof of a new progression track or a defect to announce without further observation.

Publish named skills even when an acquisition path is unknown. Keep them out of allocatable tree choices until their unlock conditions are established. Spell effects and passive descriptions can be shown as game text without asserting computed effects. Apply the existing claim types and consuming-function fingerprint requirements to every new fact producer. Add known issues only after repeated lab observations assign ownership. Otherwise report the disagreement in coverage.

### One allocation evaluator, shared rules underneath

Create a typed progression evaluator over the catalog payload. It returns point ledgers, eligibility results, invalid allocations and explicit unknown inputs. Pages and planner controls consume those results. Point grant, skill cost, attribute cost and refund, passive cost and rank rules live in `site/src/lib/rules/` with worded steps and consuming-function citations. There is no C# allocation evaluator or page-local arithmetic.

Compute grants from an established starting state plus gained levels. Do not assume that level 1 starts with zero points or that current level multiplied by the grant is the earned total. Keep skill points, level up points and each discipline's proficiency points separate. Attribute allocations are increments over the established baseline. A passive's spending is its rank count times its established rank cost, with the maximum and required level checked before accepting an increase.

Level reductions retain affected selections with diagnostics rather than silently deleting them. Deselecting a skill or passive edits a hypothetical plan. It does not promise a supported in-game refund. Derived attributes use only verified shared rules. The Fortitude observation is a check vector, not permission to extrapolate every health contribution.

### Check rules against progression runs

Follow `verification/runtime-lab` and the runtime-checks skill when implementing the lab tasks. Use only `tools/runtime/lab.mjs` against the isolated, silent, off-screen lab copy. Press bound menu handlers and call game functions. Do not assign gameplay fields to create an apparent observation. Function checks may set declared inputs and restore them, and any non-player setup must appear in the run record.

Record boundary vectors for grants, unlocks, insufficient points, attribute increases and refunds, passive thresholds, rank caps and experience. Extend the existing rule adapters and `bun run rules:check <build-id>` path to compare progression rules, including state transitions, with recorded function outputs. An empty vector set must fail for a rule rather than pass as agreement. CI uses invented fixtures and never requires game files. Changed cited fingerprints require new recorded vectors.

Keep character experience and discipline experience separate. Reuse the existing character experience rule only after locating its recorded 14-level evidence and rechecking it for the current build. Verify discipline requirements and the 50-to-51 award through their own tracking consumers. If the full discipline curve cannot be established, return unknown outside the measured domain. The planner can edit levels without pretending to know how much experience reaches them.

### Reference pages stay static

Use the shared entity header, links, relation tables, issue notes, tokens and per-kind tooltips. Follow the architecture's fixed page question order. Apply the existing compact-table disclosure behavior and publication graph checks so all relations remain crawlable. Shared vocabulary entries own labels, units and formatting for attributes, levels, costs, ranks, cooldowns, cast times and experience. Game text is marked separately from site-authored voice.

Add progression entry points to the home hub and navigation only when their routes exist. The registry drives search, sitemap and metadata. Item URLs, facts and browsing behavior stay unchanged.

### Route-scoped payload and explicit budgets

Generate planner inputs separately from entity page documents. Load them only on the planner route. Set an initial uncompressed JSON budget of 512 KiB for that payload, an 8 KiB encoded fragment budget and a 64 KiB expanded build budget. These are delivery limits, not claims about game content. Enforce them before publication or decoding. Measure the real payload before changing a limit and record the reason here rather than removing a gate.

The planner payload contains version and game-build identity, stable entity and slot keys, level domains, costs, ranks, conditions and rule inputs. It excludes prose page bodies, icon binaries and unrelated item documents. Active-character cues use a small page-specific context projection and the shared evaluator, not a download of every tree.

### Build links and local characters are separate

Encode only explicit progression state in a versioned compressed URL fragment. Do not embed local character IDs or local names by default. Use a bounded decoder that rejects malformed input, unsupported versions, duplicate or invalid allocations and expansion beyond its limit before allocating unbounded output. Never silently remap removed keys to similarly named content.

Decode to a preview. A preview cannot overwrite, select or save a browser character implicitly. Cross-build links retain their source build label and are revalidated against the current catalog. Missing keys remain unresolved with a corrective action. The site does not fetch historical game data.

Use a versioned browser store with independent character IDs and an explicit active ID. Support create, rename, edit, duplicate, select and delete. Save atomically so a failed write cannot destroy the last readable state. Report storage failure while preserving the in-memory plan and the option to copy a build link. Migrate only understood store versions. Preserve recoverable entries and report unsupported or corrupt data rather than treating it as an empty collection.

Personal cues are an opt-in browser layer over complete neutral HTML. They show current versus required levels, affordability and unresolved conditions. They never alter canonical facts, metadata or Pagefind content. Clearing the active character returns the neutral view.

## Risks / Trade-offs

- Widget identifiers can change between builds. Source-key decisions and unresolved-key handling prevent name-based links from silently changing meaning.
- Game UI descriptions can disagree with applied effects. Claims, lab checks and coverage preserve that distinction without withholding the reference entity.
- Large trees can make the phone interface unreadable. A list or table carries every requirement even when a tree view is available.
- Persistent builds can outlive game data. Source-build labels and current-catalog revalidation expose that mismatch rather than providing a false migration.
- Local character storage can fail or be cleared. Report failures and keep copyable build links without claiming cloud persistence.

## Migration Plan

Implement after the architecture foundation and item publication contract are available. Add graph kinds, schema and payload producers before routes and planner consumers. Regenerate types and invented fixtures in the same cutover. Existing item routes and browser behavior remain unchanged. There is no existing planner store or build-link format to preserve. Rollback reverts the new producers and routes together without rewriting game files or saves.

## Open Findings and Resolution Gates

These are evidence gaps, not requests to choose product scope. Each resolution task either establishes a rule or leaves a visible unknown. None permits invented defaults.

| Open finding | Resolution and publication boundary |
|---|---|
| Where unlocked skills are stored and what identifies one unlock | Trace the unlock handler's writes and consumers. Record before and after state through the lab, including reopening the tree. This must establish idempotent skill allocation identity before sharing allocatable skills. No save reader is added. |
| Initial character, attribute and discipline levels, starting point balances and level bounds | Measure a fresh lab character and level transitions through game functions. Trace caps and attribute decrease floors. Unknown baselines keep total balances unknown and prevent a valid-build claim. |
| Skill and passive refund behavior outside attribute decreases | Trace handlers and observe any reachable refund action. Planner removal remains hypothetical unless in-game refunds are established. |
| Armor proficiency gain through damage tracking | Trace `Damage Armor EXP Tracking` and the class-specific tracking functions. Run controlled damage observations for armor classes with an unarmored control. Record the finding and coverage, but do not add armor planning or training controls. |
| Exact experience curves beyond observed levels and the 50-to-51 discipline award | Recover the earlier character vectors and record discipline vectors across thresholds, rounding boundaries and award sizes. Cite each track's consumer. Unverified levels or gain adjustments stay unknown. |
| Canonical discipline, passive and spellsong identity and non-tree acquisition | Trace discriminators, serialized widget identifiers, table rows and consumers before fixing keys. Publish unestablished acquisition and associations as gaps rather than borrowing display names or table categories. |
| Unverified passive, attribute and spellsong applied effects | Observe representative effects through their consuming functions. Publish only established calculations. Extracted descriptions remain game text and unresolved effects appear in coverage. |

## Where Findings Live

Use the architecture change's [Where findings live](../establish-reference-architecture/design.md#where-findings-live) table. This design owns identity choices, measurements that justify budgets and reasons for any domain limit. `tasks.md` owns implementation and evidence-resolution work. The catalog's coverage owns unresolved facts encountered during implementation. Reproduced game defects belong only in `issues/known-issues.json`. Required behavior belongs in this change's spec deltas. Run outputs and rule vectors stay in build artifacts. The commit body records planning validation. No README addition is needed for this planning-only change.

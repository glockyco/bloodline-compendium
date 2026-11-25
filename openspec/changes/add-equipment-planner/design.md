## Context

Findings come from Steam build 24464547.

- The character sheet (`UI/CharacterSheet/CharacterSheet`) has 15 equipment widgets: seven body slots, Primary, Secondary, Heavy, Support, Amulet, Mod, Companion and Mount. Its companion slot sits beside a Companion Customization button and its mount slot beside three Mount Selection buttons, which suggests both are chosen through their own screens. No lab run has checked this.
- The Item Bar has eight slots, separate from Equipment and the two skill bars, as comparison 59 records.
- Armor is measured: `establish-reference-architecture`'s design records the armor rule from runs, such as a 20-point hit falling to 17.2 with Feral Maw and the Winter Warden Cuirass, and its walking speeds. `issues/known-issues.json` records Leather Pants adding 0 and knee armor counting only after another piece goes on.
- Walking speed depends on the Light, Medium and Heavy Armor levels. Comparison 46 set those levels aside for Equipment.
- An assembled weapon's damage, speed and value depend on its parts and the grade of each copy, as `add-crafting-reference`'s design records, so Equipment needs that change's model to hold it. Recipe weapons keep their class values.
- The planner keeps builds in a versioned link of sections, and an older planner keeps sections it does not know when it saves or shares again, as comparison 60 decided.

## Goals / Non-Goals

**Goals:**

- A shared build shows the gear it wears, with the armor and walking speed the game would give it.
- Found, bought and crafted gear share one slot and one choice of item.

**Non-Goals:**

- Companion and mount choices, which join with the plan's mounts and companions step and its pages.
- What consumables on the Item Bar do to the character, which joins with the planner's Effects section in that same step.
- Per-build social link previews, which the roadmap puts later.

## Decisions

### After the crafting reference

Equipment ships after `add-crafting-reference`, so its slot entries and link sections are designed once for found and crafted gear. Showing found gear first would leave assembled weapons out and need a second design of the same slots.

### 13 slots and the Item Bar

Equipment covers the 13 worn and held slots, and the Item Bar shares its item choice, as comparison 59 approved. Companion and mount wait for the plan's mounts and companions step, because the sheet appears to pick them through their own screens, which that step's pages explain and check.

### Slot entries

A slot holds either a found or bought item, by its catalog key and version, or a crafted piece, by its base and the choices `add-crafting-reference` publishes for it. Only the slot's acceptance check decides what the choice offers, and it rests on the lab, not on the item list's categories.

### Pieces, not equip order

A build saves the chosen pieces and not the order they went on, as comparison 77 decided, so the same gear is always the same build. The knee armor issue is named on the build instead. How the Equipment tab shows it is a comparison with the finished tab.

### Armor levels in the build

A build saves its Light, Medium and Heavy Armor levels, as comparison 77 decided, so a shared build reproduces the walking speed its author planned rather than a guessed level.

### Links

Equipment, armor levels and the Item Bar are new sections of the current link version, which older planners keep. A full build must stay within 1,500 characters, and tasks measure it with crafted pieces before the format is fixed.

## Risks / Trade-offs

- Crafted pieces carry several choices, so a full build may outgrow the link budget. Choices then encode as indexes into the published lists, and the budget measurement decides before release.
- A slot's acceptance may depend on state the sheet checks at equip time, such as a required level. The planner then offers the piece and marks the condition, as it does for skills above their level.

## Open Questions

- Which items each of the 13 slots and the Item Bar take, including Heavy, Support and Mod.
- Whether armor levels change anything besides walking speed.

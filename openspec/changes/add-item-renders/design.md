## Context

`capture-model` rendered one equippable item at a time in the character sheet's preview diorama, with the light and colour handling that `establish-reference-architecture`'s "Presenting item models" records. It could not render items that cannot be equipped, the female body, translucent materials, or subjects whose engine bounds reach beyond what they draw, and nothing carried a render to the site.

## Goals / Non-Goals

Goals:
- A render for every item the game draws a model for, on both bodies where armor's meshes differ.
- A check that refuses broken renders and lists every item without one with its reason.
- Renders on item pages and in link previews.

Non-goals:
- Creature and person renders, which come with their pages.
- Renders in lists, recipes and hover previews, which keep the icon the game shows there.

## Decisions

### What the render shows

The subject follows what the game draws:
- Armor is equipped and rendered as the preview character wears it, by itself, in the body's rest pose. The preview's idle animation bent the sleeves at the elbow and hunched the torso, and the Erenshor project's item renders (`erenshor-data-mining`, change `show-item-pictures-by-role`, D2) show worn pieces on a hidden body in its rest pose for the same reason. Body meshes come from `ArmorInfo`, and pieces carried at a socket, such as helmets and pauldrons, come from the item's attached actor. Every equip attaches one more copy, and the newest may still be scaled to almost nothing, so the largest copy counts.
  Building armor from its meshes without equipping was tried and dropped. Every modular body mesh carries the same `ModularFantasyHeroCharacters` material, the item's class and its own actor name no colours, and a mesh set on a separate actor, even one following the preview body's pose and taking its material, rendered blue-grey where the equipped Copper Cuirass shows brown leather, in a T-pose, and with magenta skin. Calling the player's or the preview's `EquipItem` directly put on helmets and pauldrons but no body meshes. Equipping through the inventory slot is therefore the one way that shows armor in the colours the game gives it.
- Armor the preview wears without drawing anything, such as Assassin's Belt, is rendered as its own actor instead.
- A weapon is its own actor, spawned in the diorama, so it no longer depends on the preview carrying it. Rendering the carried copy failed for daggers, spears, staves and halberds, which the preview did not show. The largest mesh decides how the actor turns, and every visible mesh of the actor shows, so a staff keeps its orb and The Embersnap its pipe and cog.
- A weapon part is the mesh, materials and scale of its `WeaponComponentTable` row, because its own actor draws nothing. Cores have no mesh in that row and get no render.
- Any other item is its own actor, as it lies in the world.

Of the 2,072 published items, a survey of every item actor (`tools/runtime/checks/item-meshes.lua`) found no mesh for 386, among them every food without a world model, arena contracts, notes, currencies and rings. They get no render.

### Views

Worn armor keeps the three-quarter view from the front, weapons and parts stand face-on turned 15 degrees, and firearms, which now include guns, and crossbows lie on their side. Objects are seen from 25 degrees to the left and 35 degrees above, because their top is what a player sees when the item lies on the ground. The views for each category and slot are approved in Plannotator on sample renders.

### Framing

The camera first frames the engine bounds of the subject. Some skeletal parts, such as pauldrons, report bounds far larger than what they draw, which left the item a few pixels wide. `lab.mjs` therefore measures the subject's pixels in each capture and, when the subject is cut off or fills less than half the frame, captures again in the frame those pixels give, at most three more times.

### Coverage

The depth pass marks opaque surfaces, but translucent materials, such as slime metal, write no depth. The colour pass stores the inverse of the drawn opacity in its alpha, so a sample counts as the subject where the depth marks it or as far as that alpha says something was drawn. A surface that only glows, such as Anointed Halo's ring with the material `M_GoodSky_Sun_Emissive`, writes neither. When both find nothing and the capture's border shows one colour, a sample counts where its colour differs from that backdrop.

### The female body

`new-save <name> female` presses the character creator's own Female button before creating the character. The female sweep renders only armor whose male render showed `SK_Chr_*_Male_*` body meshes, because only those have female counterparts. The capture reports which body the preview showed, from the preview's own body mesh.

### Sweep, check and publication

`render-sweep.mjs` renders every published item and keeps each render under the item's class name in the run. It resumes from the run records, records failures with the game's error and restarts the game only when it stopped answering. `--again` renders chosen items again after a view changes.

`publish-renders.mjs` takes the latest render of each item and body, refuses one without a transparent background, smaller than 64 pixels, covering less than 3% or drawing on its edge, and writes passing renders as WebP of at most 640 pixels to `artifacts/<build>/renders/`, named by hash, with `index.json` and `check.json`. The catalog reads the index, publishes `renders` on each item page with each render's size, and copies the files into the site data, as it does icons. The site writes 320 and 640 pixel versions and a 1200 by 630 share image per render.

### On the page

The render opens the main column above Details, at most 18rem tall, and the game card stays alone in the side column and starts level with the render. This was decided in Plannotator over the other option, above the game card as large wikis show an item's picture at the top of the infobox, because the side column will hold other content later. Where the item has a render on each body, Male and Female buttons switch it, with the male body first as character creation starts with it.

## Risks / Trade-offs

- About 2,400 renders take three to four hours of lab time per build. The sweep resumes, so an interrupted run loses at most one item.
- Renders come from the lab and never enter the repository, so a fresh clone builds the site without them until the sweep and the publish step run.
- A render frames what the preview draws. A piece the preview draws differently from the world, such as a belt worn under a coat, shows as the preview draws it.

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
  Building armor from its meshes without equipping was tried and dropped. Every modular body mesh carries the same `ModularFantasyHeroCharacters` material, the item's class and its own actor name no colours, and a mesh set on a separate actor, even one following the preview body's pose and taking its material, rendered blue-grey where the equipped Heavy Iron Cuirass shows brown leather, in a T-pose, and with magenta skin. Calling the player's or the preview's `EquipItem` directly put on helmets and pauldrons but no body meshes. Equipping through the inventory slot is therefore the one way that shows armor in the colours the game gives it. The attempt was made before captures loaded textures whole (see "Textures") and was not repeated afterwards.
- Armor the preview wears without drawing anything is rendered as its own actor instead.
- A mesh that an item hangs on another character is left out. Assassin's Belt, lying in the world, hangs one of its daggers on the player's body, which stretched the frame over the whole level and left no subject in it. Without that mesh the belt renders as the preview wears it.
- Of the item's attached actor, the meshes count that hang at a socket of the body, by themselves or with the actor. The actor also hangs on a character that wears a piece with body meshes, at no socket, and keeps the model it has where the item lies in the world: Leather Bracers showed a third bracer at the character's feet, and Gale Runners a second pair of boots inside the legs, while their wings hang at the calves and count.
- A body part counts when its bounds meet the body's. The mesh in Chainmail Hood's `ArmorInfo` was made for another skeleton, so the part that draws it lies at the level's origin, which stretched the frame over the level and left the hood to show as an object on its back. Without that part the hood shows as its actor hangs at the head.
- What a hand holds, which is whatever fills the Primary, Secondary or Shield slot, is its own actor, spawned in the diorama, so it does not depend on the preview carrying it. Rendering the carried copy failed for daggers, spears, staves and halberds, which the preview did not show. Every visible mesh of the actor shows, so a staff keeps its orb and The Embersnap its pipe and cog. All 369 weapons fill one of these slots and no other item does. Choosing by weapon type had left the 32 shields without one to show as objects from above.
- A weapon part is the mesh, materials and scale of its `WeaponComponentTable` row, because its own actor draws nothing. Cores have no mesh in that row and get no render.
  17 part classes stand in more than one row, and the rows show different parts, because the game lets one item serve for each of them: Beveled Copper Shaft's class also stands in the row of Beveled Iron Shaft. The item's own row is the one that carries the item's name, which decides 14 of them, or else the one named like its class, which decides the other three, whose names differ from their row's by a letter or stand in two rows. A sweep had taken the last row of each class and showed 16 of the 17 as another part, Emberglass Wand Focus as Galeheart Wand Focus among them. A class that neither name tells apart gets no render.
- Any other item is its own actor, as it lies in the world.

Of the 2,072 published items, a survey of every item actor (`tools/runtime/checks/item-meshes.lua`) found no mesh for 386, among them every food without a world model, arena contracts, notes, currencies and rings. They get no render.

### Views

The views were decided on sample renders in Plannotator (`artifacts/design/process/trial/44-renders.md`):
- Worn armor: each piece by itself, three quarters from the front, 25 degrees to the left and 12 degrees above.
- Pieces worn on the back, such as wings and packs: from behind.
- Melee weapons, staves and weapon parts: standing, grip down, face-on, turned 15 degrees so the thickness shows.
- Bows: standing, seen from the side, with the nocked arrow where the model has one.
- Crossbows and firearms: lying, with the muzzle to the left. The samples had the muzzle to the right and some pointing the other way.
- Shields: face-on, turned 15 degrees. In the samples most shields showed as objects from above.
- Other items: as they lie, 25 degrees to the left and 35 degrees above, because their top is what a player sees when the item lies on the ground.

The full set was reviewed again in Plannotator (`artifacts/design/process/trial/45-render-results.md`): firearms, crossbows and shields as they are now, weapon parts as they sit in a weapon that stands tip up, worn armor as the game draws it on the body without the copy of the item's world model that five pieces keep inside the character, and no render for an item without a model. An item whose model is shared with items of other names, such as Beef's gold key, gets no render, because that model says nothing true about it. `publish-renders.mjs` finds them by comparing renders and lists them in `check.json` as stand-ins.

### Which way a held item points

The first renders turned a weapon by its largest mesh alone: the longest axis upright or lying, the end farther from the mesh's origin up or to the right. That end was the wrong one for a mesh modelled the other way round or held near its middle, which showed copper daggers and a soulstaff upside down, two crossbows on end and guns pointing both ways.

The game attaches a held item's actor to a socket of the hand without turning it (relative rotation and location are zero), so the actor is built in the socket's frame. `tools/runtime/checks/hand-frame.lua` reads the direction the player looks and the direction that is up in that frame for each item the first-person body holds (run `2026-11-22T00-32-05-749Z-handframe1`):

| Held | Socket | The player looks along | Up |
|---|---|---|---|
| Gun, pistol | Gun | +Y | +Z |
| Crossbow | Crossbow | +Z | +Y |
| Bow | Bow2 | -X | +Z |
| Shield beside a sword | KiteshieldSlot | the eye is on the -Y side | +Z |
| Shield beside a spear | SpearShield | +Y | +Z |

The largest mesh still lends its axes, so the picture is square to the item, and the socket decides between an axis's two ends:
- A firearm's longest axis is its barrel and its thinnest its width. The end toward the socket's +Y lies to the left and the side toward +Z is up. Lonland Rifle, Undead Rifle and Dwarven Grenade Launcher are modelled along other axes than the common muskets and now point the same way.
- A crossbow's thinnest axis is its height and faces the camera with its +Y side, so it shows from above. Of the other two, the one along the socket's Z is the barrel, because the limbs of Light Crossbow and Thorn Crossbow span more than their length.
- A shield's thinnest axis faces the camera with its +Y side, the side away from the player's eye, and the axis along the socket's Z is up. Lonland Shield's mesh is turned around in its blueprint, which the mesh alone cannot tell. An item whose thinnest axis is the socket's Z lies flat in the hand, as the six open spellbooks do, and shows as an object. Wings of the Wall lies on its side because its blueprint turns it so.
- A bow stands along the socket's Z axis, because Warpwood Bolter is two staves that each lean, and its -X side, where the arrow flies, lies to the left.
- Any other weapon stands along its longest axis with the end farther from the hand up, measured over all of its meshes from the actor's own place, so a blade on a separate hilt counts. Where the hand holds the weapon within a tenth of its length from the middle, the end toward the socket's +Z is up, along which these sockets hold their weapons. On the sheets of all 33 spears and all 25 staves, which the hand mostly holds near the middle, the head is up. The six lutes of the game's weapon class Lute hang from the hand by the neck, so their end toward +Z is up too. The plain Lute is a one-handed weapon of the class Shortsword that the hand holds by the neck with the body up, and stands like that.

A firearm, a crossbow, a shield and a bow are measured by their largest mesh alone, because what is fitted to it reaches sideways: The Embersnap's pipe made the pistol show from above.

Reading the held copy's pose at each capture was tried and dropped. An equipped item hangs on the first-person and on the third-person body at different sockets, the pose depends on what the other hand holds (a shield without a weapon beside it hangs at the side with its top forward), and a body the game does not draw keeps its rest pose. The socket's axes are the same for every item it holds.

### Which way a weapon part stands

A part first stood on its longest side with the end farther from its mesh's origin up, on the assumption that the origin is where the part joins the weapon. It is not: the origin of Gold Kilji Blade's mesh lies nearer its tip, so the blade stood on its tip, as did Kora and Yataghan blades, and pommels stood upside down. Parts of one kind also differed: 25 of 46 axe heads are longest across the shaft and stood with the blade's edge up, and most crossguards stood on end.

The game joins parts at sockets on their meshes. `tools/runtime/checks/part-sockets.lua` read the box and the sockets of all 385 part meshes (run `2026-11-22T02-11-31-811Z-sockets1`), and every mesh is modelled as it sits in a weapon that points up its Z axis:
- The `HandleSocket` of every blade, dagger blade, greatblade, rapier blade and spear head lies in the lower half of its box, mostly at the very end.
- Every shaft's `HandleSocket` or `PommelSocket` lies below its `BladeSocket`, and every grip's `PommelSocket` below its `GuardSocket`.
- No guard's `HandleSocket` lies above its `BladeSocket`.
- The socket of every pommel and every wand focus, which hangs under the shaft as a pommel does, lies in the upper half, except on one mesh that also serves as a guard.

So a part stands with its mesh's Z axis up and the thinner of its two level sides toward the camera, which shows every part as it sits in a weapon that stands with its tip up. Of the 762 parts with a mesh, 83 are thinnest along Z: crossguards, plates that the blade passes through, and some flat pommels, mace heads, hammer heads and wand foci. Edge-on they would show a sliver, so they show their face from the blade's side with their longer side level, as a guard lies across a weapon.

### Textures

The lab game runs render mode with `-NoTextureStreaming`. A capture is taken two frames after an item appears, and with streaming an item's textures were often still loading: Warpwood Bolter's first capture of a session came out grey (mean colour 133, 135, 105) and its second green (145, 144, 92). With textures loaded whole, the first and the second capture of a session match.

### Lights

Every capture is lit by the preview's own light alone. The lights an item carries are switched off, both on an actor spawned for a capture and on whatever the preview character wears, because a worn piece stays on the character until the next piece of armor is captured. Altarlight Pauldrons' two candle lights lit every capture after them: in a sweep the mean brightness of renders rose from about 100 to about 230 from the pauldrons onwards. An earlier fix had blamed the Acorn Lute, which a sweep renders just before them.

The preview's point light keeps the intensity the game gives it, 2000. It clips the brightest surfaces to white: the flat faces of silver and steel weapon parts and the character's skin. At 700 the share of pure white on Curved Silver Axe Head fell from 69% to 1%, but the part still showed as one pale face, and an apple, a shield, an ingot and a cuirass barely changed (run `2026-11-22T01-24-43-048Z-lighttest`). The game's sheet shows the character's skin as pale as the renders do, and the sample renders that the views were decided on had the same white, so the game's light stays.

### Colours of worn armor

183 armor classes colour the body themselves: on `ReceiveBeginPlay` each makes a dynamic material from `ModularFantasyHeroCharacters`, sets its colours and puts it on the body parts it covers. The other armor classes set no material, so their parts show whatever material the body carries. On a new character that is the character's own. After a piece that colours the same parts, it is that piece's: Bandit's Armored Skirt rendered after Askillian Greaves showed the greaves' purple and bright pink skin, because the greaves cover the legs and leave the skin colour unset. A sweep renders items by name with the slots mixed, and four of its first 148 armor renders had such pink skin (Forearm Straps, Curdstompers, Bandit's Armored Skirt, Cloth Foot Wraps). This was first put down to textures still loading, which it was not.

Clicking an equipment slot, as a player does to take a piece off, puts the character's own material back on that piece's parts, on the first-person body and on the preview. `unequip-all` clicks every slot that holds an item, and `lab.mjs` runs it before each worn capture, so every piece shows as on a new character that wears nothing else. Seventeen pieces rendered in one order and then in the reverse order had the same mean colour both times and no pink (run `2026-11-22T01-38-16-806Z-colours1`).

Taking a piece off does not clear every part the piece coloured. Archer's Straps and Single-Arm Chain Sleeve colour the two elbow parts, which kept that material after the piece came off (run `2026-11-22T03-55-11-665Z-oddities1`). Pauldrons draw their right half on an elbow part too, so in a sweep Spiked Pauldrons and Standard Pauldrons showed the sleeve's brown leather inside one pauldron. `capture-model` therefore gives every part of the player's bodies and of the preview the character's own material before the piece goes on. Sixteen pieces, these among them, then rendered alike in both orders.

A second sweep of all armor with that reset changed more than the pauldrons. Leather Breastplate, which sets no colours of its own, had shown another piece's brown leather and shows the character's grey now, and 43 armor renders differ visibly between the two sweeps.

The material's defaults for the three skin colours, `Color_Skin`, `Color_Stubble` and `Color_Scar`, are a bright pink. A piece that colours its parts sets the ones it shows and leaves the others at the default where the body hides them: inside the collar of Askillian Soldier Cuirass, which the head covers, the render showed a pink patch. A render shows the piece without the body, so each of these colours that a shown part's material left at the default takes the character's own. A colour the piece set stays.

### Framing

The camera first frames the engine bounds of the subject. Some skeletal parts, such as pauldrons, report bounds far larger than what they draw, which left the item a few pixels wide. `lab.mjs` therefore measures the subject's pixels in each capture and, when the subject is cut off or fills less than half the frame, captures again in the frame those pixels give, at most three more times.

A frame that cut the subject off grows by half. It first doubled, and Bard's Hat then never fitted: seen from the nearer camera of the frame its pixels gave, the hat's near side grew past the edge, the doubled frame showed it at 44% of the capture, which counted as too small, and the next frame cut it off again.

### Coverage

The depth pass marks opaque surfaces, but translucent materials, such as slime metal, write no depth. The colour pass stores the inverse of the drawn opacity in its alpha, so a sample counts as the subject where the depth marks it or as far as that alpha says something was drawn. A surface that only glows, such as Anointed Halo's ring with the material `M_GoodSky_Sun_Emissive`, writes neither. When both find nothing and the capture's border shows one colour, a sample counts where its colour differs from that backdrop.

### The female body

`new-save <name> female` presses the character creator's own Female button before creating the character. The female sweep renders only armor whose male render showed `SK_Chr_*_Male_*` body meshes, because only those have female counterparts. The capture reports the body from the player's `Male?` flag, by which the game chooses each piece's mesh. It first read the name of the preview's body mesh, which is the same for both bodies, so a whole female sweep reported the male body and none of its renders counted. The sweep now stops when a capture reports another body than the one it sweeps.

### Sweep, check and publication

`render-sweep.mjs` renders every published item and keeps each render under the item's class name in the run. It resumes from the run records and records failures with the game's error. When the game stopped answering or the character kept a piece on, it restarts the game and tries the item once more. `--again` renders chosen items again after a view changes, and a render taken again replaces the item's earlier failure.

Two weapons failed their first capture and rendered on a second: Apprentice's Soulstaff drew no mesh a moment before the game stopped answering, and Chitin Shield's capture showed nothing. In a new game both rendered at once, and the first and second capture of 46 items matched. A second sweep then rendered all 1,267 held items, parts and objects again. Apart from the 17 parts whose row changed, the two renders of an item differ only where the model's glow moves, on seven wand shafts with orbs, spirals or gems, Ethereal Claymore and Ethereal Slicer.

`publish-renders.mjs` takes the latest render of each item and body, refuses one without a transparent background, smaller than 64 pixels, covering less than 3% or drawing on its edge, and writes passing renders as WebP of at most 640 pixels to `artifacts/<build>/renders/`, named by hash, with `index.json` and `check.json`. The catalog reads the index, publishes `renders` on each item page with each render's size, and copies the files into the site data, as it does icons. The site writes 320 and 640 pixel versions and a 1200 by 630 share image per render.

### On the page

The render opens the main column above Details, at most 18rem tall, and the game card stays alone in the side column and starts level with the render. This was decided in Plannotator over the other option, above the game card as large wikis show an item's picture at the top of the infobox, because the side column will hold other content later. Where the item has a render on each body, Male and Female buttons switch it, with the male body first as character creation starts with it.

## Risks / Trade-offs

- About 2,400 renders take three to four hours of lab time per build. The sweep resumes, so an interrupted run loses at most one item.
- Renders come from the lab and never enter the repository, so a fresh clone builds the site without them until the sweep and the publish step run.
- A render frames what the preview draws. A piece the preview draws differently from the world, such as a belt worn under a coat, shows as the preview draws it.

## 1. Rendering in the lab

- [x] 1.1 Render weapons as their own actor, weapon parts from their table row and other items as their own actor, fall back to the own actor for armor the preview does not draw, and verify a sword, a staff with an orb, an ingot and a blade part
- [x] 1.2 Count translucent surfaces through the colour pass's alpha, and verify a slime-metal blade renders
- [x] 1.3 Refine the frame from a capture's pixels when the subject is cut off or small, and verify Frostplate Pauldrons fill their render
- [x] 1.4 Create a female character with `new-save <name> female`, report the preview's body with each capture, and verify Heavy Iron Cuirass renders the female torso
- [x] 1.5 Add `render-sweep.mjs`, which renders every item on a body, resumes, records failures and renders again on request, and verify it on a sample of every category and slot

## 2. Check and publication

- [x] 2.1 Add `publish-renders.mjs`, which checks the latest render of each item and body, writes passing renders with their size and an index, and lists failed renders and unrendered items with reasons
- [x] 2.2 Publish each item's renders from the index in the catalog and copy them into the site data, and verify the catalog fails on an index entry without its file
- [x] 2.3 Write render sizes and share images in the site assets

## 3. Review

- [x] 3.1 Decide the views for each category and slot, including bows and shields, and the render's place on the item page in Plannotator, and apply the answers
- [x] 3.2 Render every item on both bodies, publish the renders, and review every failed render and unrendered item, with each decision recorded

## 4. Site

- [x] 4.1 Show the render above Details in the main column with a Male and Female choice where an item has both, and verify it with invented fixture renders
- [x] 4.2 Preview a shared item link with its render, and verify the share image and card type
- [x] 4.3 Document the sweep, the female body and the publish step in the runtime-checks skill and the README's update workflow

## 5. Integration

- [x] 5.1 Run every check, build real data with renders, review every page kind at 1440 and 390 px, deploy to staging and production, and record the review in the commit bodies

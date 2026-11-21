## Why

Item pages show each item only through its inventory icon, which is small and often generic, so a player cannot see what a weapon, a piece of armor or a crafting part looks like. The lab can already render one item the way the character sheet's preview draws it, and no other source of The Bloodline shows its items as 3D models.

## What Changes

- The lab renders every item that the game draws a model for: armor as the preview character wears it, on the male and on the female body where the body meshes differ, weapons standing or lying by kind, weapon parts from the mesh their table row names, and every other item as its own actor lies in the world.
- A sweep renders all items of a build in the lab, resumes where it stopped and records each failure with the game's reason.
- A check refuses a render without a transparent background, too small, nearly empty or touching its edge, and lists every item without a render with the reason. Only renders that pass are published.
- The catalog publishes each item's renders by body, and the site shows them on item pages and as link preview images.

## Capabilities

### New Capabilities
- `verification/item-renders`: how the lab renders, checks and publishes item models, and which items have none.

### Modified Capabilities
- `site/item-page`: an item page shows the item's render, with a choice of body where the item has two.
- `site/item-reference`: a shared item link previews the item's render.

## Impact

- `tools/runtime/`: `capture-model.lua`, `lab.mjs` (`new-save` with the female body, translucent subjects), new `render-sweep.mjs`, `publish-renders.mjs` and `renders.mjs`, and `create-character.lua`.
- The catalog: `RenderIndex`, `ItemPage.Renders`, `CatalogWriter` copies renders into the site data.
- The site: `scripts/site-assets.mjs` sizes renders and share images, the item page and `SeoHead.svelte`.
- About 2,400 more image files in the deployed site, inside the hosting limits that the asset gate checks.

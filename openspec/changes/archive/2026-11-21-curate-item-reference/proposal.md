## Why

The item reference shows the catalog's data as the catalog stores it instead of answering what players ask: how to get an item, whether it is better than what they have, what it is for, and what it takes to make. A review against the Afallon, Ancient Kingdoms and Erenshor compendiums, an audit of every page kind on real data, an audit of the game data and research into what players ask found three kinds of problems:

- Wrong values. Potion ingredient treatments come from a field the game no longer reads, shop prices are base values that every shop multiplies, container odds use one pick too few, and rare drop pools are claimed per kill for enemies that never roll them.
- Missing answers. About 100 of the 263 items without a source come from mechanisms the catalog does not read yet. Recipes lack their station, crafted weapons and armor lack their assembly rules, settlement shops read "Location unknown", and 49 places have no name.
- Careless presentation. Pages talk about the site instead of the game, repeat generic warnings under every source, show pipeline words and raw identifiers, hide recipe ingredients behind a disclosure, repeat facts in a Related entities section, offer one mixed Type filter with empty options, hide 263 items from name search, and show numbers players cannot use.

Group 8 of `establish-reference-architecture` planned the item page rebuild. This change replaces it with a plan grounded in those findings.

## What Changes

- Publish the values the game applies: potion ingredient treatments from `AllIngredientEffects`, prices as base values with the shop's pricing rule, odds per container, kill or delivery from the game's selection code, and rare drops only where a death path rolls them. Lab runs confirm each.
- Mark items that only test levels hold as not obtainable in normal play.
- Publish recipe context: station, discipline and level, experience and output.
- Replace generic condition warnings with specific conditions in player terms, name settlement buildings as the player's settlement, and name the remaining places.
- Port reader-text rules: no text about the reference itself, no pipeline words or raw identifiers, uncertainty attached to the fact it affects with its consequence, title and sentence casing, correct plurals.
- Rebuild item browsing: separate filters for weapon type, armor slot, armor class, weapon part, effect, how to get and where, counts that follow the other filters, columns for each category, stat filters with Any or All, name search over every item, and search results that open filtered lists.
- Rebuild item pages: a first-screen answer with source verbs and conditions, a visible recipe line, Used for with quantities and filters, labelled odds, the game's tooltip value beside the applied value where they differ, sources grouped by place, previews on entity links, and no Related entities section.
- Reproduce every known issue in the rendered game, by what the player sees and by its effect, and capture every item's tooltip as the game draws it.
- Prepare for more kinds with pages: one preview presenter for every kind, search entries from the kind registry and a review of entities that share a name.
- Link a GitHub issue form for wrong facts from every page.
- **BREAKING** Remove the Coverage page from the site. Its diagnostics move to a maintainer report beside the site data, and About explains the reference's limits in one place. Known Issues shows players their effect and workaround first and keeps developer evidence in a collapsed section.

## Capabilities

### New Capabilities

- `catalog/applied-item-facts`: prices, odds, ingredient treatments and item status as the game applies them.
- `catalog/acquisition-recovery`: crafting stations, specific conditions and place names.
- `site/reader-text`: how the site words facts, unknowns and explanations.
- `site/item-browsing`: the item list, its filters, columns and search.
- `site/item-page`: the order and content of an item page.
- `site/presentation`: spacing roles, layout primitives and the layout check.
- `site/support-pages`: About, Known Issues, the maintainer report that replaces Coverage, the home page and missing pages.

### Modified Capabilities

None. The capabilities this change builds on belong to changes that are not archived yet, so this change adds its own.

## Impact

- Catalog: `ItemFacts.cs`, the source mechanisms, `WorldSourceData.cs`, `Places.cs`, `CatalogBuilder.cs`, `CatalogWriter.cs`, `TooltipCheck.cs`, `KnownIssues.cs`, and the site-data contract in `SiteDataModel.cs` with regenerated types.
- Site: the item list, item page, tooltip, condition and source components, labels, the rules module, About, Known Issues, the home page, the footer, the sitemap and search records. The Coverage route is removed.
- Lab: checks for purchases and sales, container picks, rare drops, known issues and noted armor, and tooltip screenshots.
- `establish-reference-architecture`: group 8 is replaced by this change.
- `add-crafting-and-equipment` takes over crafting models, hand-mapped recipes, recipe learning, the weapon builder and armor arithmetic. `openspec/plan.md` places stables, contracts, seasonal gifts, archaeology, level rewards, faction shop defaults, enemy rare weapons, the settlement Blacksmith's stock, the respect control and seasonal state with the changes for their kinds.

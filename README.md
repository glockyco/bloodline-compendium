# The Bloodline Compendium

A searchable wiki for players of [The Bloodline](https://store.steampowered.com/app/1159290/The_Bloodline/), generated from the game's files. This unofficial fan project is not affiliated with Shieldbearer Studios or 505 Games.

[Open the compendium](https://the-bloodline.compendiums.org/) · [Support on Ko-fi](https://ko-fi.com/wowmuch)

## Use the compendium

- Browse or search items and follow their acquisition routes and uses.
- Read About for what the compendium is, how it is made and which game release it covers.

You do not need the game to use the published site. This checkout is for changing the project or rebuilding its data.

## Setup

Use `nix develop` on macOS or Linux. Without Nix, install the tools declared in [flake.nix](flake.nix), using the SDK and Bun versions declared in [global.json](global.json) and [package.json](package.json).

From the repository root:

```sh
dotnet tool restore
dotnet restore --locked-mode
bun install --frozen-lockfile
bun run --cwd site playwright install chromium
```

### Run the site with fixtures

```sh
bun run --cwd site dev
```

The invented fixtures need no game installation and cannot be published as production data. To work with extracted data instead, set `BLOODLINE_SITE_DATA` to its absolute site-data directory.

## Extract and publish

Copy [bloodline.example.json](bloodline.example.json) to `bloodline.local.json` and fill in the local Steam library setting. Keep game files and extracted output out of Git. The Steam installation remains read-only.

```sh
dotnet run --project src/Bloodline -- extract
bun tools/runtime/lab.mjs prepare
bun tools/runtime/lab.mjs launch
bun tools/runtime/lab.mjs new-save <name>
bun tools/runtime/lab.mjs close-tutorials
bun tools/runtime/tooltip-sweep.mjs <build>
bun tools/runtime/harvest-sweep.mjs <build>
bun tools/runtime/lab.mjs quit
bun tools/runtime/render-sweep.mjs <build>
bun tools/runtime/render-sweep.mjs <build> female
bun tools/runtime/lab.mjs quit
bun tools/runtime/publish-renders.mjs <build>
dotnet run --project src/Bloodline -- catalog
```

The tooltip sweep reads every item's tooltip in the running game, and cataloging uses those readings to name the items each known issue affects. A site build stops when a known issue affects no item, which means the sweep did not run for the build. Save names are at most 20 characters. The harvest sweep opens every level that places harvestables or their spawners, in about an hour, and reads the harvestables in play, which cataloging compares with what it says each level gives.

The render sweeps start their own render-mode games and render every item's model, then the armor with female meshes on the female body, in about an hour and a half. Both resume where they stopped. `publish-renders.mjs` checks every render and exits 1 when one fails, and its `check.json` beside the build's renders lists each failed render and each item without a render with its reason. Review that list before publishing, and render items again with `render-sweep.mjs <build> --items <class path>,... --again` after a view changes.

After a game update, review and add the build's entry in [releases.json](releases.json), repeat extraction and cataloging, compare the new output with the previous build, recheck every known issue as the [game-issues skill](.agent/skills/game-issues/SKILL.md) describes, run the skill and gathering checks again as the [runtime-checks skill](.agent/skills/runtime-checks/SKILL.md) lists them, run `bun run rules:check <build>`, and review the catalog's `report.json` beside the site data before publishing. `rules:check` compares the site's rules, such as prices, experience and proficiency points, with every result the build's lab runs recorded.

Extraction writes the Blueprint function cross-reference to each build's `records/code/index.json`. Cataloging writes the type, inputs and function citations of every published item, skill and passive fact, keyed by kind and entity key, to `claims.json` beside `site/`, without adding them to site data. When lab runs of the build have read item tooltips, cataloging also compares every item's facts with its tooltip, writes each difference and its known cause to `tooltip-check.json`, and lists differences without a known cause in `report.json`. With the latest runs of `skill-tooltips.lua` and `skill-trees.lua`, it compares every skill's facts and every tree's slots and passives with what the game built in the same way, with the latest harvest sweep it compares every level's harvestables in play with the presets it gives there and fails on a difference, and it fails when the game's harvest presets differ from `src/Bloodline/Content/harvest-presets.json`, which gives each preset's loot and gathering level. After extracting a new build, list the facts whose cited code needs review:

```sh
dotnet run --project src/Bloodline -- compare artifacts/<previous-build> artifacts/<current-build>
```

To review real data locally without uploading:

```sh
BLOODLINE_SITE_DATA=<absolute-site-data-directory> bun run --cwd site build:preview
bun run --cwd site review
```

Every build stops when a page breaks the voice rules in `scripts/voice-rules.json`, links to a page that does not exist, or exceeds its payload budget in `scripts/payload-gate.mjs`.

Before committing a change to the site's interface, check the real-data pages that stress layout most at four widths against the running development server:

```sh
bun run --cwd site layout:check <site-data-directory>
```

Authenticate Wrangler with `bunx wrangler login` from `site/`. Commit changes and rerun `catalog` before the production build:

```sh
BLOODLINE_SITE_DATA=<absolute-site-data-directory> bun run --cwd site build
bun run --cwd site deploy:preview
```

Review the staging URL before deploying production:

```sh
bun run --cwd site deploy:production
```

To roll back:

```sh
bun run --cwd site deploy:versions
bun run --cwd site deploy:rollback -- <version-ID> --message "<reason>"
```

`bloodline.compendiums.org` redirects every path to the site's address through its own Worker, which changes only with `site/src/redirect-worker.ts`:

```sh
bun run --cwd site deploy:redirect
```

## Observe the running game

Facts that the game data cannot settle are checked in a modded copy of the game, which `tools/runtime/lab.mjs` runs off screen through CrossOver on macOS. Make the copy once per game build, then start it:

```sh
bun tools/runtime/lab.mjs prepare
bun tools/runtime/lab.mjs launch
```

The [runtime-checks skill](.agent/skills/runtime-checks/SKILL.md) lists the commands and the procedure. Runs land in `artifacts/<build>/runtime/`.

## Change the project

Read [AGENTS.md](AGENTS.md) for contribution boundaries and browser-review requirements. [openspec/](openspec/) holds behavior requirements and change designs. [SiteDataModel.cs](src/Bloodline/SiteData/SiteDataModel.cs) defines the site contract. Regenerate its consumers with `bun run types` after changing it.

Stored characters and shared build links outlive releases. Raise `PlannerFormat` in [SkillProducer.cs](src/Bloodline/Catalog/SkillProducer.cs) when a field of `planner.json` changes meaning, and `linkVersion` in [link.ts](site/src/lib/planner/link.ts) when the link's data changes shape, because an old link must fail with a message rather than open a different build.

Update dependencies by hand. Leave the Renovate app disabled for this repository, and review CUE4Parse updates against regenerated site data because a parser change can change published facts.

Run the same checks as CI:

```sh
dotnet csharpier check . && dotnet build --no-restore && dotnet test --no-build
bun run types:check && bun run guard
bun run --cwd site lint && bun run --cwd site format:check && bun run --cwd site check
bun run --cwd site test:unit && bun run --cwd site build:fixtures && bun run --cwd site test:e2e
```

In browser accessibility tests, expand content through the enhanced controls before scanning it, because hydration replaces the static fallback disclosures. Keep checks of no-JavaScript disclosures separate from the enhanced page checks.

When planting DOM defects in browser tests, wait for an enhanced control before changing the page, because hydration can rebuild server-rendered nodes.

For OpenSpec validation on Apple silicon Macs or Linux:

```sh
nix build --no-link .#checks.<system>.openspec
```

If extraction reports a changed build, wait for Steam to finish updating before retrying. If Playwright cannot find Chromium, repeat the browser installation command in Setup.

## License

The code is licensed under the [MIT License](LICENSE). The Bloodline and its names, artwork and assets belong to their respective owners.

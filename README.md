# The Bloodline Compendium

A searchable item reference for players of [The Bloodline](https://store.steampowered.com/app/1159290/The_Bloodline/), built from the game's own data. This unofficial fan project is not affiliated with Shieldbearer Studios or 505 Games.

[Open the compendium](https://bloodline.compendiums.org/) · [Support on Ko-fi](https://ko-fi.com/wowmuch)

## Use the compendium

- Browse or search items and follow their acquisition routes and uses.
- Check the release information and Coverage page before relying on incomplete information.
- Read About for the extraction method and the distinction between the game release and the data update.

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
dotnet run --project src/Bloodline -- catalog
```

After a game update, review and add the build's entry in [releases.json](releases.json), repeat extraction and cataloging, compare the new output with the previous build, recheck every known issue as the [game-issues skill](.agent/skills/game-issues/SKILL.md) describes, and review the catalog's `report.json` beside the site data before publishing.

Extraction writes the Blueprint function cross-reference to each build's `records/code/index.json`. Cataloging writes fact types, inputs and function citations to `item-claims.json` beside `site/`, without adding them to site data. When lab runs of the build have read item tooltips, cataloging also compares every item's facts with its tooltip, writes each difference and its known cause to `tooltip-check.json`, and lists differences without a known cause in `report.json`. After extracting a new build, list the facts whose cited code needs review:

```sh
dotnet run --project src/Bloodline -- compare artifacts/<previous-build> artifacts/<current-build>
```

To review real data locally without uploading:

```sh
BLOODLINE_SITE_DATA=<absolute-site-data-directory> bun run --cwd site build:preview
bun run --cwd site review
```

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

## Observe the running game

Facts that the game data cannot settle are checked in a modded copy of the game, which `tools/runtime/lab.mjs` runs off screen through CrossOver on macOS. Make the copy once per game build, then start it:

```sh
bun tools/runtime/lab.mjs prepare
bun tools/runtime/lab.mjs launch
```

The [runtime-checks skill](.agent/skills/runtime-checks/SKILL.md) lists the commands and the procedure. Runs land in `artifacts/<build>/runtime/`.

## Change the project

Read [AGENTS.md](AGENTS.md) for contribution boundaries and browser-review requirements. [openspec/](openspec/) holds behavior requirements and change designs. [SiteDataModel.cs](src/Bloodline/SiteData/SiteDataModel.cs) defines the site contract. Regenerate its consumers with `bun run types` after changing it.

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

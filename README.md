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

After a game update, review and add the build's entry in [releases.json](releases.json), repeat extraction and cataloging, compare the new output with the previous build, and review Coverage before publishing.

To review real data locally without uploading:

```sh
BLOODLINE_SITE_DATA=<absolute-site-data-directory> bun run --cwd site build:preview
bun run --cwd site review
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

## Change the project

Read [AGENTS.md](AGENTS.md) for contribution boundaries and browser-review requirements. [openspec/](openspec/) holds behavior requirements and change designs. [SiteDataModel.cs](src/Bloodline/SiteData/SiteDataModel.cs) defines the site contract. Regenerate its consumers with `bun run types` after changing it.

Run the same checks as CI:

```sh
dotnet csharpier check . && dotnet build --no-restore && dotnet test --no-build
bun run types:check && bun run guard
bun run --cwd site lint && bun run --cwd site format:check && bun run --cwd site check
bun run --cwd site test:unit && bun run --cwd site build:fixtures && bun run --cwd site test:e2e
```

For OpenSpec validation on Apple silicon Macs or Linux:

```sh
nix build --no-link .#checks.<system>.openspec
```

If extraction reports a changed build, wait for Steam to finish updating before retrying. If Playwright cannot find Chromium, repeat the browser installation command in Setup.

## License

The code is licensed under the [MIT License](LICENSE). The Bloodline and its names, artwork and assets belong to their respective owners.

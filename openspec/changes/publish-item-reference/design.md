# Design

## Context

See proposal.md for motivation. Measurements below come from spikes on Steam build 24464547 (2026-10-25).

- The game is Unreal Engine 4.27 with one IoStore container of 42,277 packages, Zlib-compressed and unencrypted. Packages use tagged properties, so no AES key or `.usmap` mappings are needed.
- Gameplay is Blueprint. The native game module holds seven utility classes, so rules live in Blueprint bytecode inside the packages.
- Items are 2,161 Blueprint classes descending from `BP_BaseItemAsset_C`. Their class default objects store only overrides of their parents. 48 DataTables hold shops, armour, weapon components, skills, quests and loot presets.
- The shipped container also contains demonstration content from marketplace asset packs, for example 569 packages under `BossAIToolkit/Demo`. Real cities and NPCs also live under asset-pack folders, so folder names do not separate the two.
- Contributors may use macOS, Windows or Linux. CI never has game files.

## Goals / Non-Goals

**Goals:**

- One path from a pinned build to a deployed page, shared by every later content kind.
- Records that any machine with the same build reproduces byte for byte.
- A site that states which build it describes and never shows partially parsed data.

**Non-Goals:**

- Running the game. Map capture and checking formulas against the running game come in later changes.
- Content other than items, and change history pages. The records kept per build make history possible later.
- A shared code base with earlier compendium projects. Their lessons inform this design, their code does not carry over.

## Decisions

### Read the packages offline instead of scanning the running game

CUE4Parse reads every package except one on macOS arm64. The exception is a 2.2 GB level that overflows its loader and is not needed for items. With fatal object serialization enabled, no export failed silently. Two runs produced identical hashes.

Alternatives: a runtime scanner inside the game, as the Unity projects needed, requires the game, Wine on macOS and level traversal for data that is already readable from files. FModel exports are interactive and swallow exceptions in bulk exports.

### Extract targeted records, plus an inventory of every package

The extractor writes an inventory of all packages (path, chunk hash, parse status) and targeted records for what the catalog needs. Items and all DataTables took 22 seconds, 1.4 GB of memory and 38 MB of JSON (1.7 MB gzipped).

Alternative: a generic JSON dump of every package took 35 minutes, 10 to 18 GB of memory and 20 GB of output, and failed on 10 large levels in the serializer. It stays a private diagnostic, not a pipeline stage.

The inventory's chunk hashes tell an update which packages changed, so later stages can re-check exactly what changed.

### Merge inherited defaults explicitly and record each value's ancestor

For each class, walk the parent chain from the asset registry and overlay class default objects from the oldest ancestor to the class itself. Nested structs merge field by field. Arrays, object references and texts replace as a whole. Explicit zero, false, null and empty arrays override. Each field records the ancestor that supplied it.

Fields that no Blueprint ancestor sets have native C++ defaults, which the files do not contain. They are reported as unresolved instead of guessed. ARK's Purlovia and Soulmask's extractor hard-code such defaults, which breaks silently when the game changes.

### Treat partial parses as failures

CUE4Parse catches object deserialization errors and, for Blueprint bytecode, keeps the successfully parsed prefix. The extractor enables fatal serialization errors and checks that every function it reads parses to its end-of-script token. A failure stops extraction and names the package, export or function.

### Store each build's records in the repository

Records go to `data/builds/<build-id>/` as sorted, canonical JSON, one file per item class and per DataTable, with the build record and extractor version. One file per entity keeps Git diffs readable when an update touches a few items. Git deduplicates unchanged files across builds, so repository growth tracks what an update changes.

Alternative: a separate data repository, as ARK's Obelisk uses. One repository keeps an extractor change and the records it produces in the same commit. Revisit if the repository passes about 1 GB.

Alternative: a content-addressed evidence store outside Git. It grew to 280 GB in an earlier project before garbage collection existed, and it does not give diffs for free.

### Pin builds as copies, cloned where the file system allows

Pinning copies the game directory to a machine-local location named by build ID. Where the file system supports cheap clones (APFS, Btrfs and XFS reflinks, ReFS block cloning), the copy costs almost no space until Steam overwrites the original. Otherwise it is a full copy of about 19 GB. The Steam library and pinned-copy locations come from a machine-local configuration file that Git ignores, with an example file in the repository.

Build identity comes from Steam's app manifest (build ID, depot manifest ID) and hashes of the container files, so it does not depend on paths. The depot manifest ID lets a machine download an older build with a tool such as DepotDownloader if one is ever needed again.

### C# extractor, TypeScript catalog and site, one schema source

The extractor is C# because CUE4Parse is. Its export types are the single source of the record format. JSON Schema is generated from them with the serializer's own settings (`JsonSchemaExporter`). TypeScript types are generated from the schema, and the catalog validates records against it when loading. No hand-written duplicate schema exists.

Alternative: TypeScript-first schemas with generated C#. It puts the contract away from the code that produces the data, and C# generators lag the serializer's behaviour on nulls and enums.

### Decide publication by reachability

The catalog builds a graph of what gives items to the player: shop stock, loot tables, recipes, quest rewards, and items placed or spawned in playable levels. Playable levels are those reachable from the overworld's travel actors and doors, not every level in the container. Items with no path in the graph go to a review list instead of the site. Reviewed overrides with reasons can publish or withhold an item.

This replaces folder-based exclusion, which would drop real content kept in asset-pack folders and keep demo content stored elsewhere. Items granted only by Blueprint logic, for example a scripted reward, can be missed. The review list makes such misses visible, and later changes add bytecode references to the graph.

### Static site on Cloudflare Workers static assets

SvelteKit prerenders every page with its static adapter. Pagefind indexes the output for search without a server. Every page shows the game version and Steam build ID. Wrangler deploys the output as Workers static assets, which Cloudflare recommends for new projects. The paid plan allows 100,000 files and 25 MiB per file, and the build fails before either is exceeded.

### Icons stored in the repository by content hash

CI cannot decode textures without game files, so the extractor stores decoded icons as PNG under `data/icons/<sha256>.png`, referenced by hash from the records. Unchanged icons are stored once across builds. A spike decoded 30 of 30 sampled icons (DXT5 and DXT1) on macOS arm64. In that sample, WebP at quality 100 was larger than PNG, so the site converts PNG to its delivery format at build time.

### Tooling

- A Nix flake supplies one development shell for macOS and Linux, including a .NET SDK of at least 10.0.402, because earlier 10.0 SDKs are affected by the October 2026 SDK security advisory. Windows contributors install the SDK and Bun versions named in `global.json` and `package.json`.
- C#: CSharpier, SDK analyzers with nullable references, xUnit v3 on Microsoft.Testing.Platform.
- TypeScript and Svelte: ESLint with eslint-plugin-svelte, Prettier, svelte-check, Vitest, and Playwright for page behaviour. Faster tools such as Oxlint and Biome do not yet cover Svelte templates fully.
- GitHub Actions runs the checks on the committed records, without game files. Renovate opens grouped dependency updates. CUE4Parse updates are reviewed by hand, because a parser change can change records.

### Minimal agent instructions

`AGENTS.md` holds only boundaries an agent cannot infer from the code. Procedures become skills only once a workflow exists and has been repeated. Measured studies of repository instruction files show no reliable benefit from long, generated briefings and about 20% higher cost.

## Risks / Trade-offs

- [CUE4Parse changes its output between versions] → Pin its version. Re-extract after an update and review the record diff before accepting it.
- [Reachability misses items granted by Blueprint logic] → Unreached items appear on the review list with their references. Overrides publish them until the graph covers the case.
- [Large levels exceed CUE4Parse's loader] → Placement extraction streams actor lists and skips render payloads. The 2.2 GB Moonhorn Cliffs level loaded this way in a spike after conversion with retoc.
- [Repository growth from records and icons] → Records are per entity and deduplicated by Git. Check repository size per accepted build, and move records to a separate repository if it passes about 1 GB.
- [Contributors on Windows see different line endings or path separators] → Records use LF and forward slashes regardless of platform, and a test compares committed records against a fresh extraction.
- [Early Access updates rename or remove item classes] → Item identity is the class path. A removed class disappears from the next accepted build's site, and its records stay in the history.

## Migration Plan

New repository, nothing to migrate. The first deployment goes to a preview URL. The custom domain `bloodline.compendiums.org` is attached after review. Rollback redeploys the previous publication with Wrangler.

import { execFileSync } from "node:child_process";
import { readFile, rm, cp, mkdir } from "node:fs/promises";
import { resolve, join } from "node:path";
import { assetGate } from "./asset-gate.mjs";
import { payloadGate } from "./payload-gate.mjs";
import { assertProductionBuild } from "./provenance.mjs";
import { publicationGate } from "./publication-gate.mjs";
import { writeSiteAssets } from "./site-assets.mjs";
import { voiceGate } from "./voice-gate.mjs";
const root = resolve(import.meta.dirname, "..");
const site = join(root, "site");
const fixtures = process.argv.includes("--fixtures");
const preview = process.argv.includes("--preview");
const production = !fixtures && !preview;
const stage = join(site, ".stage/production");
const input = fixtures
  ? join(site, "fixtures/site-data")
  : process.env.BLOODLINE_SITE_DATA;
if (!input)
  throw new Error(
    "Set BLOODLINE_SITE_DATA to build game data. Use build:fixtures for invented data.",
  );
const directory = resolve(input);
const build = JSON.parse(await readFile(join(directory, "build.json"), "utf8"));
if (production) {
  assertProductionBuild(build, root);
  // A failed production rebuild must not leave an older stage available to publish.
  await rm(stage, { recursive: true, force: true });
}
const started = performance.now();

/** Indexes the built pages and adds a record for each category and type list, as Pagefind's CLI cannot. */
async function indexSearch(dataDirectory) {
  const pagefind = await import(Bun.resolveSync("pagefind", site));
  const { listPage, listRecords } = await import(
    join(site, "src/lib/search-lists.ts")
  );
  const { kindPath } = await import(join(site, "src/lib/entities.ts"));
  const read = async (name) =>
    JSON.parse(await readFile(join(dataDirectory, name), "utf8"));
  const kinds = await read("kinds.json");
  const { index, errors } = await pagefind.createIndex({});
  if (!index) throw new Error("Pagefind failed: " + errors.join(", "));
  const pages = await index.addDirectory({ path: join(site, "build") });
  if (pages.errors.length)
    throw new Error("Pagefind failed: " + pages.errors.join(", "));
  const records = listRecords(
    await read("items.json"),
    kindPath("item", kinds),
  );
  for (const record of records) {
    const added = await index.addHTMLFile({
      url: record.url,
      content: listPage(record),
    });
    if (added.errors.length)
      throw new Error("Pagefind failed: " + added.errors.join(", "));
  }
  const written = await index.writeFiles({
    outputPath: join(site, "build/pagefind"),
  });
  if (written.errors.length)
    throw new Error("Pagefind failed: " + written.errors.join(", "));
  await pagefind.close();
  return `Search: ${pages.page_count} pages and ${records.length} item lists.`;
}
const assets = await writeSiteAssets({
  site,
  dataDirectory: directory,
  fixtures,
});
console.log(assets.report);
execFileSync("bun", ["run", "vite", "build"], {
  cwd: site,
  stdio: "inherit",
  env: { ...process.env, BLOODLINE_SITE_DATA: directory },
});
// Generated images live outside static/ so that fixture builds cannot replace what a development server shows.
await cp(assets.directory, join(site, "build"), { recursive: true });
// The presentation fixture page serves layout checks and review, not readers.
if (production)
  await rm(join(site, "build/design"), { recursive: true, force: true });
console.log(await indexSearch(directory));
console.log(
  (
    await assetGate(join(site, "build"), {
      maxFiles: Number(process.env.BLOODLINE_MAX_FILES ?? 100000),
      maxBytes: Number(process.env.BLOODLINE_MAX_FILE_BYTES ?? 26214400),
    })
  ).report,
);
console.log((await payloadGate(join(site, "build"))).report);
console.log((await publicationGate(join(site, "build"))).report);
console.log((await voiceGate(join(site, "build"))).report);
if (production) {
  assertProductionBuild(build, root);
  await mkdir(join(site, ".stage"), { recursive: true });
  await cp(join(site, "build"), stage, { recursive: true });
}
console.log(
  "Build completed in " +
    ((performance.now() - started) / 1000).toFixed(2) +
    " seconds.",
);

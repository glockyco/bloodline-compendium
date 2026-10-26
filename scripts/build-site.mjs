import { execFileSync } from "node:child_process";
import { readFile, rm, cp } from "node:fs/promises";
import { resolve, join } from "node:path";
import { assetGate } from "./asset-gate.mjs";
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
execFileSync("bun", ["run", "pagefind", "--site", "build"], {
  cwd: site,
  stdio: "inherit",
});
console.log(
  (
    await assetGate(join(site, "build"), {
      maxFiles: Number(process.env.BLOODLINE_MAX_FILES ?? 100000),
      maxBytes: Number(process.env.BLOODLINE_MAX_FILE_BYTES ?? 26214400),
    })
  ).report,
);
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

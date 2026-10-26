import { execFileSync } from "node:child_process";
import { readFile, mkdir, rm, readdir, cp } from "node:fs/promises";
import { resolve, join } from "node:path";
import { createRequire } from "node:module";
import { assetGate } from "./asset-gate.mjs";
import { assertProductionBuild } from "./provenance.mjs";
const root = resolve(import.meta.dirname, "..");
const site = join(root, "site");
const require = createRequire(join(site, "package.json"));
const sharp = require("sharp");
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
const icons = join(site, "static/icons");
await rm(icons, { recursive: true, force: true });
await mkdir(icons, { recursive: true });
for (const name of await readdir(join(directory, "icons"))) {
  if (!name.endsWith(".png")) continue;
  for (const size of [48, 160])
    await sharp(join(directory, "icons", name))
      .resize(size, size, { fit: "inside", withoutEnlargement: true })
      .webp({ quality: 82 })
      .toFile(join(icons, name.slice(0, -4) + "-" + size + ".webp"));
}
const identity = join(site, "static/favicon.svg");
for (const [name, size] of [
  ["favicon-16x16.png", 16],
  ["favicon-32x32.png", 32],
  ["favicon-192x192.png", 192],
  ["favicon-512x512.png", 512],
  ["apple-touch-icon.png", 180],
]) {
  await sharp(identity)
    .resize(size, size)
    .png()
    .toFile(join(site, "static", name));
}
await sharp({
  create: { width: 1200, height: 630, channels: 4, background: "#142c2b" },
})
  .composite([{ input: await sharp(identity).resize(420, 420).toBuffer() }])
  .png()
  .toFile(join(site, "static/og-default.png"));
execFileSync("bun", ["run", "vite", "build"], {
  cwd: site,
  stdio: "inherit",
  env: { ...process.env, BLOODLINE_SITE_DATA: directory },
});
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

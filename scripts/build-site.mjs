import { execFileSync } from "node:child_process";
import { readFile, mkdir, rm, readdir, cp } from "node:fs/promises";
import { resolve, join } from "node:path";
import { createRequire } from "node:module";
import { assetGate } from "./asset-gate.mjs";
import { assertProductionBuild } from "./provenance.mjs";
import { publicationGate } from "./publication-gate.mjs";
import { storeHeroArt } from "./store-art.mjs";
import { voiceGate } from "./voice-gate.mjs";
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
const art = join(site, "static/art");
await rm(art, { recursive: true, force: true });
await mkdir(art, { recursive: true });
// Fixture builds use invented art, so checks never need the network or the publisher's art.
const hero = fixtures
  ? join(site, "fixtures/store-art/hero.jpg")
  : await storeHeroArt(join(site, ".cache/store-art"));
for (const width of [1280, 1920, 2560, 3840])
  await sharp(hero)
    .resize({ width })
    .webp({ quality: 80 })
    .toFile(join(art, `hero-${width}.webp`));
// One emblem serves the header, browser icons and link previews, so readers meet one mark everywhere.
const emblem = join(site, "brand/emblem.png");
const brand = join(site, "static/brand");
await rm(brand, { recursive: true, force: true });
await mkdir(brand, { recursive: true });
for (const size of [48, 96])
  await sharp(emblem)
    .resize(size, size)
    .webp({ quality: 90 })
    .toFile(join(brand, `emblem-${size}.webp`));
for (const [name, size] of [
  ["favicon-16x16.png", 16],
  ["favicon-32x32.png", 32],
  ["favicon-192x192.png", 192],
  ["favicon-512x512.png", 512],
]) {
  await sharp(emblem)
    .resize(size, size)
    .png()
    .toFile(join(site, "static", name));
}
// Home screens draw no background of their own, so the touch icon carries the site's.
const night = "#15121d";
await sharp({
  create: { width: 180, height: 180, channels: 4, background: night },
})
  .composite([{ input: await sharp(emblem).resize(148, 148).toBuffer() }])
  .png()
  .toFile(join(site, "static/apple-touch-icon.png"));
await sharp({
  create: { width: 1200, height: 630, channels: 4, background: night },
})
  .composite([{ input: await sharp(emblem).resize(460, 460).toBuffer() }])
  .png()
  .toFile(join(site, "static/og-default.png"));
execFileSync("bun", ["run", "vite", "build"], {
  cwd: site,
  stdio: "inherit",
  env: { ...process.env, BLOODLINE_SITE_DATA: directory },
});
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

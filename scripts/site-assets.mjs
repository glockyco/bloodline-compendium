import { createRequire } from "node:module";
import { mkdir, readdir, rename, rm, stat } from "node:fs/promises";
import { dirname, join, relative } from "node:path";
import { assetDirectory } from "./asset-directory.mjs";
import { storeHeroArt } from "./store-art.mjs";

/**
 * Writes the generated images for one data source. Every file is written beside its target and renamed into
 * place, and content-named icons that already exist are kept, so a server reading the directory never sees a
 * missing image during a rebuild. Files the source no longer produces are removed at the end.
 */
export async function writeSiteAssets({ site, dataDirectory, fixtures }) {
  const require = createRequire(join(site, "package.json"));
  const sharp = require("sharp");
  const target = assetDirectory(site, dataDirectory);
  const written = new Set();
  async function write(file, image, keepExisting = false) {
    const path = join(target, file);
    written.add(path);
    if (keepExisting && (await stat(path).catch(() => null))) return;
    await mkdir(dirname(path), { recursive: true });
    const partial = path + ".partial";
    await image.toFile(partial);
    await rename(partial, path);
  }

  for (const name of await readdir(join(dataDirectory, "icons"))) {
    if (!name.endsWith(".png")) continue;
    for (const size of [48, 160])
      await write(
        `icons/${name.slice(0, -4)}-${size}.webp`,
        sharp(join(dataDirectory, "icons", name))
          .resize(size, size, { fit: "inside", withoutEnlargement: true })
          .webp({ quality: 82 }),
        true,
      );
  }

  // Fixture builds use invented art, so checks never need the network or the publisher's art.
  const hero = fixtures
    ? join(site, "fixtures/store-art/hero.jpg")
    : await storeHeroArt(join(site, ".cache/store-art"));
  for (const width of [1280, 1920, 2560, 3840])
    await write(
      `art/hero-${width}.webp`,
      sharp(hero).resize({ width }).webp({ quality: 80 }),
    );

  // One emblem serves the header, browser icons and link previews, so readers meet one mark everywhere.
  const emblem = join(site, "brand/emblem.png");
  for (const size of [48, 96])
    await write(
      `brand/emblem-${size}.webp`,
      sharp(emblem).resize(size, size).webp({ quality: 90 }),
    );
  for (const [name, size] of [
    ["favicon-16x16.png", 16],
    ["favicon-32x32.png", 32],
    ["favicon-192x192.png", 192],
    ["favicon-512x512.png", 512],
  ])
    await write(name, sharp(emblem).resize(size, size).png());
  // Home screens draw no background of their own, so the touch icon carries the site's.
  const night = "#15121d";
  await write(
    "apple-touch-icon.png",
    sharp({
      create: { width: 180, height: 180, channels: 4, background: night },
    })
      .composite([{ input: await sharp(emblem).resize(148, 148).toBuffer() }])
      .png(),
  );
  await write(
    "og-default.png",
    sharp({
      create: { width: 1200, height: 630, channels: 4, background: night },
    })
      .composite([{ input: await sharp(emblem).resize(460, 460).toBuffer() }])
      .png(),
  );

  async function prune(directory) {
    for (const entry of await readdir(directory, { withFileTypes: true })) {
      const path = join(directory, entry.name);
      if (entry.isDirectory()) await prune(path);
      else if (!written.has(path)) await rm(path, { force: true });
    }
  }
  await prune(target);
  return {
    directory: target,
    files: written.size,
    report: `Assets: ${written.size} images in ${relative(site, target)}.`,
  };
}

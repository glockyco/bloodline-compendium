// Checks every kept model render of a build and publishes those that pass for the catalog.
// Usage: bun tools/runtime/publish-renders.mjs <build-id>
// The latest render of each item and body counts. A render passes when it has an alpha channel, is at least 64
// pixels on its shorter side, draws the item on at least 3% of its area and leaves its outer pixel rows and columns
// transparent, so nothing is cut off. Passing renders are scaled to at most 640 pixels on their longer side and
// written to artifacts/<build>/renders/<sha256>.webp, with index.json mapping each item's class path to its renders by
// body, each with its hash, width and height. check.json lists every render that failed with its reason, and every
// item without a render with the error its capture reported. The command exits 1 when a render failed the check.
import { createHash } from "node:crypto";
import { mkdir, readFile, rm, writeFile } from "node:fs/promises";
import { createRequire } from "node:module";
import { join, resolve } from "node:path";
import { keptRenders } from "./renders.mjs";

const build = process.argv[2];
if (!build) throw new Error("Name the build: publish-renders.mjs <build-id>");
const root = resolve(import.meta.dirname, "../..");
// The site's own dependencies hold the image library, as for lab.mjs.
const sharp = createRequire(join(root, "site/package.json"))("sharp");
const runtime = join(root, "artifacts", build, "runtime");
const target = join(root, "artifacts", build, "renders");

/** Why a render fails the check, or null when it passes. */
async function problem(file) {
  const image = sharp(file);
  const { width, height, channels, hasAlpha } = await image.metadata();
  if (!hasAlpha || channels !== 4) return "It has no transparent background.";
  if (Math.min(width, height) < 64)
    return `It is ${width} by ${height} pixels, smaller than 64.`;
  const pixels = await image.raw().toBuffer();
  let drawn = 0;
  let edge = 0;
  for (let y = 0; y < height; y++)
    for (let x = 0; x < width; x++) {
      const alpha = pixels[(y * width + x) * 4 + 3];
      if (alpha > 0) drawn++;
      if (
        alpha > 0 &&
        (x === 0 || y === 0 || x === width - 1 || y === height - 1)
      )
        edge++;
    }
  if (edge)
    return `${edge} pixels on its edge are drawn, so the item may be cut off.`;
  if (drawn < 0.03 * width * height)
    return `The item covers ${((100 * drawn) / (width * height)).toFixed(1)}% of it, less than 3%.`;
  return null;
}

const latest = new Map();
for (const render of await keptRenders(runtime))
  latest.set(`${render.item}\n${render.body}`, render);

await rm(target, { recursive: true, force: true });
await mkdir(target, { recursive: true });
const index = {};
const failed = [];
for (const render of latest.values()) {
  const reason = await problem(render.file).catch((error) =>
    String(error.message),
  );
  if (reason) {
    failed.push({
      item: render.item,
      body: render.body,
      file: render.file,
      problem: reason,
    });
    continue;
  }
  const { data: webp, info } = await sharp(render.file)
    .resize(640, 640, { fit: "inside", withoutEnlargement: true })
    .webp({ quality: 85, alphaQuality: 100 })
    .toBuffer({ resolveWithObject: true });
  const hash = createHash("sha256").update(webp).digest("hex");
  await writeFile(join(target, `${hash}.webp`), webp);
  (index[render.item] ??= {})[render.body] = {
    hash,
    width: info.width,
    height: info.height,
  };
}

const failures = JSON.parse(
  await readFile(join(runtime, "render-failures.json"), "utf8").catch(
    () => "[]",
  ),
);
const unrendered = failures.filter(
  (failure) => !index[failure.item]?.[failure.body],
);
await writeFile(
  join(target, "index.json"),
  JSON.stringify(index, null, 2) + "\n",
);
await writeFile(
  join(target, "check.json"),
  JSON.stringify({ failed, unrendered }, null, 2) + "\n",
);
console.log(
  `${Object.keys(index).length} items have renders. ${failed.length} renders failed the check, ${unrendered.length} captures failed.`,
);
if (failed.length) process.exitCode = 1;

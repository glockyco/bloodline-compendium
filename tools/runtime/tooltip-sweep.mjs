// Reads the tooltip of every published item in the lab game and, with --capture, takes a screenshot of every batch
// of eight tooltips as the game draws them.
// Usage: bun tools/runtime/tooltip-sweep.mjs <build-id> [--capture]. Items that earlier sweeps of the build already
// read, or with --capture already captured, are skipped, so the sweep can resume. Only shown-only readings count.
// A batch that stops the game from answering is retried in halves on a restarted game, and an item that stops it
// alone is written to sweep-failures.json in the runtime directory.
import { execFileSync } from "node:child_process";
import { readFile, readdir, writeFile } from "node:fs/promises";
import { join, resolve } from "node:path";

const build = process.argv[2];
if (!build)
  throw new Error("Name the build: tooltip-sweep.mjs <build-id> [--capture]");
const capture = process.argv.includes("--capture");
const root = resolve(import.meta.dirname, "../..");
const runtime = join(root, "artifacts", build, "runtime");
const site = join(root, "artifacts", build, "site");
const lab = (...args) =>
  execFileSync("bun", [join(import.meta.dirname, "lab.mjs"), ...args], {
    encoding: "utf8",
    stdio: ["ignore", "pipe", "pipe"],
  });

async function readItems() {
  const ids = [];
  for (const file of (await readdir(join(site, "items"))).sort())
    ids.push(JSON.parse(await readFile(join(site, "items", file), "utf8")).key);
  return ids;
}

/** The bridge encodes an empty Lua table as an object, so lists may arrive as {}. */
const list = (value) => (Array.isArray(value) ? value : []);

/**
 * Items that shown-only tooltip batches of this build already read or rejected, from every run's record. With
 * --capture an item counts only once a screenshot followed its placement.
 */
async function doneItems() {
  const done = new Set();
  for (const run of await readdir(runtime, { withFileTypes: true })) {
    if (!run.isDirectory()) continue;
    const log = await readFile(
      join(runtime, run.name, "run.jsonl"),
      "utf8",
    ).catch(() => "");
    let placed = [];
    for (const line of log.split("\n").filter(Boolean)) {
      const entry = JSON.parse(line);
      if (!entry.ok) continue;
      if (entry.command === "tooltip-batch" && entry.result.shownOnly) {
        for (const row of list(entry.result.rejected)) done.add(row.item);
        if (!capture)
          for (const row of list(entry.result.tooltips)) done.add(row.item);
      }
      if (entry.command === "place-tooltips")
        placed = list(entry.result.placed);
      if (entry.command === "capture-screen" && placed.length) {
        for (const item of placed) if (capture) done.add(item);
        placed = [];
      }
    }
  }
  return done;
}

function restart() {
  try {
    lab("quit");
  } catch {
    // The game may no longer answer, and quit then stops its process.
  }
  lab("launch", capture ? "render" : "headless", "tooltips-sweep");
  lab("new-save", `Tips${Date.now() % 1e9}`);
  lab("close-tutorials");
  lab("open-sheet");
}

const failuresFile = join(runtime, "sweep-failures.json");
const failures = JSON.parse(
  await readFile(failuresFile, "utf8").catch(() => "[]"),
);
let shot = 0;

/** Reads a batch's tooltips and, with --capture, places them and takes a screenshot before emptying the inventory. */
function batch(items) {
  const result = JSON.parse(
    lab("tooltip-batch", JSON.stringify({ items, frames: 10, keep: capture })),
  );
  if (capture) {
    lab("place-tooltips");
    lab("capture-screen", `tooltips-${String(shot++).padStart(4, "0")}`);
    const cleared = JSON.parse(lab("clear-tooltips"));
    if (cleared.left)
      throw new Error(`${cleared.left} items stayed in the inventory.`);
  } else if (result.left)
    throw new Error(`${result.left} items stayed in the inventory.`);
  return list(result.tooltips).length;
}

async function sweep(items) {
  try {
    return batch(items);
  } catch (error) {
    console.log(
      `A batch of ${items.length} failed: ${String(error.message).split("\n")[0]}`,
    );
    restart();
    if (items.length === 1) {
      failures.push({
        item: items[0],
        error: String(error.message).split("\n")[0],
      });
      await writeFile(failuresFile, JSON.stringify(failures, null, 2) + "\n");
      return 0;
    }
    const half = Math.ceil(items.length / 2);
    return (
      (await sweep(items.slice(0, half))) + (await sweep(items.slice(half)))
    );
  }
}

const done = await doneItems();
const failed = new Set(failures.map((failure) => failure.item));
const remaining = (await readItems()).filter(
  (id) => !done.has(id) && !failed.has(id),
);
console.log(
  `${done.size} items already done, ${failed.size} failed before, ${remaining.length} to do.`,
);
lab("open-sheet");
// A screenshot shows eight tooltips without overlap. Without screenshots, the inventory's 100 items bound a batch.
const size = capture ? 8 : 80;
let read = 0;
for (let start = 0; start < remaining.length; start += size) {
  read += await sweep(remaining.slice(start, start + size));
  console.log(
    `${Math.min(start + size, remaining.length)} of ${remaining.length} done.`,
  );
}
console.log(
  `Read ${read} tooltips. ${failures.length} items stopped the game.`,
);

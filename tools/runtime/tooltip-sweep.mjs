// Reads the tooltip of every published item in the lab game (task 5.4 of establish-reference-architecture).
// Usage: bun tools/runtime/tooltip-sweep.mjs <build-id>. Items that earlier sweeps of the build already read are
// skipped, so the sweep can resume. A batch that stops the game from answering is retried in halves on a restarted
// game, and an item that stops it alone is written to sweep-failures.json in the runtime directory.
import { execFileSync } from "node:child_process";
import { readFile, readdir, writeFile } from "node:fs/promises";
import { join, resolve } from "node:path";

const build = process.argv[2];
if (!build) throw new Error("Name the build: tooltip-sweep.mjs <build-id>");
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
    ids.push(JSON.parse(await readFile(join(site, "items", file), "utf8")).id);
  return ids;
}

/** The bridge encodes an empty Lua table as an object, so lists may arrive as {}. */
const list = (value) => (Array.isArray(value) ? value : []);

/** Items that tooltip batches of this build already read or rejected, from every run's record. */
async function doneItems() {
  const done = new Set();
  for (const run of await readdir(runtime, { withFileTypes: true })) {
    if (!run.isDirectory()) continue;
    const log = await readFile(
      join(runtime, run.name, "run.jsonl"),
      "utf8",
    ).catch(() => "");
    for (const line of log.split("\n").filter(Boolean)) {
      const entry = JSON.parse(line);
      if (entry.command !== "tooltip-batch" || !entry.ok) continue;
      for (const row of list(entry.result.tooltips)) done.add(row.item);
      for (const row of list(entry.result.rejected)) done.add(row.item);
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
  lab("launch", "headless", "tooltips-sweep");
  lab("new-save", `LabTooltips${Date.now()}`);
  lab("close-tutorials");
  lab("open-sheet");
}

const failuresFile = join(runtime, "sweep-failures.json");
const failures = JSON.parse(
  await readFile(failuresFile, "utf8").catch(() => "[]"),
);

async function sweep(items) {
  try {
    const result = JSON.parse(
      lab("tooltip-batch", JSON.stringify({ items, frames: 10 })),
    );
    if (result.left)
      throw new Error(`${result.left} items stayed in the inventory.`);
    return list(result.tooltips).length;
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
  `${done.size} items already read, ${failed.size} failed before, ${remaining.length} to read.`,
);
lab("open-sheet");
// The inventory holds 100 items, and batches stay below that.
const size = 80;
let read = 0;
for (let start = 0; start < remaining.length; start += size) {
  read += await sweep(remaining.slice(start, start + size));
  console.log(
    `${Math.min(start + size, remaining.length)} of ${remaining.length} read.`,
  );
}
console.log(
  `Read ${read} tooltips. ${failures.length} items stopped the game.`,
);

// Reads every harvestable in play in each level that places harvestables or harvestable spawners, so the catalog can
// compare them with what it says each level gives.
// Usage: bun tools/runtime/harvest-sweep.mjs <build-id>, in a lab game that a run has begun and a new save has
// started. For each level the sweep reads the saved harvestables that loading spawns again, opens the level, waits
// for its spawners and reads harvest-census.lua. Levels that earlier sweeps of the build already read are skipped, so
// the sweep can resume.
import { execFileSync } from "node:child_process";
import { readFile, readdir } from "node:fs/promises";
import { join, resolve } from "node:path";

const build = process.argv[2];
if (!build) throw new Error("Name the build: harvest-sweep.mjs <build-id>");
const root = resolve(import.meta.dirname, "../..");
const runtime = join(root, "artifacts", build, "runtime");
const levels = join(root, "artifacts", build, "records", "levels");
const census = join(import.meta.dirname, "checks", "harvest-census.lua");
const opener = join(import.meta.dirname, "checks", "placed-pickups.lua");
const lab = (...args) =>
  execFileSync("bun", [join(import.meta.dirname, "lab.mjs"), ...args], {
    encoding: "utf8",
    stdio: ["ignore", "pipe", "pipe"],
  });
// Spawners finish their Begin Play spawning within this time of a level opening in the lab.
const settleSeconds = 70;
const harvestClasses = new Set([
  "HarvestableInteractionObject_C",
  "Harvestable_Item_Spawner_Box_C",
  "OpenWorld_Populator_C",
  "RepeatableDungeonPopulator_C",
]);

async function* files(directory) {
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) yield* files(path);
    else if (entry.name.endsWith(".json")) yield path;
  }
}

/** Level packages that place a harvestable or a spawner of harvestables. */
async function harvestLevels() {
  const found = [];
  for await (const file of files(levels)) {
    const level = JSON.parse(await readFile(file, "utf8"));
    if (
      level.actors.some((actor) =>
        harvestClasses.has(actor.class.split(".").pop()),
      )
    )
      found.push(level.path);
  }
  return found.sort();
}

/** Level names that a census of this build already read. */
async function doneLevels() {
  const done = new Set();
  for (const run of await readdir(runtime, { withFileTypes: true }).catch(
    () => [],
  )) {
    if (!run.isDirectory()) continue;
    const log = await readFile(
      join(runtime, run.name, "run.jsonl"),
      "utf8",
    ).catch(() => "");
    for (const line of log.split("\n").filter(Boolean)) {
      const entry = JSON.parse(line);
      if (
        entry.ok &&
        entry.command === "eval" &&
        entry.result?.level &&
        "harvestables" in entry.result
      )
        done.add(entry.result.level);
    }
  }
  return done;
}

const done = await doneLevels();
for (const path of await harvestLevels()) {
  const name = path.split("/").pop();
  if (done.has(name)) continue;
  // Loading spawns the saved harvestables again at their saved spots and then saves anew, so the spots come first.
  const { saved } = JSON.parse(
    lab("eval", census, JSON.stringify({ saved: true })),
  );
  lab("eval", opener, JSON.stringify({ open: path }));
  await new Promise((resolve) => setTimeout(resolve, settleSeconds * 1000));
  const result = JSON.parse(
    lab(
      "eval",
      census,
      JSON.stringify({ restoring: Array.isArray(saved) ? saved : [] }),
    ),
  );
  if (result.level !== name)
    throw new Error(`Opened ${path} but the game reports ${result.level}.`);
  const count = Array.isArray(result.harvestables)
    ? result.harvestables.length
    : 0;
  console.log(`${name}: ${count} harvestables`);
}

// Renders the model of every published item in the lab game, for publish-renders.mjs to check and publish.
// Usage: bun tools/runtime/render-sweep.mjs <build-id> [female] [--items <class path>,...] [--again]
// Each render is kept in the run under its render name from renders.mjs. The female body renders only armor whose
// male render showed body meshes, because only those have female counterparts. Items that earlier sweeps of the
// build already rendered on the same body, or that failed, are skipped, so the sweep can resume. --again renders
// them anyway, such as after a view changed, and the latest render of an item counts. An item that fails is written
// to render-failures.json in the runtime directory with its error. When the game stopped answering or the character
// kept a piece on, the game restarts and the item is tried once more.
import { execFileSync } from "node:child_process";
import { readFile, readdir, writeFile } from "node:fs/promises";
import { join, resolve } from "node:path";
import { keptRenders, renderName } from "./renders.mjs";

const build = process.argv[2];
if (!build)
  throw new Error(
    "Name the build: render-sweep.mjs <build-id> [female] [--items <class path>,...] [--again]",
  );
const body = process.argv[3] === "female" ? "female" : "male";
const only = process.argv.includes("--items")
  ? new Set(process.argv[process.argv.indexOf("--items") + 1].split(","))
  : null;
const again = process.argv.includes("--again");
const root = resolve(import.meta.dirname, "../..");
const runtime = join(root, "artifacts", build, "runtime");
const site = join(root, "artifacts", build, "site");
const records = join(root, "artifacts", build, "records");
const lab = (...args) =>
  execFileSync("bun", [join(import.meta.dirname, "lab.mjs"), ...args], {
    encoding: "utf8",
    stdio: ["ignore", "pipe", "pipe"],
  });

/** Every published item with whether it is armor, including the versions that share a page. */
async function readItems() {
  const items = new Map();
  for (const file of (await readdir(join(site, "items"))).sort()) {
    const page = JSON.parse(await readFile(join(site, "items", file), "utf8"));
    for (const item of [page, ...(page.versions ?? [])])
      items.set(item.key, item.category.slug === "armor");
  }
  return items;
}
const armor = await readItems();

/** The mesh, materials and scale of each weapon part, from its row in WeaponComponentTable. */
async function readParts() {
  const table = JSON.parse(
    await readFile(
      join(records, "tables/Game/BloodlinesAlpha/WeaponComponentTable.json"),
      "utf8",
    ),
  );
  const parts = new Map();
  for (const { values } of table.rows) {
    if (!values.Class?.$ref || !values.ComponentMesh?.$ref) continue;
    parts.set(values.Class.$ref, {
      mesh: values.ComponentMesh.$ref,
      materials: (values.ComponentMaterial ?? []).map((entry) => entry.$ref),
      scale: values.Scale,
    });
  }
  return parts;
}

function restart() {
  try {
    lab("quit");
  } catch {
    // The game may no longer answer, and quit then stops its process.
  }
  lab("launch", "render", `renders-${body}`);
  lab(
    "new-save",
    `R${body[0]}${Date.now() % 1e9}`,
    ...(body === "female" ? ["female"] : []),
  );
  lab("close-tutorials");
}

const failuresFile = join(runtime, "render-failures.json");
const failures = JSON.parse(
  await readFile(failuresFile, "utf8").catch(() => "[]"),
);
const parts = await readParts();
const kept = await keptRenders(runtime);
const done = new Set(
  kept.filter((render) => render.body === body).map((render) => render.item),
);
const failed = new Set(
  failures
    .filter((failure) => failure.body === body)
    .map((failure) => failure.item),
);
// Body meshes are named SK_Chr_<part>_Male_<n>, and each has a female counterpart.
const bodyMeshes = new Set(
  kept
    .filter(
      (render) =>
        render.body === "male" &&
        render.meshes.some((mesh) => /SK_Chr_\w+_Male_/.test(mesh)),
    )
    .map((render) => render.item),
);
const remaining = [...armor.keys()].filter(
  (key) =>
    (again || (!done.has(key) && !failed.has(key))) &&
    (!only || only.has(key)) &&
    (body === "male" || bodyMeshes.has(key)),
);
console.log(
  `${done.size} items already rendered on the ${body} body, ${failed.size} failed before, ${remaining.length} to do.`,
);
if (remaining.length) restart();
/** Captures an item's render, and returns the line of the error that tells why it failed or null when it is kept. */
function capture(key) {
  const args = {
    item: key,
    name: renderName(key, body),
    worn: armor.get(key),
  };
  if (parts.has(key)) args.part = parts.get(key);
  try {
    lab("capture-model", JSON.stringify(args));
    return null;
  } catch (error) {
    const lines = String(error.stderr || error.message).split("\n");
    return (
      lines.find((line) =>
        /error|failed|did not|draws|shows nothing|touches/i.test(line),
      ) ?? lines[0]
    ).trim();
  }
}

// An error about the item's own model leaves the game as it was. A game that stopped answering, or a character that
// kept a piece on or never showed the item, would fail every capture after it too.
const stuck =
  /Timed out|not running|No session|Still equipped|not equipped|No inventory slot/i;
let count = 0;
for (const key of remaining) {
  let message = capture(key);
  if (message && stuck.test(message)) {
    console.log(`${key} failed: ${message} Trying once more on a new game.`);
    restart();
    message = capture(key);
  }
  if (message) {
    console.log(`${key} failed: ${message}`);
    failures.push({ item: key, body, error: message });
    await writeFile(failuresFile, JSON.stringify(failures, null, 2) + "\n");
    if (stuck.test(message)) restart();
    continue;
  }
  if (++count % 50 === 0)
    console.log(`${count} of ${remaining.length} rendered.`);
}
console.log(
  `Rendered ${count} items on the ${body} body. ${failures.length} renders failed in this build.`,
);

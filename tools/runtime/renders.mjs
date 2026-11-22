// What render-sweep.mjs and publish-renders.mjs share: the published items, how a render is named and how runs
// record renders.
import { readFile, readdir } from "node:fs/promises";
import { basename, join } from "node:path";

/** The file name a render of an item keeps in its run, without the extension. */
export const renderName = (key, body) =>
  key.split(".").at(-1) + (body === "female" ? "-female" : "");

/** Every published item's name and category by class path, from the site data, with the versions that share a page. */
export async function publishedItems(site) {
  const items = new Map();
  for (const file of (await readdir(join(site, "items"))).sort()) {
    const page = JSON.parse(await readFile(join(site, "items", file), "utf8"));
    for (const item of [page, ...(page.versions ?? [])])
      items.set(item.key, { name: item.name, category: item.category.slug });
  }
  return items;
}

/** The bridge encodes an empty Lua table as an object, so lists may arrive as {}. */
const list = (value) => (Array.isArray(value) ? value : []);

/**
 * Every render that runs of the build kept under its item's render name, in run order: the item, the body the
 * preview showed, the subject kind, the meshes it showed and the file. A capture counts only with the keep that
 * follows it, and only when the game reported the body, so exploratory captures under other names are left out.
 */
export async function keptRenders(runtime) {
  const found = [];
  const runs = (await readdir(runtime, { withFileTypes: true }))
    .filter((run) => run.isDirectory())
    .map((run) => run.name)
    .sort();
  for (const run of runs) {
    const log = await readFile(join(runtime, run, "run.jsonl"), "utf8").catch(
      () => "",
    );
    let capture = null;
    for (const line of log.split("\n").filter(Boolean)) {
      const entry = JSON.parse(line);
      // Keep records carry no ok field, so only a failed command is skipped.
      if (entry.ok === false) continue;
      if (entry.command === "capture-model") capture = entry.result;
      else if (entry.command === "keep" && capture?.body) {
        const file = join(
          runtime,
          "..",
          "..",
          "..",
          entry.file ?? entry.result?.file ?? "",
        );
        if (basename(file, ".png") === renderName(capture.item, capture.body))
          found.push({
            item: capture.item,
            body: capture.body,
            kind: capture.kind,
            meshes: list(capture.meshes),
            file,
          });
        capture = null;
      }
    }
  }
  return found;
}

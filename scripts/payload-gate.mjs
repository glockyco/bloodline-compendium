import { readdir, readFile } from "node:fs/promises";
import { join, relative } from "node:path";
import { fileURLToPath } from "node:url";
import { gzipSync } from "node:zlib";

// What a reader downloads and parses for one page: its document, and the data a client navigation fetches instead.
// Raw bytes bound parsing on a phone, and gzip bytes bound the transfer. The item list carries every item's
// summary, so it has its own, larger budget.
const KiB = 1024;
const budgets = {
  page: { raw: 512 * KiB, gzip: 96 * KiB },
  pageData: { raw: 256 * KiB, gzip: 64 * KiB },
  list: { raw: 2048 * KiB, gzip: 256 * KiB },
  listData: { raw: 2048 * KiB, gzip: 384 * KiB },
};

function budgetOf(name) {
  const list = name === "items/index.html" || name === "items/__data.json";
  const data = name.endsWith("__data.json");
  return list
    ? [data ? "listData" : "list", budgets[data ? "listData" : "list"]]
    : [data ? "pageData" : "page", budgets[data ? "pageData" : "page"]];
}

export async function payloadGate(directory) {
  const largest = {};
  const over = [];
  async function walk(dir) {
    for (const entry of await readdir(dir, { withFileTypes: true })) {
      const path = join(dir, entry.name);
      if (entry.isDirectory()) await walk(path);
      else if (entry.name === "index.html" || entry.name === "__data.json") {
        const name = relative(directory, path).replaceAll("\\", "/");
        const bytes = await readFile(path);
        const size = { raw: bytes.length, gzip: gzipSync(bytes).length };
        const [kind, budget] = budgetOf(name);
        if (!largest[kind] || size.raw > largest[kind].raw)
          largest[kind] = { name, ...size };
        if (size.raw > budget.raw || size.gzip > budget.gzip)
          over.push(
            `${name}: ${size.raw} bytes, ${size.gzip} gzip, over ${budget.raw} and ${budget.gzip}`,
          );
      }
    }
  }
  await walk(directory);
  const report =
    "Payload, largest of each kind against its budget:\n" +
    Object.entries(largest)
      .map(
        ([kind, size]) =>
          `${kind}: ${size.name} ${size.raw} of ${budgets[kind].raw} bytes, ${size.gzip} of ${budgets[kind].gzip} gzip`,
      )
      .join("\n");
  if (over.length)
    throw new Error(report + "\nPayload budgets exceeded:\n" + over.join("\n"));
  return { largest, report };
}

if (process.argv[1] === fileURLToPath(import.meta.url))
  console.log((await payloadGate(process.argv[2] ?? "site/build")).report);

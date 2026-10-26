import { readdir, stat } from "node:fs/promises";
import { join, relative } from "node:path";
import { fileURLToPath } from "node:url";
export async function assetGate(
  directory,
  { maxFiles = 100000, maxBytes = 25 * 1024 * 1024 } = {},
) {
  if (
    !Number.isSafeInteger(maxFiles) ||
    maxFiles < 1 ||
    !Number.isSafeInteger(maxBytes) ||
    maxBytes < 1
  )
    throw new Error("Asset limits must be positive safe integers.");
  const counts = {};
  const oversized = [];
  let files = 0;
  let bytes = 0;
  async function walk(dir) {
    for (const entry of await readdir(dir, { withFileTypes: true })) {
      const path = join(dir, entry.name);
      if (entry.isDirectory()) await walk(path);
      else if (entry.isFile()) {
        const size = (await stat(path)).size;
        const name = relative(directory, path).replaceAll("\\", "/");
        const top = name.includes("/") ? name.split("/")[0] : "(root)";
        counts[top] = (counts[top] ?? 0) + 1;
        files++;
        bytes += size;
        if (size > maxBytes) oversized.push(name + ": " + size + " bytes");
      }
    }
  }
  await walk(directory);
  const report =
    files +
    " files, " +
    bytes +
    " bytes\n" +
    Object.entries(counts)
      .sort()
      .map(([dir, count]) => dir + ": " + count)
      .join("\n");
  if (files > maxFiles || oversized.length)
    throw new Error(
      report +
        "\nAsset limits exceeded (max " +
        maxFiles +
        " files, " +
        maxBytes +
        " bytes per file).\n" +
        oversized.join("\n"),
    );
  return { files, bytes, counts, report };
}
if (process.argv[1] === fileURLToPath(import.meta.url))
  console.log(
    (
      await assetGate(process.argv[2] ?? "site/build", {
        maxFiles: Number(process.env.BLOODLINE_MAX_FILES ?? 100000),
        maxBytes: Number(process.env.BLOODLINE_MAX_FILE_BYTES ?? 26214400),
      })
    ).report,
  );

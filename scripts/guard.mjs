import { execFileSync } from "node:child_process";
import { fileURLToPath } from "node:url";
export function trackedGameFiles(repo) {
  return execFileSync("git", ["ls-files", "-z"], {
    cwd: repo,
    encoding: "utf8",
  })
    .split("\0")
    .filter(
      (p) =>
        /(^|\/)artifacts\//i.test(p) ||
        /(^|\/)bloodline\.local\.json$/i.test(p) ||
        /\.(pak|utoc|ucas|uasset|umap|uexp|ubulk|usmap|sav)$/i.test(p),
    );
}
if (process.argv[1] === fileURLToPath(import.meta.url)) {
  const files = trackedGameFiles(process.argv[2] ?? process.cwd());
  if (files.length) {
    console.error(
      "Tracked game files or extracted output:\n" + files.join("\n"),
    );
    process.exitCode = 1;
  } else console.log("No tracked game files or extracted output.");
}

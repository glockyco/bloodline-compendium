import { execFileSync } from "node:child_process";
export function assertProductionBuild(build, repo) {
  const changes = execFileSync(
    "git",
    ["status", "--porcelain", "--untracked-files=all"],
    { cwd: repo, encoding: "utf8" },
  ).trim();
  if (changes)
    throw new Error("Production requires a clean working tree:\n" + changes);
  const commit = execFileSync("git", ["rev-parse", "HEAD"], {
    cwd: repo,
    encoding: "utf8",
  }).trim();
  if (build.dirty !== false)
    throw new Error(
      "Site data was generated from a dirty working tree. Regenerate it from HEAD.",
    );
  if (build.commit !== commit)
    throw new Error(
      "Site data commit " +
        build.commit +
        " differs from HEAD " +
        commit +
        ". Regenerate the site data.",
    );
}

import { execFileSync } from "node:child_process";
import { mkdtemp, readFile, rm, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join, resolve } from "node:path";
import { createRequire } from "node:module";
const root = resolve(import.meta.dirname, "..");
const require = createRequire(join(root, "site/package.json"));
const { compile } = require("json-schema-to-typescript");
const directory = await mkdtemp(join(tmpdir(), "bloodline-schema-"));
try {
  const schemaPath = join(directory, "schema.json");
  const registryPath = join(directory, "registry.json");
  execFileSync(
    "dotnet",
    [
      ...["run", "--project", "src/Bloodline", "--", "schema"],
      ...["--out", schemaPath, "--registry", registryPath],
    ],
    { cwd: root, stdio: "inherit" },
  );
  const schemaText = await readFile(schemaPath, "utf8");
  const types = await compile(JSON.parse(schemaText), "SiteDataFiles", {
    unreachableDefinitions: true,
  });
  for (const [target, content] of [
    ["site/src/lib/site-data.schema.json", schemaText],
    ["site/src/lib/site-data.ts", types],
    ["issues/known-issues.schema.json", await readFile(registryPath, "utf8")],
  ]) {
    const path = join(root, target);
    if (process.argv.includes("--check")) {
      if ((await readFile(path, "utf8").catch(() => "")) !== content)
        throw new Error(path + " is stale. Run bun run types.");
    } else await writeFile(path, content);
  }
} finally {
  await rm(directory, { recursive: true, force: true });
}

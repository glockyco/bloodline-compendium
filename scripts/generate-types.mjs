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
  execFileSync(
    "dotnet",
    ["run", "--project", "src/Bloodline", "--", "schema", "--out", schemaPath],
    { cwd: root, stdio: "inherit" },
  );
  const schemaText = await readFile(schemaPath, "utf8");
  const types = await compile(JSON.parse(schemaText), "SiteDataFiles", {
    unreachableDefinitions: true,
  });
  for (const [path, content] of [
    ["site-data.schema.json", schemaText],
    ["site-data.ts", types],
  ]) {
    const target = join(root, "site/src/lib", path);
    if (process.argv.includes("--check")) {
      if ((await readFile(target, "utf8").catch(() => "")) !== content)
        throw new Error(target + " is stale. Run bun run types.");
    } else await writeFile(target, content);
  }
} finally {
  await rm(directory, { recursive: true, force: true });
}

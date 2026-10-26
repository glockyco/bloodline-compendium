// Compares every rule with the game's own results that lab `call` runs recorded for the build.
// Usage: bun scripts/rule-check.mjs <build-id>. The results are derived from the game, so they stay in artifacts.
import { readFile, readdir } from "node:fs/promises";
import { join, resolve } from "node:path";
import { ruleMismatches, rules } from "../site/src/lib/rules/check.ts";

const build = process.argv[2];
if (!build)
  throw new Error("Name the build: bun scripts/rule-check.mjs <build-id>");
const runtime = resolve(
  import.meta.dirname,
  "..",
  "artifacts",
  build,
  "runtime",
);
const records = [];
for (const run of await readdir(runtime, { withFileTypes: true })) {
  if (!run.isDirectory()) continue;
  const log = await readFile(
    join(runtime, run.name, "run.jsonl"),
    "utf8",
  ).catch(() => "");
  for (const line of log.split("\n").filter(Boolean)) {
    const entry = JSON.parse(line);
    if (entry.command !== "call" || !entry.ok) continue;
    records.push({
      class: entry.result.target,
      function: entry.result.function,
      rows: entry.result.rows,
    });
  }
}
let compared = 0;
const problems = [];
for (const rule of rules) {
  const own = records.filter(
    (record) => record.function === rule.check.function,
  );
  compared += own.reduce((sum, record) => sum + record.rows.length, 0);
  if (!own.length)
    problems.push(
      `${rule.id} has no recorded game results. Record them with lab call.`,
    );
  problems.push(...ruleMismatches(rule, own));
}
for (const problem of problems) console.error(problem);
console.log(
  `Rules: ${rules.length} rules against ${compared} recorded game results, ${problems.length} problems.`,
);
if (problems.length) process.exitCode = 1;

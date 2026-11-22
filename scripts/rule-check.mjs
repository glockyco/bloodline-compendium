// Compares every rule with the game's own results that lab `call` runs recorded for the build.
// Usage: bun scripts/rule-check.mjs <build-id>. The results are derived from the game, so they stay in artifacts.
import { readFile, readdir } from "node:fs/promises";
import { join, resolve } from "node:path";
import {
  proficiencyMismatches,
  rankCostMismatches,
  ruleMismatches,
  rules,
} from "../site/src/lib/rules/check.ts";

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
const progression = [];
for (const run of await readdir(runtime, { withFileTypes: true })) {
  if (!run.isDirectory()) continue;
  const log = await readFile(
    join(runtime, run.name, "run.jsonl"),
    "utf8",
  ).catch(() => "");
  for (const line of log.split("\n").filter(Boolean)) {
    const entry = JSON.parse(line);
    // Progression runs record level ups and passive purchases through eval of progression.lua.
    if (
      entry.command === "eval" &&
      entry.ok &&
      Array.isArray(entry.result?.steps) &&
      typeof entry.args === "object"
    ) {
      progression.push({ args: entry.args, steps: entry.result.steps });
      continue;
    }
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
// Passive rank costs compare against each passive's cost and increase as the catalog published them.
const site = resolve(import.meta.dirname, "..", "artifacts", build, "site");
const passives = new Map();
for (const file of await readdir(join(site, "disciplines"))) {
  const discipline = JSON.parse(
    await readFile(join(site, "disciplines", file), "utf8"),
  );
  for (const row of discipline.passiveRows)
    for (const passive of row.passives) {
      const [tree, widget] = passive.key.split(":WidgetTree.");
      passives.set(
        tree.split(".").at(-1).replace(/_C$/, "") + ":" + widget,
        passive,
      );
    }
}
for (const check of [
  ["proficiency-points", proficiencyMismatches(progression)],
  ["next-rank-cost", rankCostMismatches(progression, passives)],
]) {
  const [id, result] = check;
  if (!result.compared)
    problems.push(
      `${id} has no recorded game results. Record them with progression.lua.`,
    );
  compared += result.compared;
  problems.push(...result.problems);
}
for (const problem of problems) console.error(problem);
console.log(
  `Rules: ${rules.length + 2} rules against ${compared} recorded game results, ${problems.length} problems.`,
);
if (problems.length) process.exitCode = 1;

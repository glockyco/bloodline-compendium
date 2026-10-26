// Prints a bug report for the game's Discord from a known issue, so the draft always follows its entry.
// Usage: bun scripts/issue-report.mjs [<issue-id> ...]. Without IDs it prints every entry that is not reported yet.
import { readFile } from "node:fs/promises";
import { resolve } from "node:path";

const root = resolve(import.meta.dirname, "..");
const { issues } = JSON.parse(
  await readFile(resolve(root, "issues/known-issues.json"), "utf8"),
);
const ids = process.argv.slice(2);
const missing = ids.filter((id) => !issues.some((issue) => issue.id === id));
if (missing.length)
  throw new Error("Unknown known issues: " + missing.join(", "));
const chosen = ids.length
  ? issues.filter((issue) => ids.includes(issue.id))
  : issues.filter((issue) => issue.report === "notReported");

// Discord rejects messages longer than 2,000 characters.
const limit = 2000;

/** The report for one issue: what players see first, then how to reproduce it, then the cause in the game's code. */
function report(issue) {
  return [
    `**${issue.title}**`,
    issue.effect,
    "",
    "Steps:",
    ...issue.steps.map((step, index) => `${index + 1}. ${step}`),
    "",
    `Expected: ${issue.expected}`,
    `Observed: ${issue.observed}`,
    "",
    `Likely cause, from reading the game's Blueprints: ${issue.cause}`,
    `Functions: ${issue.citations.map((citation) => "`" + citation + "`").join(", ")}`,
    `Steam build: ${issue.builds.join(", ")}`,
  ].join("\n");
}

for (const issue of chosen) {
  const text = report(issue);
  if (text.length > limit)
    throw new Error(
      `The report for ${issue.id} has ${text.length} characters, more than Discord's ${limit}.`,
    );
  console.log(`----- ${issue.id} (${text.length} characters)\n${text}\n`);
}

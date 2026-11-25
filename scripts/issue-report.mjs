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

/** The report's sections, kept intact when Discord needs more than one message. */
function reportSections(issue) {
  return [
    [
      `**${issue.title}**`,
      ...(issue.effect ? [issue.effect] : []),
      ...(issue.effectDetails ?? []).map(
        ({ label, text }) => `**${label}:** ${text}`,
      ),
    ].join("\n"),
    [
      "Steps:",
      ...issue.steps.flatMap(({ action, result }, index) => [
        `${index + 1}. ${action}`,
        `   ${result}`,
      ]),
    ].join("\n"),
    `Expected: ${issue.expected}\nObserved: ${issue.observed}`,
    `Likely cause, from reading the game's Blueprints: ${issue.cause}`,
    [
      `Functions: ${issue.citations.map((citation) => "`" + citation + "`").join(", ")}`,
      `Steam build: ${issue.builds.join(", ")}`,
    ].join("\n"),
  ];
}

for (const issue of chosen) {
  let message = "";
  for (const section of reportSections(issue)) {
    if (section.length > limit)
      throw new Error(
        `A section of ${issue.id} has ${section.length} characters, more than Discord's ${limit}.`,
      );
    if (message && message.length + 2 + section.length > limit) {
      console.log(`----- ${issue.id} (${message.length} characters)\n${message}\n`);
      message = "";
    }
    message = message ? `${message}\n\n${section}` : section;
  }
  console.log(`----- ${issue.id} (${message.length} characters)\n${message}\n`);
}

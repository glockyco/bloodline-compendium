import { readFile, readdir } from "node:fs/promises";
import { join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { parse } from "parse5";

const rules = JSON.parse(
  await readFile(new URL("./voice-rules.json", import.meta.url), "utf8"),
).map((rule) => ({ ...rule, expression: new RegExp(rule.pattern, "iu") }));

// Game text, marked with data-game, keeps the game's wording, so only the site's own words are checked.
const skipped = new Set(["script", "style", "noscript", "template"]);

function siteText(node, parts) {
  if (node.nodeName === "#text") parts.push(node.value);
  if (skipped.has(node.tagName)) return;
  if ((node.attrs ?? []).some((attribute) => attribute.name === "data-game"))
    return;
  for (const child of node.childNodes ?? []) siteText(child, parts);
  // Block boundaries keep words from neighbouring elements apart.
  parts.push(" ");
}

function findBody(node) {
  if (node.tagName === "body") return node;
  for (const child of node.childNodes ?? []) {
    const body = findBody(child);
    if (body) return body;
  }
  return null;
}

/** Fails when the site's own text on a built page uses a banned construction from voice-rules.json. */
export async function voiceGate(directory) {
  const problems = [];
  let pages = 0;
  async function visit(relative = "") {
    for (const entry of await readdir(join(directory, relative), {
      withFileTypes: true,
    })) {
      const file = relative ? relative + "/" + entry.name : entry.name;
      if (entry.isDirectory()) await visit(file);
      else if (entry.isFile() && file.endsWith(".html")) {
        pages++;
        const body = findBody(
          parse(await readFile(join(directory, file), "utf8")),
        );
        if (!body) continue;
        const parts = [];
        siteText(body, parts);
        const text = parts.join("").replace(/\s+/g, " ");
        for (const rule of rules) {
          const match = rule.expression.exec(text);
          if (match)
            problems.push(
              `${file}: "${text.slice(Math.max(0, match.index - 30), match.index + match[0].length + 30).trim()}" (${rule.reason})`,
            );
        }
      }
    }
  }
  await visit();
  if (problems.length)
    throw new Error(
      "The site's own text breaks the voice rules:\n" +
        problems.slice(0, 50).join("\n") +
        (problems.length > 50 ? `\n${problems.length - 50} more.` : ""),
    );
  return { pages, report: `Voice: ${pages} pages follow the voice rules.` };
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  const directory = resolve(process.argv[2] ?? "site/build");
  console.log((await voiceGate(directory)).report);
}

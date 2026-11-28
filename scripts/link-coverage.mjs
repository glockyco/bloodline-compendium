// Finds every entity name the built pages show without a link: names of kinds without pages, and names that text
// written for the site mentions without linking. Counts per kind are recorded in coverage/link-coverage.json and may
// only fall. The names themselves come from game files, so they stay out of the repository: each run writes them to
// artifacts/link-coverage.json, and a failing run names what is new against the previous run's list.
// Usage: bun scripts/link-coverage.mjs <built site directory> [--record]
import { mkdir, readFile, readdir, writeFile } from "node:fs/promises";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { parse } from "parse5";

const root = resolve(import.meta.dirname, "..");
const recordedFile = join(root, "coverage/link-coverage.json");
const namesFile = join(root, "artifacts/link-coverage.json");

// The game's own words, such as item descriptions and quoted dialogue, are quotations and keep their wording.
// Text inside these elements is not read for names.
const skippedTags = new Set(["script", "style", "template", "svg", "h1"]);
// A name needs a link once in the block that shows it, as a wiki links a name once per row or paragraph.
const blockTags = new Set([
  "p",
  "li",
  "td",
  "th",
  "dd",
  "dt",
  "h2",
  "h3",
  "h4",
  "caption",
  "summary",
]);

function attributes(node) {
  return Object.fromEntries(
    (node.attrs ?? []).map(({ name, value }) => [name, value]),
  );
}

function textOf(node) {
  return node.nodeName === "#text"
    ? node.value
    : (node.childNodes ?? []).map(textOf).join("");
}

/**
 * The entity an element names and its name, which is its text unless the element says otherwise. A link to an
 * entity page gets its kind from its address, by the routes of the kinds with pages, and a name without a page
 * carries its kind as EntityLink marks it.
 */
function entityOf(node, routes) {
  const attrs = attributes(node);
  let kind = attrs["data-kind"];
  if (!kind && node.tagName === "a" && "data-game" in attrs && attrs.href)
    kind = routes.find(({ prefix }) =>
      new RegExp("^" + prefix + "[^/?#]+/").test(attrs.href),
    )?.kind;
  if (!kind) return null;
  return {
    kind,
    name: (attrs["data-name"] ?? textOf(node)).replace(/\s+/g, " ").trim(),
  };
}

function escape(name) {
  return name.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

async function htmlFiles(directory, relative = "") {
  const files = [];
  for (const entry of await readdir(join(directory, relative), {
    withFileTypes: true,
  })) {
    const file = relative ? relative + "/" + entry.name : entry.name;
    if (entry.isDirectory()) files.push(...(await htmlFiles(directory, file)));
    else if (file.endsWith(".html")) files.push(file);
  }
  return files.sort();
}

/** Reads the built pages and returns the plain names per kind and the unlinked mentions per page. */
export async function linkCoverage(directory) {
  const routes = JSON.parse(
    await readFile(join(directory, "kinds.json"), "utf8"),
  )
    .filter((kind) => kind.hasPages)
    .map((kind) => ({
      kind: kind.kind,
      prefix: escape("/" + kind.route.replace(/^\/+|\/+$/g, "") + "/"),
    }));
  const pages = [];
  for (const file of await htmlFiles(directory)) {
    const document = parse(await readFile(join(directory, file), "utf8"));
    let main = null;
    (function find(node) {
      if (main) return;
      if (node.tagName === "main") main = node;
      for (const child of node.childNodes ?? []) find(child);
    })(document);
    // Pages marked noindex, such as the presentation fixtures, are not part of the reference.
    let noindex = false;
    (function find(node) {
      const attrs = attributes(node);
      if (
        node.tagName === "meta" &&
        attrs.name === "robots" &&
        /noindex/i.test(attrs.content ?? "")
      )
        noindex = true;
      for (const child of node.childNodes ?? []) find(child);
    })(document);
    if (main && !noindex) pages.push({ file, main });
  }

  // Every entity name any page shows, with its kind, is a name text must link, and a name some page links has
  // somewhere to lead.
  const kindsByName = new Map();
  const plain = new Map();
  const linkable = new Set();
  // The names of entries that links reach at an anchor of a page, such as a passive on its discipline's page, by
  // that page's address. There the name is the entry itself.
  const hosted = new Map();
  for (const { main } of pages)
    (function visit(node) {
      const entity = entityOf(node, routes);
      const kind = entity?.kind;
      const name = entity?.name;
      if (kind && name) {
        if (!kindsByName.has(name)) kindsByName.set(name, new Set());
        kindsByName.get(name).add(kind);
        if (node.tagName === "a") {
          linkable.add(name);
          const href = attributes(node).href;
          if (href?.includes("#")) {
            const host = href.split("#")[0];
            if (!hosted.has(host)) hosted.set(host, new Set());
            hosted.get(host).add(name);
          }
        } else {
          if (!plain.has(kind)) plain.set(kind, new Set());
          plain.get(kind).add(name);
        }
      }
      for (const child of node.childNodes ?? []) visit(child);
    })(main);

  const names = [...kindsByName.keys()].sort(
    (a, b) => b.length - a.length || a.localeCompare(b),
  );
  const pattern = new RegExp(
    "(?<![\\w'-])(?:" + names.map(escape).join("|") + ")(?![\\w'-])",
    "g",
  );
  const unlinked = new Map();
  for (const { file, main } of pages) {
    const path =
      file === "index.html" ? "/" : "/" + file.replace(/index\.html$/, "");
    let self = null;
    (function find(node) {
      if (self !== null) return;
      if (node.tagName === "h1") {
        self = (function text(child) {
          return child.nodeName === "#text"
            ? child.value
            : (child.childNodes ?? []).map(text).join("");
        })(node).trim();
        return;
      }
      for (const child of node.childNodes ?? []) find(child);
    })(main);
    const found = new Set();
    const here = hosted.get(path) ?? new Set();
    // Each block's text and the names it links.
    function block(node) {
      const texts = [];
      const linked = new Set();
      const gameNames = [];
      (function visit(child, top) {
        const attrs = attributes(child);
        if (!top && blockTags.has(child.tagName)) return block(child);
        // Text only a screen reader reads, such as "Details for" a row's name, is no place for a link.
        if (
          skippedTags.has(child.tagName) ||
          attrs["aria-hidden"] === "true" ||
          (attrs.class ?? "").split(/\s+/).includes("sr-only")
        )
          return;
        const entity = entityOf(child, routes);
        if (entity) {
          if (child.tagName === "a") linked.add(entity.name);
          texts.push(" ");
          return;
        }
        // Text inside any other link, such as a card, already leads somewhere.
        if (child.tagName === "a") {
          texts.push(" ");
          return;
        }
        // Game text keeps its wording, but an element that is only a linkable name of two or more words is a name
        // shown without its link. A one-word name there is often a value, such as the item type Pommel, which is
        // also a skill's name.
        if ("data-game" in attrs) {
          const whole = textOf(child).replace(/\s+/g, " ").trim();
          if (linkable.has(whole) && whole.includes(" ")) gameNames.push(whole);
          texts.push(" ");
          return;
        }
        if (child.nodeName === "#text") texts.push(child.value);
        for (const grandchild of child.childNodes ?? [])
          visit(grandchild, false);
      })(node, true);
      for (const name of gameNames)
        if (name !== self && !here.has(name) && !linked.has(name))
          found.add(name);
      const text = texts.join("").replace(/\s+/g, " ");
      for (const match of text.matchAll(pattern)) {
        const name = match[0];
        if (name === self || linked.has(name)) continue;
        const before = text.slice(0, match.index);
        const after = text.slice(match.index + name.length);
        // A name joined to another capitalized word names something else, such as Arcane in Arcane Bolt.
        if (/^ [A-Z]/.test(after)) continue;
        const sentenceStart = /(^|[.!?:] )$/.test(before);
        if (!sentenceStart && /[A-Z][\w'-]* $/.test(before)) continue;
        // A one-word name that starts a sentence may be a common noun, such as Daggers for the weapons.
        if (sentenceStart && !name.includes(" ")) continue;
        found.add(name);
      }
    }
    block(main);
    if (found.size) unlinked.set(path, [...found].sort());
  }
  return { plain, unlinked, kindsByName };
}

/** The counts the recorded file holds: plain names per kind and unlinked mentions per kind of the name. */
export function counts({ plain, unlinked, kindsByName }) {
  const plainCounts = {};
  for (const [kind, names] of [...plain].sort(([a], [b]) => a.localeCompare(b)))
    plainCounts[kind] = names.size;
  const unlinkedCounts = {};
  for (const names of unlinked.values())
    for (const name of names) {
      const kind = [...kindsByName.get(name)].sort().join(" or ");
      unlinkedCounts[kind] = (unlinkedCounts[kind] ?? 0) + 1;
    }
  return {
    plain: plainCounts,
    unlinked: Object.fromEntries(
      Object.entries(unlinkedCounts).sort(([a], [b]) => a.localeCompare(b)),
    ),
  };
}

/**
 * Compares the built pages with the recorded counts. A count above its record fails and names what is new against
 * the previous run's names, and a count below its record fails until the record is lowered.
 */
export async function linkCoverageGate(directory, { record = false } = {}) {
  const result = await linkCoverage(directory);
  const current = counts(result);
  const names = {
    plain: Object.fromEntries(
      [...result.plain].map(([kind, set]) => [kind, [...set].sort()]),
    ),
    unlinked: Object.fromEntries(result.unlinked),
  };
  const previous = await readFile(namesFile, "utf8")
    .then(JSON.parse)
    .catch(() => null);
  await mkdir(dirname(namesFile), { recursive: true });
  await writeFile(namesFile, JSON.stringify(names, null, 2) + "\n");
  if (record) {
    await writeFile(recordedFile, JSON.stringify(current, null, 2) + "\n");
    return { report: "Link coverage recorded in coverage/link-coverage.json." };
  }
  const recorded = await readFile(recordedFile, "utf8")
    .then(JSON.parse)
    .catch(() => {
      throw new Error(
        "coverage/link-coverage.json is missing. Record it with: bun scripts/link-coverage.mjs <built site> --record",
      );
    });
  const problems = [];
  for (const group of ["plain", "unlinked"])
    for (const kind of new Set([
      ...Object.keys(recorded[group]),
      ...Object.keys(current[group]),
    ])) {
      const was = recorded[group][kind] ?? 0;
      const now = current[group][kind] ?? 0;
      if (now > was) {
        let added = "";
        if (previous && group === "plain") {
          const before = new Set(previous.plain[kind] ?? []);
          added = names.plain[kind]
            .filter((name) => !before.has(name))
            .join(", ");
        } else if (previous) {
          added = Object.entries(names.unlinked)
            .flatMap(([path, list]) =>
              list
                .filter(
                  (name) => !(previous.unlinked[path] ?? []).includes(name),
                )
                .map((name) => name + " on " + path),
            )
            .join(", ");
        }
        problems.push(
          (group === "plain"
            ? "Names of kind " + kind + " shown without a link"
            : "Unlinked mentions of " + kind + " names in text") +
            " rose from " +
            was +
            " to " +
            now +
            (added ? ": " + added : ". See artifacts/link-coverage.json") +
            ".",
        );
      } else if (now < was)
        problems.push(
          (group === "plain" ? "Plain " : "Unlinked ") +
            kind +
            " names fell from " +
            was +
            " to " +
            now +
            ". Lower the record with: bun scripts/link-coverage.mjs <built site> --record",
        );
    }
  if (problems.length) throw new Error(problems.join("\n"));
  const plainTotal = Object.values(current.plain).reduce((a, b) => a + b, 0);
  const unlinkedTotal = Object.values(current.unlinked).reduce(
    (a, b) => a + b,
    0,
  );
  return {
    report:
      "Link coverage: " +
      plainTotal +
      " names without a page, " +
      unlinkedTotal +
      " unlinked mentions, none above the record.",
  };
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  const directory = resolve(process.argv[2] ?? "site/build");
  console.log(
    (
      await linkCoverageGate(directory, {
        record: process.argv.includes("--record"),
      })
    ).report,
  );
}

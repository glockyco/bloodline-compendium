// Lists everything the extracted class records hold for one kind of thing, so a page design starts from all of it
// instead of from the fields someone remembered. For every field of the classes below an ancestor, and of the game's
// own components on them, it says how many classes set it apart from the usual value, how many values it takes,
// their range or examples, and which game functions read it. `.agent/skills/page-design/SKILL.md` says where the
// census goes in a design.
// Usage: bun scripts/census.mjs <records directory> --descendants-of <class path> [--only <file of class paths>]
import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

function* files(directory) {
  for (const name of readdirSync(directory)) {
    const path = join(directory, name);
    if (statSync(path).isDirectory()) yield* files(path);
    else if (name.endsWith(".json")) yield path;
  }
}

const short = (path) => path.split(".").at(-1).replace(/_C$/, "");
const canonical = (value) => JSON.stringify(value);

/** A value as a reader can take it in: a reference by its asset name, text by its words, the rest as JSON. */
function shown(value) {
  if (value && typeof value === "object" && !Array.isArray(value)) {
    if (typeof value.$ref === "string") return short(value.$ref);
    if (typeof value.$text === "string") return `"${value.$text}"`;
    if (typeof value.$enum === "string")
      return value.display ?? value.$enum.split("::").at(-1);
  }
  if (Array.isArray(value)) return `[${value.map(shown).join(", ")}]`;
  const text = typeof value === "string" ? `"${value}"` : canonical(value);
  return text.length > 60 ? text.slice(0, 57) + "..." : text;
}

function kindOf(values) {
  const kinds = new Set(
    values
      .filter((value) => value !== null)
      .map((value) => (Array.isArray(value) ? "list" : typeof value)),
  );
  if (kinds.size === 0) return "empty";
  return kinds.size === 1 ? [...kinds][0] : "mixed";
}

const brief = (text) => (text.length > 80 ? text.slice(0, 77) + "..." : text);

/**
 * The census of the classes below `ancestor`: the fields whose value differs between classes, each with its
 * counts, range or examples and readers, the same for every component whose class the game defines, and the
 * fields that hold one value everywhere. A component's field counts with the value the class sets or else the
 * component's own default. `only` narrows the classes to given paths, such as those behind published pages.
 */
export function census(recordsDirectory, ancestor, only = null) {
  const records = new Map();
  for (const path of files(join(recordsDirectory, "classes"))) {
    const record = JSON.parse(readFileSync(path, "utf8"));
    records.set(record.path, record);
  }
  const classes = [...records.values()]
    .filter(
      (record) =>
        record.ancestors?.includes(ancestor) &&
        (!only || only.has(record.path)),
    )
    .sort((a, b) => a.path.localeCompare(b.path));

  const readersByOwner = new Map();
  const index = join(recordsDirectory, "code", "index.json");
  const code = existsSync(index) ? JSON.parse(readFileSync(index, "utf8")) : {};
  for (const reference of code.references ?? []) {
    const at = reference.target?.path?.lastIndexOf(":") ?? -1;
    if (reference.target?.kind !== "property" || at < 0) continue;
    const owner = reference.target.path.slice(0, at);
    const fields = readersByOwner.get(owner) ?? new Map();
    fields.set(reference.target.path.slice(at + 1), reference.readers ?? []);
    readersByOwner.set(owner, fields);
  }
  // What a class does beyond its fields, such as an attack of its own, shows only in the functions it defines.
  const defined = new Map();
  const assets = new Map(
    classes.map((record) => [record.path.split(".")[0], short(record.path)]),
  );
  for (const { citation } of code.functions ?? []) {
    const at = citation?.lastIndexOf(":") ?? -1;
    const owner = at < 0 ? null : assets.get(citation.slice(0, at));
    const name = owner && citation.slice(at + 1);
    if (!name || generated.test(name)) continue;
    defined.set(name, [...(defined.get(name) ?? []), owner]);
  }
  /** The functions that read each field declared on the given classes or their ancestors. */
  const readersOf = (paths) => {
    const readers = new Map();
    const owners = new Set(
      paths.flatMap((path) => [path, ...(records.get(path)?.ancestors ?? [])]),
    );
    for (const owner of owners)
      for (const [field, found] of readersByOwner.get(owner) ?? []) {
        const all = readers.get(field) ?? new Set();
        for (const reader of found) all.add(reader);
        readers.set(field, all);
      }
    return readers;
  };

  /** One field's row: how its values spread over the classes that hold it. */
  const row = (name, holders, readers) => {
    const groups = new Map();
    for (const { record, value } of holders) {
      const key = canonical(value);
      const group = groups.get(key) ?? { value, classes: [] };
      group.classes.push(short(record.path));
      groups.set(key, group);
    }
    const ordered = [...groups.values()].sort(
      (a, b) => b.classes.length - a.classes.length,
    );
    const values = holders.map((holder) => holder.value);
    const numbers = values.filter((value) => typeof value === "number");
    const read = [...(readers.get(name) ?? [])].sort();
    return {
      name,
      kind: kindOf(values),
      classes: holders.length,
      apart: holders.length - ordered[0].classes.length,
      distinct: ordered.length,
      usual: shown(ordered[0].value),
      range:
        numbers.length === values.length && numbers.length
          ? [Math.min(...numbers), Math.max(...numbers)]
          : null,
      examples: ordered
        .slice(1, 4)
        .map(
          (group) =>
            `${shown(group.value)} (${group.classes.slice(0, 2).join(", ")}${group.classes.length > 2 ? ` and ${group.classes.length - 2} more` : ""})`,
        ),
      readers: read.length,
      readBy: read.slice(0, 3).map((reader) => reader.split("/").at(-1)),
    };
  };
  /** A class's own parts, such as its mesh, differ on every class by name alone and say nothing about it. */
  const ownPart = ({ record, value }) =>
    typeof value?.$ref === "string" &&
    value.$ref.includes(`.Default__${record.path.split(".").at(-1)}:`);
  const byApart = (a, b) => b.apart - a.apart || a.name.localeCompare(b.name);
  const split = (pick, readers) => {
    const holders = new Map();
    for (const record of classes)
      for (const [name, value] of Object.entries(pick(record) ?? {})) {
        const found = holders.get(name) ?? [];
        found.push({ record, value });
        holders.set(name, found);
      }
    const fields = [...holders]
      .filter(([, found]) => !found.every(ownPart))
      .map(([name, found]) => row(name, found, readers));
    return {
      varying: fields.filter((field) => field.distinct > 1).sort(byApart),
      uniform: fields
        .filter((field) => field.distinct === 1)
        .map((field) => `${field.name}: ${brief(field.usual)}`)
        .sort(),
    };
  };

  const named = new Map();
  for (const record of classes)
    for (const [name, component] of Object.entries(record.components ?? {})) {
      const found = named.get(name) ?? { types: new Set(), classes: 0 };
      found.types.add(component.class);
      found.classes += 1;
      named.set(name, found);
    }
  const components = new Map();
  const engine = [];
  for (const [name, { types, classes: count }] of [...named].sort(([a], [b]) =>
    a.localeCompare(b),
  )) {
    const type = [...types].map(short).join(" or ");
    if (![...types].some((path) => path.startsWith("/Game/"))) {
      engine.push(`${name} (${type}, ${count})`);
      continue;
    }
    const values = (record) => {
      const component = record.components?.[name];
      return (
        component && {
          ...records.get(component.class)?.properties,
          ...component.properties,
        }
      );
    };
    components.set(name, {
      type,
      classes: count,
      ...split(values, readersOf([...types])),
    });
  }
  return {
    classes: classes.map((record) => record.path),
    ...split(
      (record) => record.properties,
      readersOf(classes.map((record) => record.path)),
    ),
    components,
    engine,
    functions: [...defined]
      .map(([name, owners]) => ({ name, classes: owners.sort() }))
      .sort(
        (a, b) =>
          b.classes.length - a.classes.length || a.name.localeCompare(b.name),
      ),
  };
}

const cell = (text) => String(text).replaceAll("|", "\\|");

/** Function names that say nothing: the compiler's for event graphs, timelines and callbacks, and the engine's events. */
const generated =
  /^(ExecuteUbergraph_|BndEvt__|InpActEvt_|InpAxisEvt_)|__(UpdateFunc|FinishedFunc|EventFunc|DelegateSignature)$|_[0-9A-F]{32}$|^(Receive[A-Z]\w*|UserConstructionScript|CustomEvent(_\d+)?)$/;

/** The census as Markdown: the fields that differ, the same for each of the game's components, and the rest named. */
export function report(result, ancestor) {
  const table = (fields) => [
    "| Field | Kind | Classes apart from the usual value | Values | Usual value | Range or other values | Read by |",
    "| --- | --- | --- | --- | --- | --- | --- |",
    ...fields.map((field) =>
      [
        "",
        cell(field.name),
        field.kind,
        `${field.apart} of ${field.classes}`,
        field.distinct,
        cell(field.usual),
        cell(
          field.range
            ? `${field.range[0]} to ${field.range[1]}`
            : field.examples.join("; "),
        ),
        field.readers
          ? cell(`${field.readers}: ${field.readBy.join(", ")}`)
          : "none",
        "",
      ]
        .join(" | ")
        .trim(),
    ),
    "",
  ];
  const same = (uniform) =>
    uniform.length
      ? [`One value on every class: ${uniform.join("; ")}`, ""]
      : [];
  return [
    `# Census of ${short(ancestor)}: ${result.classes.length} classes`,
    "",
    "A field that no function reads may be unused by the game. A field that many read is not yet explained: read the functions and observe the game before a page states what it does.",
    "",
    `## Fields that differ between classes (${result.varying.length})`,
    "",
    ...table(result.varying),
    ...same(result.uniform),
    ...[...result.components].flatMap(([name, component]) => [
      `## Component ${name} (${component.type}, on ${component.classes} classes)`,
      "",
      ...(component.varying.length ? table(component.varying) : []),
      ...same(component.uniform),
    ]),
    `## Functions the classes define (${result.functions.length})`,
    "",
    "Each with the classes that define it. A function of one or a few classes is often an attack or an ability of theirs.",
    "",
    ...result.functions.map(
      ({ name, classes }) =>
        `- ${name}: ${classes.length} (${classes.slice(0, 4).join(", ")}${classes.length > 4 ? ` and ${classes.length - 4} more` : ""})`,
    ),
    "",
    `## Engine components (${result.engine.length})`,
    "",
    "Shapes, meshes, sounds and widgets, each with its class and the number of classes that carry it. Their settings are left out.",
    "",
    result.engine.join(", "),
    "",
  ].join("\n");
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  const [directory, ...rest] = process.argv.slice(2);
  const option = (name) =>
    rest.includes(name) ? rest[rest.indexOf(name) + 1] : null;
  const ancestor = option("--descendants-of");
  if (!directory || !ancestor) {
    console.error(
      "Usage: bun scripts/census.mjs <records directory> --descendants-of <class path> [--only <file of class paths>]",
    );
    process.exit(2);
  }
  const only = option("--only");
  const result = census(
    resolve(directory),
    ancestor,
    only
      ? new Set(
          readFileSync(only, "utf8")
            .split("\n")
            .map((line) => line.trim())
            .filter(Boolean),
        )
      : null,
  );
  if (!result.classes.length) {
    console.error(
      `No class below ${ancestor}. Give the full class path, such as /Game/Folder/Name.Name_C.`,
    );
    process.exit(1);
  }
  console.log(report(result, ancestor));
}

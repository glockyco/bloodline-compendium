// Checks that a design comparison and its reference board hold what a design decision needs before it is asked:
// a board of references with screenshots, an account of what the game holds for the thing, a brief, and options
// that differ and name what they borrow.
// `.agent/skills/page-design/SKILL.md` gives the procedure and the file formats.
// Usage: bun scripts/check-comparison.mjs <comparison.md>
//        bun scripts/check-comparison.mjs --board <board.md>
//        bun scripts/check-comparison.mjs --data <data.md>
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

/** The text under each `## ` heading, by heading. */
function sections(text) {
  const found = new Map();
  let heading = null;
  for (const line of text.split("\n")) {
    const match = /^## (.+?)\s*$/.exec(line);
    if (match) found.set((heading = match[1]), []);
    else if (heading) found.get(heading).push(line);
  }
  return new Map([...found].map(([name, lines]) => [name, lines.join("\n")]));
}

/** The `### ` entries of a section, each with its title and its `- Name: value` lines. */
function entries(section) {
  return section
    .split(/^### /m)
    .slice(1)
    .map((block) => {
      const [title, ...lines] = block.split("\n");
      const fields = new Map();
      for (const line of lines) {
        const match = /^- ([A-Z][A-Za-z]*): *(.*)$/.exec(line);
        if (match) fields.set(match[1], match[2].trim());
      }
      return { title: title.trim(), fields };
    });
}

const bullets = (section) =>
  section.split("\n").filter((line) => /^(- |\d+\. )\S/.test(line));
const tableRows = (section) =>
  section
    .split("\n")
    .filter((line) => /^\|/.test(line) && !/^\|[\s:|-]+\|$/.test(line))
    .slice(1);
const images = (text) =>
  [...text.matchAll(/!\[[^\]]*\]\(([^)\s]+)\)/g)].map((match) => match[1]);
const missingImages = (text, directory) =>
  images(text).filter(
    (path) => !/^https?:/.test(path) && !existsSync(resolve(directory, path)),
  );
/** Runs a check on a section of a file, or reports the section as missing with what it holds. */
const reader = (parts, problems, file) => (name, holds, check) => {
  const part = parts.get(name);
  if (part === undefined)
    problems.push(`${file} has no "## ${name}" section, which ${holds}.`);
  else check(part);
};

/**
 * What a reference board lacks. A board answers how others show the thing: the site today, the sibling compendiums,
 * large wikis and the game itself, each with a screenshot and what to borrow, then the data and assets the catalog
 * has and the patterns worth an option.
 */
export function checkBoard(path) {
  if (!existsSync(path))
    return [
      `${path} is missing. Start the design-research agent for the topic.`,
    ];
  const text = readFileSync(path, "utf8");
  const directory = dirname(path);
  const problems = [];
  const section = reader(sections(text), problems, "The board");
  section(
    "Questions",
    "names the questions a player brings to the page",
    (part) => {
      if (bullets(part).length < 1)
        problems.push(
          "Questions names no question a player brings to the page.",
        );
    },
  );
  const groups = [
    [
      "Ours",
      1,
      false,
      "shows the page on this site today and the components to reuse",
    ],
    [
      "Siblings",
      2,
      true,
      "shows how Afallon, Ancient Kingdoms and Erenshor present the thing",
    ],
    ["Wikis", 3, true, "shows how at least 3 large wikis present the thing"],
    ["Game", 1, false, "shows how the game itself presents the thing"],
  ];
  const hosts = new Set();
  for (const [name, least, linked, holds] of groups)
    section(name, holds, (part) => {
      const found = entries(part);
      if (found.length < least)
        problems.push(
          `${name} has ${found.length} entries and needs at least ${least}.`,
        );
      for (const entry of found) {
        const where = `${name}, "${entry.title}"`;
        const url = entry.fields.get("URL") ?? "";
        if (linked && !/^https?:\/\/\S+$/.test(url))
          problems.push(`${where} has no URL.`);
        if (name === "Wikis" && /^https?:/.test(url))
          hosts.add(new URL(url).hostname);
        const shown = images(entry.fields.get("Shot") ?? "").length > 0;
        // The game is shown by a screenshot or by a recorded lab run. Only the game may lack both, with its reason.
        const run =
          name === "Game" &&
          (entry.fields.get("Run") || entry.fields.get("None"));
        if (!shown && !run) problems.push(`${where} has no screenshot.`);
        for (const field of ["Shows", "Borrow"])
          if (!entry.fields.get(field))
            problems.push(`${where} has no "${field}:" line.`);
      }
    });
  if (hosts.size > 0 && hosts.size < 3)
    problems.push(
      `Wikis covers ${hosts.size} sites and needs at least 3 different ones.`,
    );
  section(
    "Data and assets",
    "lists what the catalog holds for the thing",
    (part) => {
      if (tableRows(part).length < 3)
        problems.push(
          "Data and assets needs a table of at least 3 things with whether the catalog has them.",
        );
    },
  );
  section(
    "Patterns",
    "names the ways to show the thing that are worth an option",
    (part) => {
      if (bullets(part).length < 3)
        problems.push(
          "Patterns needs at least 3 numbered patterns, each naming the entries that show it.",
        );
    },
  );
  for (const path of missingImages(text, directory))
    problems.push(`The board shows ${path}, which does not exist.`);
  return problems;
}

const cells = (row) =>
  row
    .split(/(?<!\\)\|/)
    .slice(1, -1)
    .map((cell) => cell.trim().replaceAll("\\|", "|"));

/**
 * The names of a census that a design has to account for: each field that differs between the classes and that a
 * function reads, on the classes and on the game's components, and each function the classes define.
 */
function censusNames(text) {
  const names = new Set();
  for (const [heading, part] of sections(text))
    if (/^(Fields that differ|Component )/.test(heading)) {
      for (const row of tableRows(part).map(cells))
        if (row.at(-1) !== "none") names.add(row[0]);
    } else if (/^Functions the classes define/.test(heading))
      for (const line of part.split("\n")) {
        const name = /^- (.+): \d+ \(/.exec(line)?.[1];
        if (name) names.add(name);
      }
  return [...names];
}

/**
 * What a data file lacks. It accounts for the census of a kind of thing: each row says what a player could learn,
 * the census names behind it, how their meaning is known and whether it goes on the page. A row goes on the page
 * only when a function was read or the game was observed, never on a name alone.
 */
export function checkData(path) {
  if (!existsSync(path))
    return [
      `${path} is missing. Take the census of the kind and account for it, as stage 1 of .agent/skills/page-design/SKILL.md says.`,
    ];
  const text = readFileSync(path, "utf8");
  const problems = [];
  const link = /\]\(([^)\s]*census\.md)\)/.exec(text)?.[1];
  const census = link && resolve(dirname(path), link);
  if (!link && !/^No census: \S/m.test(text))
    problems.push(
      'The data file links no census.md. Run `bun scripts/census.mjs` for the kind\'s ancestor class and link its output, or give a line "No census: <why the kind has no class records>".',
    );
  if (census && !existsSync(census))
    problems.push(
      `${link} does not exist. Run \`bun scripts/census.mjs\` for the kind's ancestor class.`,
    );
  const rows = tableRows(text).map(cells);
  if (rows.length < 3)
    problems.push(
      "The data file needs a table of at least 3 rows, each with what a player learns, the census names behind it, how that is known and whether it goes on the page.",
    );
  const named = new Set();
  for (const [learns, names = "", known = "", shown = ""] of rows) {
    const where = `Data, "${learns}"`;
    for (const name of names.split(",")) named.add(name.trim());
    if (!/^(Read: \S|Seen: \S|Name only$)/.test(known))
      problems.push(
        `${where}: the third cell is "Read: <function>", "Seen: <lab run>" or "Name only".`,
      );
    if (!/^(Yes$|Later: \S|No: \S)/.test(shown))
      problems.push(
        `${where}: the last cell is "Yes", "Later: <task>" or "No: <reason>".`,
      );
    else if (shown === "Yes" && known === "Name only")
      problems.push(
        `${where}: goes on the page but is known by name only. Read the functions that use it or check it in the lab first.`,
      );
  }
  if (census && existsSync(census)) {
    const listed = rows.map((row) => row[1] ?? "").join("\n");
    const missing = censusNames(readFileSync(census, "utf8")).filter(
      (name) =>
        !named.has(name) && !(name.includes(",") && listed.includes(name)),
    );
    if (missing.length)
      problems.push(
        `The data file leaves out ${missing.length} census name${missing.length === 1 ? "" : "s"}: ${missing.join(", ")}. Put each in a row, with "No: <reason>" when a player has no use for it.`,
      );
  }
  return problems;
}

/** The evidence files a review file links. */
const evidenceLinks = (review) =>
  [...review.matchAll(/\]\(([^)\s]+-evidence\.md)\)/g)].map(
    (match) => match[1],
  );

/** The options of a question card: those that are new, as letter and text, and whether it offers the rest. */
function card(body) {
  const options = [...body.matchAll(/^- \[ \] (.+)$/gm)].map((match) =>
    match[1].trim(),
  );
  const isCurrent = (option) => /^(Current|Before)\b/i.test(option);
  const isNone = (option) => /^None of these/i.test(option);
  return {
    fresh: options.filter((option) => !isCurrent(option) && !isNone(option)),
    none: options.some(isNone),
    recommended: /^Recommended: \S/m.test(body),
    why: /^Why: \S/m.test(body),
  };
}

/**
 * What a design comparison lacks: the review file with its question cards and screenshots, and the evidence file
 * it links. The evidence names its case. Explore needs the board, the inventory and the game's own presentation.
 * Choose needs each option to name a file of the repository that already has the form it uses. `root` is the
 * repository those paths are read from.
 */
export function checkComparison(
  path,
  root = resolve(dirname(fileURLToPath(import.meta.url)), ".."),
) {
  if (!existsSync(path)) return [`${path} does not exist.`];
  const review = readFileSync(path, "utf8");
  const directory = dirname(path);
  const problems = [];
  const cards = [...review.matchAll(/^:::question\n([\s\S]*?)\n:::/gm)].map(
    (match) => card(match[1]),
  );
  if (!cards.length)
    problems.push(
      "The review file has no question card. To show built work for sign-off, open its screenshots with `plannotator annotate --gate` instead.",
    );
  const letters = new Set();
  cards.forEach((found, index) => {
    const name = `Question ${index + 1}`;
    if (found.fresh.length < 2)
      problems.push(
        `${name} offers ${found.fresh.length} new option${found.fresh.length === 1 ? "" : "s"} and needs 2 to 4 that differ in structure. One built design against the current page is a sign-off, not a comparison.`,
      );
    if (found.fresh.length > 4)
      problems.push(
        `${name} offers ${found.fresh.length} new options and may offer 4.`,
      );
    if (!found.none) problems.push(`${name} does not offer "None of these".`);
    if (!found.recommended)
      problems.push(`${name} has no "Recommended:" line.`);
    if (!found.why) problems.push(`${name} has no "Why:" line.`);
    for (const option of found.fresh) {
      const letter = /^([A-Z]):/.exec(option)?.[1];
      if (!letter)
        problems.push(
          `${name}: "${option}" does not start with its letter, as in "A: ...".`,
        );
      else letters.add(letter);
    }
  });
  const rows = review.split("\n").filter((line) => /^\|/.test(line));
  for (const letter of letters)
    if (
      !rows.some(
        (row) =>
          new RegExp(`^\\| *${letter}[: ]`).test(row) &&
          images(row).length >= 2,
      )
    )
      problems.push(
        `Option ${letter} has no table row with its desktop and phone screenshots.`,
      );
  for (const image of missingImages(review, directory))
    problems.push(`The review file shows ${image}, which does not exist.`);

  // A comparison of wording alone links no evidence file. Its card and screenshots are checked, and the
  // comparison-check agent says whether its options change more than wording.
  const linked = evidenceLinks(review);
  if (linked.length === 0) return problems;
  if (linked.length > 1) {
    problems.push(
      `The review file links ${linked.length} evidence files and may link one, named <comparison>-evidence.md.`,
    );
    return problems;
  }
  const evidencePath = resolve(directory, linked[0]);
  if (!existsSync(evidencePath))
    return [...problems, `${linked[0]} does not exist.`];
  const evidence = readFileSync(evidencePath, "utf8");
  const kind = /^Case: (Choose|Explore)\. \S/m.exec(evidence)?.[1];
  if (!kind)
    problems.push(
      'The evidence file has no "Case: Choose. <fact>" or "Case: Explore. <fact>" line. `.agent/skills/page-design/SKILL.md` says which holds.',
    );
  const explore = kind !== "Choose";
  const all = reader(sections(evidence), problems, "The evidence file");
  const section = (name, holds, check) =>
    explore || !["Board", "Data", "Inventory", "In the game"].includes(name)
      ? all(name, holds, check)
      : undefined;
  section(
    "Player questions",
    "names what a player wants to know or do on the page",
    (part) => {
      if (bullets(part).length < 1)
        problems.push(
          "Player questions names no question a player brings to the page.",
        );
    },
  );
  let titles = [];
  section("Board", "links the topic's reference board", (part) => {
    const boardLink = /\]\(([^)\s]+board\.md)\)/.exec(part)?.[1];
    if (!boardLink)
      return problems.push(
        "Board links no board.md. Start the design-research agent for the topic and link its board.",
      );
    const boardPath = resolve(dirname(evidencePath), boardLink);
    problems.push(...checkBoard(boardPath));
    if (existsSync(boardPath))
      titles = [...sections(readFileSync(boardPath, "utf8"))].flatMap(
        ([, text]) => entries(text).map((entry) => entry.title.toLowerCase()),
      );
  });
  section("Data", "links the kind's data file", (part) => {
    const dataLink = /\]\(([^)\s]+data\.md)\)/.exec(part)?.[1];
    if (!dataLink)
      return problems.push(
        "Data links no data.md. Take the census of the kind and account for it, as stage 1 of .agent/skills/page-design/SKILL.md says.",
      );
    problems.push(...checkData(resolve(dirname(evidencePath), dataLink)));
  });
  section(
    "Inventory",
    "lists what pages of this kind show elsewhere and whether this change shows it",
    (part) => {
      if (tableRows(part).length < 3)
        problems.push(
          "Inventory needs a table of at least 3 things that pages of this kind show, with who shows it, whether the catalog has it and whether this change shows it.",
        );
    },
  );
  section("In the game", "says how the game itself shows the thing", (part) => {
    if (
      !images(part).length &&
      !/\d{4}-\d{2}-\d{2}T[\d-]+Z/.test(part) &&
      !/^The game does not show this/m.test(part)
    )
      problems.push(
        'In the game needs a screenshot, a lab run, or a line that starts "The game does not show this" with the reason.',
      );
  });
  section(
    "Options",
    explore
      ? "says what each option borrows from the board"
      : "names the file that already has the form each option uses",
    (part) => {
      for (const letter of letters) {
        const line = bullets(part).find((bullet) =>
          bullet.startsWith(`- ${letter}:`),
        );
        if (!line)
          problems.push(
            `Options has no "- ${letter}:" line saying what option ${letter} borrows.`,
          );
        else if (!explore) {
          const paths = [...line.matchAll(/`([^`\s]+\/[^`\s]+)`/g)].map(
            (match) => match[1],
          );
          if (!paths.some((file) => existsSync(resolve(root, file))))
            problems.push(
              `Options, ${letter}: names no existing file that already has the form it uses. A form the site does not have yet makes the case Explore.`,
            );
        } else if (
          titles.length &&
          !titles.some((title) => line.toLowerCase().includes(title))
        )
          problems.push(
            `Options, ${letter}: names no board entry it borrows from. Use an entry's title as the board writes it.`,
          );
      }
    },
  );
  section(
    "Earlier decisions",
    "names the decisions in openspec/ and earlier comparisons that apply",
    (part) => {
      if (bullets(part).length < 1)
        problems.push(
          "Earlier decisions lists none. Search openspec/ and artifacts/design/process/trial/ and say what applies, or what you searched for.",
        );
    },
  );
  for (const image of missingImages(evidence, dirname(evidencePath)))
    problems.push(`The evidence file shows ${image}, which does not exist.`);
  return problems;
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  const mode = ["--board", "--data"].find((flag) => flag === process.argv[2]);
  const board = mode === "--board";
  const path = process.argv[mode ? 3 : 2];
  if (!path) {
    console.error(
      "Usage: bun scripts/check-comparison.mjs <comparison.md> | --board <board.md> | --data <data.md>",
    );
    process.exit(2);
  }
  const problems = board
    ? checkBoard(resolve(path))
    : mode
      ? checkData(resolve(path))
      : checkComparison(resolve(path));
  if (problems.length) {
    console.error(problems.map((problem) => "- " + problem).join("\n"));
    console.error(
      `\n${problems.length} gap${problems.length === 1 ? "" : "s"}. Fill each one. Do not open the comparison yet.`,
    );
    process.exitCode = 1;
  } else if (board) console.log("The board is complete.");
  else if (mode) console.log("The data file accounts for the census.");
  else if (evidenceLinks(readFileSync(resolve(path), "utf8")).length)
    console.log(
      "The comparison is complete. Start the comparison-check agent with its path.",
    );
  else
    console.log(
      "Checked as a comparison of wording alone, because it links no evidence file. When an option changes layout, structure or what the page shows, write the evidence file first. Otherwise start the comparison-check agent with its path.",
    );
}

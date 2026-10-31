// Equips every armor item that the catalog notes with an armor issue and reads the armor the game applies, with a
// screenshot of the character sheet each time, so each noted page rests on its own observation. Run it inside
// `nix develop` with the lab game launched with `launch render` on a new save:
//   bun tools/runtime/checks/noted-armor.mjs <build-id>
// Pieces other than knee armor count without another piece, so from a bare character each item that adds no armor
// leaves Total Armor at 0, and one that replaces it in its slot leaves it there too. Each knee item is equipped next,
// then a head piece that adds armor, which makes the game count the knees: the total stays put after the knees and
// then rises by both.
import { execFileSync } from "node:child_process";
import { readFile } from "node:fs/promises";
import { join, resolve } from "node:path";

const build = process.argv[2];
if (!build) throw new Error("Name the build: noted-armor.mjs <build-id>");
const root = resolve(import.meta.dirname, "../../..");
const lab = (...args) =>
  JSON.parse(
    execFileSync("bun", [join(import.meta.dirname, "../lab.mjs"), ...args], {
      encoding: "utf8",
      stdio: ["ignore", "pipe", "pipe"],
    }),
  );
const issues = JSON.parse(
  await readFile(join(root, "artifacts", build, "site/issues.json"), "utf8"),
);
const noted = (id) =>
  issues.find((entry) => entry.issue.id === id)?.affected ?? [];
const pieces = [
  ...noted("armor-without-row"),
  ...noted("armor-slot-adds-none"),
];
const knees = noted("knee-armor-not-counted");
const head = "/Game/BloodlinesAlpha/Interactables/Armor/Head";
// Two heads that add armor, Feral Maw with 1 and Areman Iron Corinthian Helm with 9, so each knee item meets a new one.
const heads = [
  `${head}/HeadArmor_Tusks.HeadArmor_Tusks_C`,
  `${head}/Iron_NasalHelm.Iron_NasalHelm_C`,
];
const armor = () =>
  lab(
    "player",
    JSON.stringify({ values: ["PlayerPassiveLevelStatComponent.Total Armor"] }),
  ).values["PlayerPassiveLevelStatComponent.Total Armor"];
const equip = (key, name) => {
  lab("give", JSON.stringify({ items: [key] }));
  lab("equip", JSON.stringify({ item: key }));
  lab("close-tutorials");
  if (name) lab("capture-screen", name);
};

lab("open-sheet");
const rows = [];
for (const piece of pieces) {
  equip(piece.key, `armor-${piece.slug}`);
  rows.push({ item: piece.name, issue: "piece", totalArmor: armor() });
}
for (const [index, knee] of knees.entries()) {
  const before = armor();
  equip(knee.key, `knee-${knee.slug}`);
  const afterKnee = armor();
  equip(heads[index % heads.length], `knee-${knee.slug}-head`);
  rows.push({
    item: knee.name,
    issue: "knee",
    before,
    afterKnee,
    afterHead: armor(),
  });
}
console.log(JSON.stringify(rows, null, 2));

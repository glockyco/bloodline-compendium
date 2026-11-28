import { expect, it } from 'vitest';
import { mkdir, mkdtemp, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { checkComparison } from '../../scripts/check-comparison.mjs';

const entry = (site, linked = true) =>
	[
		`### ${site}: creature page`,
		'',
		...(linked
			? [`- URL: https://${site.toLowerCase().replaceAll(' ', '-')}.example/creature`]
			: []),
		'- Shot: ![](shot.png)',
		'- Shows: Level, health and drops in an infobox beside a portrait.',
		'- Borrow: The portrait beside the figures.',
		''
	].join('\n');

const board = [
	'# Board: creature pages',
	'## Questions',
	'- How tough is this creature at my level?',
	'## Ours',
	entry('Ours', false),
	'## Siblings',
	entry('Afallon'),
	entry('Ancient Kingdoms'),
	'## Wikis',
	entry('Terraria'),
	entry('Old School RuneScape'),
	entry('Path of Exile'),
	'## Game',
	entry('Health bar', false),
	'## Data and assets',
	'| Thing | Catalog has it | Where |',
	'| --- | --- | --- |',
	'| Health | Yes | MaxHealth |',
	'| Level range | Yes | EnemyLevelManager |',
	'| Portrait | No | needs a render |',
	'## Patterns',
	'1. A table with a row per level. Shown by: Path of Exile.',
	'2. A calculator for the player level. Shown by: Afallon.',
	'3. An infobox beside a portrait. Shown by: Old School RuneScape.'
].join('\n');

const census = [
	'# Census of Base: 3 classes',
	'## Fields that differ between classes (3)',
	'| Field | Kind | Classes apart from the usual value | Values | Usual value | Range or other values | Read by |',
	'| --- | --- | --- | --- | --- | --- | --- |',
	'| MaxHealth | number | 1 of 3 | 2 | 50 | 50 to 400 | 2: Base:Receive Damage, Hero:Swing |',
	'| Armor | number | 1 of 3 | 2 | 0 | 0 to 250 | 1: Base:Receive Damage |',
	'| Editor Note | string | 1 of 3 | 2 | "" | "wip" (Troll) | none |',
	'## Component LevelManager (LevelManager, on 3 classes)',
	'| Field | Kind | Classes apart from the usual value | Values | Usual value | Range or other values | Read by |',
	'| --- | --- | --- | --- | --- | --- | --- |',
	'| Base Level | number | 1 of 3 | 2 | 1 | 1 to 15 | 1: LevelManager:Initialize |',
	'## Functions the classes define (2)',
	'- Leap Attack: 1 (Troll)',
	'- Hair: 2 (Troll, Wolf)'
].join('\n');

const data = (rows) =>
	[
		'# Data: creature pages',
		'[Census](census.md)',
		'| A player learns | Census names | Known from | On the page |',
		'| --- | --- | --- | --- |',
		...rows
	].join('\n');

const accounted = data([
	'| How much damage it takes to kill | MaxHealth, Armor, Base Level | Read: Base:Receive Damage | Yes |',
	'| Its special attacks | Leap Attack | Name only | Later: 3.6 |',
	'| Nothing: looks | Hair | Name only | No: looks only |'
]);

const evidence = (options) =>
	[
		'# Evidence',
		'Case: Explore. The site shows no creature stats yet.',
		'## Player questions',
		'- How tough is this creature at my level?',
		'## Board',
		'[Board](references/board.md)',
		'## Data',
		'[Data](references/data.md)',
		'## Inventory',
		'| Pages of this kind show | Who shows it | Catalog has it | In this change |',
		'| --- | --- | --- | --- |',
		'| Health by level | Path of Exile | Yes | Yes |',
		'| Portrait | Old School RuneScape | No | No, it needs renders first |',
		'| Drops | All | Yes | Yes |',
		'## In the game',
		'![Health bar](references/shot.png)',
		'## Options',
		...options,
		'## Earlier decisions',
		'- Creature pages list drops in a table.'
	].join('\n');

const review = (card, rows) =>
	[
		'# Creature stats',
		'[Evidence](review-evidence.md)',
		'| | 1440 | 390 |',
		'| --- | --- | --- |',
		...rows,
		':::question',
		'How should a creature page show stats that rise with its level?',
		'',
		...card,
		':::'
	].join('\n');

/** Writes a comparison with its evidence, board and screenshots into a temporary folder and checks it. */
async function check(reviewText, evidenceText, dataText = accounted) {
	const directory = await mkdtemp(join(tmpdir(), 'bloodline-comparison-'));
	try {
		await mkdir(join(directory, 'references'));
		await writeFile(join(directory, 'review.md'), reviewText);
		await writeFile(join(directory, 'review-evidence.md'), evidenceText);
		await writeFile(join(directory, 'references/board.md'), board);
		await writeFile(join(directory, 'references/census.md'), census);
		await writeFile(join(directory, 'references/data.md'), dataText);
		for (const image of ['a.png', 'b.png', 'references/shot.png'])
			await writeFile(join(directory, image), '');
		return checkComparison(join(directory, 'review.md'), directory);
	} finally {
		await rm(directory, { recursive: true, force: true });
	}
}

it('passes a comparison of two designs that each borrow from the board', async () => {
	const problems = await check(
		review(
			[
				'Recommended: A: a table by level',
				'',
				'- [ ] A: a table by level',
				'- [ ] B: a calculator for your level',
				'- [ ] Current: no stats',
				'- [ ] None of these',
				'',
				'Why: A shows every level at once.'
			],
			['| A: Wolf | ![](a.png) | ![](a.png) |', '| B: Wolf | ![](b.png) | ![](b.png) |']
		),
		evidence([
			'- A: borrows the level table of Path of Exile: creature page.',
			'- B: borrows the level input of Afallon: creature page.'
		])
	);
	expect(problems).toEqual([]);
});

it('reports one built design asked against the current page as a sign-off', async () => {
	const problems = await check(
		review(
			[
				'Recommended: A: as shown',
				'',
				'- [ ] A: As shown',
				'- [ ] Before: no stats',
				'- [ ] None of these',
				'',
				'Why: It answers the question.'
			],
			['| A: Wolf | ![](a.png) | ![](a.png) |']
		),
		evidence(['- A: borrows the level table of Path of Exile: creature page.'])
	);
	expect(problems).toHaveLength(1);
	expect(problems[0]).toContain('offers 1 new option');
});

it('reports an option that borrows from nothing on the board and one without screenshots', async () => {
	const problems = await check(
		review(
			[
				'Recommended: A: a table by level',
				'',
				'- [ ] A: a table by level',
				'- [ ] B: a calculator for your level',
				'- [ ] None of these',
				'',
				'Why: A shows every level at once.'
			],
			['| A: Wolf | ![](a.png) | ![](a.png) |']
		),
		evidence(['- A: borrows the level table of Path of Exile: creature page.', '- B: my own idea.'])
	);
	expect(problems).toEqual([
		'Option B has no table row with its desktop and phone screenshots.',
		"Options, B: names no board entry it borrows from. Use an entry's title as the board writes it."
	]);
});

const twoForms = review(
	[
		'Recommended: A: one line per item',
		'',
		'- [ ] A: one line per item',
		'- [ ] B: two lines per item',
		'- [ ] None of these',
		'',
		'Why: A keeps the list short.'
	],
	['| A: Ashford | ![](a.png) | ![](a.png) |', '| B: Ashford | ![](b.png) | ![](b.png) |']
);

const chosen = (options) =>
	[
		'# Evidence',
		'Case: Choose. Both card forms are in use on place pages.',
		'## Player questions',
		'- What lies on the ground here?',
		'## Options',
		...options,
		'## Earlier decisions',
		'- Place pages show each thing as a card.'
	].join('\n');

it('passes a choice between two forms the site already has without a board', async () => {
	const problems = await check(
		twoForms,
		chosen([
			'- A: the one-line card of `references/board.md`.',
			'- B: the two-line card of `references/board.md`.'
		])
	);
	expect(problems).toEqual([]);
});

it('reports a choice whose option uses a form no file of the site has', async () => {
	const problems = await check(
		twoForms,
		chosen([
			'- A: the one-line card of `references/board.md`.',
			'- B: a new gallery, as `site/src/lib/Gallery.svelte` would be.'
		])
	);
	expect(problems).toEqual([
		'Options, B: names no existing file that already has the form it uses. A form the site does not have yet makes the case Explore.'
	]);
});

const wording = (card) =>
	[
		'# The empty place line',
		'| | 1440 | 390 |',
		'| --- | --- | --- |',
		'| A: Ashford | ![](a.png) | ![](a.png) |',
		'| B: Ashford | ![](b.png) | ![](b.png) |',
		':::question',
		'How should a place without item sources say so?',
		'',
		...card,
		':::'
	].join('\n');

it('checks a wording comparison without evidence by its question card alone', async () => {
	const two = [
		'Recommended: A: say nothing',
		'',
		'- [ ] A: say nothing',
		'- [ ] B: "No one here gives items."',
		'- [ ] Current: "Nothing here gives items."',
		'- [ ] None of these',
		'',
		'Why: The sections already say what is there.'
	];
	expect(await check(wording(two), '')).toEqual([]);
	const one = two.filter((line) => !line.startsWith('- [ ] B'));
	expect(await check(wording(one), '')).toEqual([
		expect.stringContaining('Question 1 offers 1 new option')
	]);
});

const levels = review(
	[
		'Recommended: A: a table by level',
		'',
		'- [ ] A: a table by level',
		'- [ ] B: a calculator for your level',
		'- [ ] None of these',
		'',
		'Why: A shows every level at once.'
	],
	['| A: Wolf | ![](a.png) | ![](a.png) |', '| B: Wolf | ![](b.png) | ![](b.png) |']
);
const borrowed = evidence([
	'- A: borrows the level table of Path of Exile: creature page.',
	'- B: borrows the level input of Afallon: creature page.'
]);

it('reports census names the data file leaves out, except fields no function reads', async () => {
	const problems = await check(
		levels,
		borrowed,
		data([
			'| How much damage it takes to kill | MaxHealth | Read: Base:Receive Damage | Yes |',
			'| Its special attacks | Leap Attack | Name only | Later: 3.6 |',
			'| Nothing: looks | Hair | Name only | No: looks only |'
		])
	);
	expect(problems).toEqual([
		'The data file leaves out 2 census names: Armor, Base Level. Put each in a row, with "No: <reason>" when a player has no use for it.'
	]);
});

it('reports a row that goes on the page while its meaning is known by name only', async () => {
	const problems = await check(
		levels,
		borrowed,
		data([
			'| How much damage it takes to kill | MaxHealth, Armor, Base Level | Read: Base:Receive Damage | Yes |',
			'| Its special attacks | Leap Attack | Name only | Yes |',
			'| Nothing: looks | Hair | Name only | No: looks only |'
		])
	);
	expect(problems).toHaveLength(1);
	expect(problems[0]).toContain(
		'"Its special attacks": goes on the page but is known by name only'
	);
});

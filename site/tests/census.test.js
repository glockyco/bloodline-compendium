import { expect, it } from 'vitest';
import { mkdir, mkdtemp, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { census, report } from '../../scripts/census.mjs';

const base = '/Game/Creatures/Base.Base_C';
const manager = '/Game/Creatures/LevelManager.LevelManager_C';

const creature = (name, properties, level) => ({
	path: `/Game/Creatures/${name}.${name}_C`,
	ancestors: [base],
	properties: {
		Mesh: { $ref: `/Game/Creatures/${name}.Default__${name}_C:Mesh` },
		Faction: 'Wild',
		...properties
	},
	components: {
		LevelManager: { class: manager, properties: level },
		Hitbox: { class: '/Script/Engine.BoxComponent', properties: { Extent: name.length } }
	}
});

const records = [
	{ path: base, ancestors: [], properties: {} },
	{ path: manager, ancestors: [], properties: { 'Base Level': 1, 'Max Level': 10 } },
	creature('Wolf', { Health: 50, Armor: 0 }, {}),
	creature('Boar', { Health: 50, Armor: 0 }, {}),
	creature('Troll', { Health: 400, Armor: 250 }, { 'Base Level': 15 }),
	{ path: '/Game/Props/Barrel.Barrel_C', ancestors: [], properties: { Health: 9000 } }
];

const references = [
	{
		target: { kind: 'property', path: `${base}:Health` },
		readers: ['/Game/Creatures/Base:Receive Damage', '/Game/Player/Hero:Swing']
	},
	{
		target: { kind: 'property', path: `${manager}:Base Level` },
		readers: [`/Game/Creatures/LevelManager:Initialize`]
	},
	{
		target: { kind: 'property', path: '/Game/Props/Barrel.Barrel_C:Armor' },
		readers: ['/Game/Props/Barrel:Break']
	}
];

const functions = [
	{ citation: '/Game/Creatures/Troll:Leap Attack' },
	{ citation: '/Game/Creatures/Troll:ExecuteUbergraph_Troll' },
	{ citation: '/Game/Creatures/Troll:OnCompleted_405E59674B9AC7EFF2E6B1882CD4BF28' },
	{ citation: '/Game/Creatures/Wolf:Bite' },
	{ citation: '/Game/Creatures/Boar:Bite' },
	{ citation: '/Game/Props/Barrel:Break' }
];

/** Writes the records into a temporary folder and takes the census of the creatures in it. */
async function taken() {
	const directory = await mkdtemp(join(tmpdir(), 'bloodline-census-'));
	try {
		await mkdir(join(directory, 'classes'));
		await mkdir(join(directory, 'code'));
		for (const [number, record] of records.entries())
			await writeFile(join(directory, 'classes', `${number}.json`), JSON.stringify(record));
		await writeFile(join(directory, 'code/index.json'), JSON.stringify({ references, functions }));
		return census(directory, base);
	} finally {
		await rm(directory, { recursive: true, force: true });
	}
}

it('reports a field that differs with its spread and the functions that read it', async () => {
	const result = await taken();

	expect(result.classes).toHaveLength(3);
	expect(result.varying.map((field) => field.name)).toEqual(['Armor', 'Health']);
	expect(result.varying[1]).toMatchObject({
		apart: 1,
		classes: 3,
		distinct: 2,
		usual: '50',
		range: [50, 400],
		readers: 2,
		readBy: ['Base:Receive Damage', 'Hero:Swing']
	});
});

it("keeps another class's readers of a field with the same name apart", async () => {
	const armor = (await taken()).varying[0];

	expect(armor.readers).toBe(0);
});

it("names a field with one value everywhere and leaves out a class's own parts", async () => {
	const result = await taken();

	expect(result.uniform).toEqual(['Faction: "Wild"']);
});

it("counts a component field with the component's default where a class does not set it", async () => {
	const level = (await taken()).components.get('LevelManager');

	expect(level.varying).toHaveLength(1);
	expect(level.varying[0]).toMatchObject({
		name: 'Base Level',
		apart: 1,
		classes: 3,
		usual: '1',
		range: [1, 15],
		readBy: ['LevelManager:Initialize']
	});
	expect(level.uniform).toEqual(['Max Level: 10']);
});

it('names an engine component without its settings', async () => {
	const result = await taken();

	expect(result.components.has('Hitbox')).toBe(false);
	expect(result.engine).toEqual(['Hitbox (BoxComponent, 3)']);
	expect(report(result, base)).not.toContain('Extent');
});

it('lists the functions the classes define without the names the compiler makes', async () => {
	const result = await taken();

	expect(result.functions).toEqual([
		{ name: 'Bite', classes: ['Boar', 'Wolf'] },
		{ name: 'Leap Attack', classes: ['Troll'] }
	]);
});

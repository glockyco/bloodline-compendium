import { describe, expect, it } from 'vitest';
import Ajv2020 from 'ajv/dist/2020.js';
import { readFile, readdir } from 'node:fs/promises';
import schema from './site-data.schema.json';
import type { CoverageReport, ItemIndex } from './site-data.js';
const directory = new URL('../../fixtures/site-data/', import.meta.url);
const ajv = new Ajv2020({ allErrors: true, strict: false });
ajv.addSchema(schema, 'site');
async function validate(name: string, type: string) {
	const data = JSON.parse(await readFile(new URL(name, directory), 'utf8'));
	const valid = ajv.compile({ $ref: 'site#/$defs/' + type });
	expect(valid(data), JSON.stringify(valid.errors)).toBe(true);
}
describe('invented fixtures match the exported contract', () => {
	it('validates every JSON file', async () => {
		await validate('build.json', 'SiteBuild');
		await validate('items.json', 'ItemIndex');
		await validate('coverage.json', 'CoverageReport');
		await validate('graph.json', 'EntityGraph');
		const kinds = JSON.parse(await readFile(new URL('kinds.json', directory), 'utf8'));
		const validKind = ajv.compile({ $ref: 'site#/$defs/EntityKindDefinition' });
		for (const kind of kinds) expect(validKind(kind), JSON.stringify(validKind.errors)).toBe(true);
		const issues = JSON.parse(await readFile(new URL('issues.json', directory), 'utf8'));
		const validIssue = ajv.compile({ $ref: 'site#/$defs/PublishedIssue' });
		for (const issue of issues)
			expect(validIssue(issue), JSON.stringify(validIssue.errors)).toBe(true);
		for (const name of await readdir(new URL('items/', directory)))
			await validate('items/' + name, 'ItemPage');
	});
	it('uses complete item references for coverage entries', async () => {
		const coverage: CoverageReport = JSON.parse(
			await readFile(new URL('coverage.json', directory), 'utf8')
		);
		const index: ItemIndex = JSON.parse(await readFile(new URL('items.json', directory), 'utf8'));
		for (const gap of coverage.gaps)
			for (const entry of gap.entries) {
				if (!entry.entity) continue;
				const item = index.items.find((item) => item.key === entry.entity!.key);
				expect(item).toBeDefined();
				expect(entry.entity).toEqual({
					kind: item!.kind,
					key: item!.key,
					name: item!.name,
					slug: item!.slug,
					icon: item!.icon
				});
			}
	});
	it('rejects a malformed fact rather than accepting arbitrary data', () => {
		const valid = ajv.compile({ $ref: 'site#/$defs/Fact' });
		expect(valid({ label: 'Damage', value: 12 })).toBe(false);
	});
});

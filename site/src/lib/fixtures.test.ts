import { describe, expect, it } from 'vitest';
import Ajv2020 from 'ajv/dist/2020.js';
import { readFile, readdir } from 'node:fs/promises';
import schema from './site-data.schema.json';
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
	it('rejects a malformed fact rather than accepting arbitrary data', () => {
		const valid = ajv.compile({ $ref: 'site#/$defs/Fact' });
		expect(valid({ label: 'Damage', value: 12 })).toBe(false);
	});
});

import { readFile } from 'node:fs/promises';
import { resolve, join } from 'node:path';
import type {
	SiteBuild,
	ItemIndex,
	ItemPage,
	CoverageReport,
	EntityKindDefinition,
	EntityGraph,
	PublishedIssue
} from '#lib/site-data.js';
const directory = resolve(process.env.BLOODLINE_SITE_DATA || 'fixtures/site-data');
async function json<T>(name: string): Promise<T> {
	return JSON.parse(await readFile(join(directory, name), 'utf8')) as T;
}
export const readBuild = () => json<SiteBuild>('build.json');
export const readIndex = () => json<ItemIndex>('items.json');
export const readCoverage = () => json<CoverageReport>('coverage.json');
export const readItem = (slug: string) => json<ItemPage>('items/' + slug + '.json');
export const readKinds = () => json<EntityKindDefinition[]>('kinds.json');
export const readGraph = () => json<EntityGraph>('graph.json');
export const readIssues = () => json<PublishedIssue[]>('issues.json');

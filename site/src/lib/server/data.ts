import { readFile, stat } from 'node:fs/promises';
import { resolve, join } from 'node:path';
import type {
	SiteBuild,
	ItemIndex,
	ItemPage,
	EntityKindDefinition,
	EntityGraph,
	PublishedIssue
} from '#lib/site-data.js';
const directory = resolve(process.env.BLOODLINE_SITE_DATA || 'fixtures/site-data');
// Every page reads the same few files, and graph.json alone is large, so each file is parsed once until it changes.
const cache = new Map<string, { modified: number; value: Promise<unknown> }>();
async function json<T>(name: string): Promise<T> {
	const path = join(directory, name);
	const modified = (await stat(path)).mtimeMs;
	const cached = cache.get(path);
	if (cached?.modified === modified) return cached.value as Promise<T>;
	const value = readFile(path, 'utf8').then((text) => JSON.parse(text) as T);
	cache.set(path, { modified, value });
	return value;
}
export const readBuild = () => json<SiteBuild>('build.json');
export const readIndex = () => json<ItemIndex>('items.json');
export const readItem = (slug: string) => json<ItemPage>('items/' + slug + '.json');
export const readKinds = () => json<EntityKindDefinition[]>('kinds.json');
export const readGraph = () => json<EntityGraph>('graph.json');
export const readIssues = () => json<PublishedIssue[]>('issues.json');

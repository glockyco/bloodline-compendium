import { readFile, stat } from 'node:fs/promises';
import { resolve, join } from 'node:path';
import type {
	SiteBuild,
	ItemIndex,
	ItemPage,
	EntityKindDefinition,
	EntityGraph,
	EntityReference,
	PublishedIssue,
	DisciplineIndex,
	DisciplinePage,
	SkillIndex,
	SkillPage,
	ProfessionIndex,
	ProfessionPage,
	ResourceIndex,
	ResourcePage,
	MechanicIndex,
	MechanicPage,
	PlannerData
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
/** The references that text written for the site links to, by their target, such as discipline:blood-magic. */
export const readLinks = () => json<Record<string, EntityReference>>('links.json');
export const readDisciplines = () => json<DisciplineIndex>('disciplines.json');
export const readDiscipline = (slug: string) =>
	json<DisciplinePage>('disciplines/' + slug + '.json');
export const readSkills = () => json<SkillIndex>('skills.json');
/** The character planner's inputs, which only the planner route reads. */
export const readPlanner = () => json<PlannerData>('planner.json');
export const readSkill = (slug: string) => json<SkillPage>('skills/' + slug + '.json');
export const readMechanics = () => json<MechanicIndex>('mechanics.json');
export const readMechanic = (slug: string) => json<MechanicPage>('mechanics/' + slug + '.json');
export const readProfessions = () => json<ProfessionIndex>('professions.json');
export const readResources = () => json<ResourceIndex>('resources.json');
export const readResource = (slug: string) => json<ResourcePage>('resources/' + slug + '.json');
export const readProfession = (slug: string) =>
	json<ProfessionPage>('professions/' + slug + '.json');
/**
 * The merchant names that several merchants share, such as Shopkeeper. Such a name is a role, which a sentence
 * writes with "the", while a person's name stands alone.
 */
export async function readSharedMerchantNames(): Promise<string[]> {
	const counts = new Map<string, number>();
	for (const { entity } of (await readGraph()).entities)
		if (entity.kind === 'merchant') counts.set(entity.name, (counts.get(entity.name) ?? 0) + 1);
	return [...counts].filter(([, count]) => count > 1).map(([name]) => name);
}

import {
	readDiscipline,
	readDisciplines,
	readItem,
	readSharedMerchantNames,
	readSkill,
	readSkills
} from './data.js';
import { entries as itemEntries } from '../../routes/items/[slug]/+page.server.js';
import { firstRoute } from '../items/acquisition.js';
import { sourceHeadings } from '../labels.js';
import { rankCosts } from '../rules/progression.js';
import { disciplineContents, skillStatus } from '../skills/describe.js';
import type {
	DisciplinePreview,
	HostedPreviews,
	ItemPreview,
	PassivePreview,
	Preview,
	PreviewResource,
	SkillPreview,
	SpellsongPreview
} from '../entity/previews.js';
import type { ItemPage, SkillPage, SourceKind } from '../site-data.js';

/**
 * For each kind with previews, the slugs that have one and how a slug's preview is built. A hosted kind's slug is
 * its host page's, whose resource holds the previews of all its entries there.
 */
export const previewSources: {
	[Kind in Preview['kind']]: {
		slugs(): Promise<string[]>;
		build(slug: string): Promise<Extract<PreviewResource, { kind: Kind }>>;
	};
} = {
	item: {
		async slugs() {
			return (await itemEntries()).map((entry) => entry.slug);
		},
		async build(slug) {
			const [item, shared] = await Promise.all([readItem(slug), readSharedMerchantNames()]);
			return itemPreview(item, new Set(shared));
		}
	},
	skill: {
		async slugs() {
			return [...new Set((await readSkills()).skills.map((skill) => skill.slug))];
		},
		async build(slug) {
			return skillPreview(await readSkill(slug));
		}
	},
	passive: {
		async slugs() {
			return (await readDisciplines()).disciplines
				.filter((discipline) => discipline.passives)
				.map((discipline) => discipline.slug);
		},
		async build(slug): Promise<HostedPreviews<PassivePreview>> {
			const discipline = await readDiscipline(slug);
			return {
				kind: 'passive',
				entries: Object.fromEntries(
					discipline.passiveRows.flatMap((row) =>
						row.passives.map((passive) => [
							passive.anchor,
							{
								kind: 'passive',
								name: passive.name,
								icon: passive.icon,
								description: passive.description,
								unlocks: `${passive.levelDiscipline.name} level ${passive.requiredLevel}`,
								costs: rankCosts(passive)
							}
						])
					)
				)
			};
		}
	},
	spellsong: {
		async slugs() {
			return (await readDisciplines()).disciplines
				.filter((discipline) => discipline.spellsongs)
				.map((discipline) => discipline.slug);
		},
		async build(slug): Promise<HostedPreviews<SpellsongPreview>> {
			const discipline = await readDiscipline(slug);
			return {
				kind: 'spellsong',
				entries: Object.fromEntries(
					discipline.spellsongs.map((song) => [
						song.anchor,
						{
							kind: 'spellsong',
							name: song.name,
							description: song.description,
							tune: song.tune,
							charges: song.charges,
							experience: song.experience,
							route: song.sources.length ? null : 'No known source.'
						}
					])
				)
			};
		}
	},
	discipline: {
		async slugs() {
			return (await readDisciplines()).disciplines.map((discipline) => discipline.slug);
		},
		async build(slug): Promise<DisciplinePreview> {
			const summary = (await readDisciplines()).disciplines.find(
				(discipline) => discipline.slug === slug
			)!;
			return {
				kind: 'discipline',
				name: summary.name,
				category: summary.category.name,
				contents: disciplineContents(summary)
			};
		}
	}
};

/** A skill's preview: its tooltip, and its first way to get it or why none is known, for each version. */
export function skillPreview(skill: SkillPage): SkillPreview {
	const tree = skill.sources.find((source) => source.kind === 'skillTree');
	// A tree's skill unlocks at a level of its own discipline, whose name the condition already carries. Only a tree
	// without a level of its own checks another discipline, and then both names matter.
	const level = tree?.conditions[0];
	const treeRoute =
		tree &&
		(!level
			? tree.name
			: level.entity?.name === tree.name
				? level.label
				: `${tree.name}, ${level.label}`);
	return {
		kind: 'skill',
		versionId: skill.versionId ?? null,
		versions: skill.versions.map(skillPreview),
		name: skill.name,
		icon: skill.icon,
		description: skill.description,
		facts: skill.facts,
		route: treeRoute
			? `${treeRoute}.`
			: (firstRoute(skill.sources, Object.keys(sourceHeadings) as SourceKind[]) ??
				skillStatus(skill))
	};
}

/** Each grouped preview retains the tooltip of every version at the name's one resource URL. */
export function itemPreview(item: ItemPage, shared: ReadonlySet<string> = new Set()): ItemPreview {
	return {
		kind: 'item',
		versionId: item.versionId ?? null,
		versions: item.versions.map((version) => itemPreview(version, shared)),
		name: item.name,
		icon: item.icon,
		description: item.description,
		facts: item.facts,
		projectileGroups: item.projectileGroups,
		onHitEffects: item.onHitEffects,
		route:
			firstRoute(item.sources, Object.keys(sourceHeadings) as SourceKind[], shared) ??
			(item.status === 'notInNormalPlay'
				? 'Cannot be obtained in normal play.'
				: 'No known source.')
	};
}

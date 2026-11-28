import {
	readDiscipline,
	readDisciplines,
	readItem,
	readMechanics,
	readProfessions,
	readResources,
	readSharedMerchantNames,
	readSkill,
	readSkills
} from './data.js';
import { levelRange, plural } from '../format.js';
import { plainText } from '../links.js';
import { resourceLine } from '../resources/summary.js';
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
	SpellsongPreview,
	SummaryPreview
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
	},
	// Resources, professions and mechanics preview what their list cards show.
	resource: {
		async slugs() {
			return (await readResources()).resources.map((resource) => resource.slug);
		},
		async build(slug): Promise<SummaryPreview<'resource'>> {
			const resource = (await readResources()).resources.find((entry) => entry.slug === slug)!;
			return {
				kind: 'resource',
				name: resource.name,
				icon: resource.icon ?? null,
				lines: [resourceLine(resource)]
			};
		}
	},
	profession: {
		async slugs() {
			return (await readProfessions()).professions.map((profession) => profession.slug);
		},
		async build(slug): Promise<SummaryPreview<'profession'>> {
			const profession = (await readProfessions()).professions.find(
				(entry) => entry.slug === slug
			)!;
			return {
				kind: 'profession',
				name: profession.name,
				icon: profession.icon ?? null,
				lines: [
					...(profession.stations.length
						? [`Made at ${profession.stations.map((station) => station.name).join(', ')}`]
						: []),
					...(profession.minLevel != null
						? [`Levels ${levelRange(profession.minLevel, profession.maxLevel)}`]
						: []),
					`Makes ${plural(profession.count, 'item', 'items')}`
				]
			};
		}
	},
	mechanic: {
		async slugs() {
			return (await readMechanics()).mechanics.map((mechanic) => mechanic.slug);
		},
		async build(slug): Promise<SummaryPreview<'mechanic'>> {
			const mechanic = (await readMechanics()).mechanics.find((entry) => entry.slug === slug)!;
			return {
				kind: 'mechanic',
				name: mechanic.name,
				icon: null,
				lines: [plainText(mechanic.intro)]
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

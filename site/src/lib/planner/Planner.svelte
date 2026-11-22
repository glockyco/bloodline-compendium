<script lang="ts">
	import { onMount, untrack } from 'svelte';
	import { page } from '$app/state';
	import Section from '../ui/Section.svelte';
	import LinkedText from '../ui/LinkedText.svelte';
	import { entityPath } from '../entities.js';
	import { formatNumber } from '../format.js';
	import { SITE_ORIGIN } from '../seo.js';
	import type { Attribute } from '../rules/attributes.js';
	import type { EntityKindDefinition, PlannerData } from '../site-data.js';
	import AttributePanel from './AttributePanel.svelte';
	import Hotbar from './Hotbar.svelte';
	import TreePanel from './TreePanel.svelte';
	import { evaluate, type Diagnostic } from './evaluate.js';
	import { decodeBuild, encodeBuild, LinkError } from './link.js';
	import {
		buyRank,
		newCharacter,
		placeOnHotbar,
		removeRank,
		setAttribute,
		setBloodMagicPoints,
		setDisciplineLevel,
		setLevel,
		toggleLearned,
		toggleSkill,
		type Edit,
		type PlannerState
	} from './state.js';
	import {
		activeCharacter,
		createCharacter,
		deleteCharacter,
		duplicateCharacter,
		emptyStore,
		loadStore,
		localBackend,
		renameCharacter,
		selectCharacter,
		updateCharacter,
		type CharacterStore,
		type StoreResult
	} from './store.js';
	import { buildText } from './text.js';
	/**
	 * The skill planner. It edits one character through the planner's actions, evaluates the whole build after every
	 * edit, and keeps characters in the browser. A build from a link opens as a preview that changes no stored
	 * character until it is saved.
	 */
	let { data, kinds }: { data: PlannerData; kinds: EntityKindDefinition[] } = $props();

	// The planner opens on a new character, and a stored character or a link replaces it once the browser has them.
	let plan: PlannerState = $state(untrack(() => newCharacter(data)));
	let store: CharacterStore = $state(emptyStore());
	let preview = $state(false);
	let notice = $state('');
	let link = $state('');
	let text = $state('');
	let name = $state('');
	// Set once the planner has read the browser's characters and any link, so tests and scripts can wait for it.
	let ready = $state(false);
	let chosen = $state(untrack(() => data.disciplines[0]?.key ?? ''));
	const backend = localBackend();
	const evaluation = $derived(evaluate(plan, data));
	const active = $derived(activeCharacter(store));
	const discipline = $derived(data.disciplines.find((entry) => entry.key === chosen));
	const bloodMagic = $derived(data.disciplines.find((entry) => entry.levelName === 'Blood Magic'));
	const categories = $derived(Map.groupBy(data.disciplines, (entry) => entry.category));
	const learnable = $derived(data.skills.filter((skill) => skill.learned));
	const names = $derived(
		new Map([
			...data.skills.map((skill) => [skill.key, skill.name] as const),
			...data.disciplines.map((entry) => [entry.key, entry.name] as const),
			...data.disciplines.flatMap((entry) =>
				entry.passives.map((passive) => [passive.key, passive.name] as const)
			)
		])
	);
	const nameOf = (key: string) => names.get(key) ?? 'Something this game version lacks';

	function stored(result: StoreResult) {
		store = result.store;
		notice = result.saved ? '' : (result.problem ?? '');
	}

	function apply(edit: Edit) {
		if (edit.refused) {
			notice = edit.refused;
			return;
		}
		notice = '';
		plan = edit.state;
		link = '';
		text = '';
		if (!preview && active) stored(updateCharacter(backend, store, active.id, plan));
	}

	onMount(async () => {
		await start();
		ready = true;
	});

	async function start() {
		const loaded = loadStore(backend);
		store = loaded.store;
		if (loaded.problem) notice = loaded.problem;
		const shared = page.url.searchParams.get('build');
		if (shared) {
			try {
				const decoded = await decodeBuild(shared, data);
				plan = decoded.state;
				preview = true;
				if (decoded.unresolved.length)
					notice = `${decoded.unresolved.length} choices of this build name things this game version lacks. They stay in the build, marked.`;
			} catch (error) {
				notice = error instanceof LinkError ? error.message : 'The build link could not be read.';
			}
			return;
		}
		const current = activeCharacter(store);
		if (current) plan = current.state;
	}

	function describe(diagnostic: Diagnostic): string {
		switch (diagnostic.kind) {
			case 'level':
				return `${nameOf(diagnostic.entry)} needs ${nameOf(diagnostic.discipline)} level ${formatNumber(diagnostic.needs)}.`;
			case 'ranks':
				return `${nameOf(diagnostic.entry)} has ${diagnostic.ranks} ranks, and ${diagnostic.max} are known.`;
			case 'overspent':
				return `The build spends ${formatNumber(diagnostic.by)} more ${diagnostic.balance} than it has.`;
			case 'attribute':
				return `${diagnostic.attribute} effects are known up to 21 points.`;
			case 'unresolved':
				return 'A choice names something this game version lacks.';
			case 'hotbar':
				return `Slot ${(diagnostic.slot % 5) + 1} of row ${Math.floor(diagnostic.slot / 5) + 1} holds a skill the character does not know.`;
		}
	}

	// Each copy also shows what it copied, so a reader can select it where the browser refuses the clipboard.
	async function copy(kind: 'link' | 'text') {
		text = '';
		try {
			link = `${SITE_ORIGIN}/planner/?build=${await encodeBuild(plan, SITE_ORIGIN)}`;
		} catch (error) {
			link = '';
			notice = error instanceof LinkError ? error.message : 'The build link could not be made.';
			return;
		}
		if (kind === 'text')
			text = buildText(
				plan,
				data,
				link,
				(slug) =>
					SITE_ORIGIN + entityPath({ kind: 'skill', key: slug, name: '', slug, icon: null }, kinds)!
			);
		try {
			await navigator.clipboard.writeText(kind === 'link' ? link : text);
			notice = kind === 'link' ? 'The build link is copied.' : 'The build is copied as text.';
		} catch {
			notice = 'The browser did not allow copying. Select it below instead.';
		}
	}

	function saveAsNew() {
		stored(
			createCharacter(
				backend,
				store,
				name.trim() || `Character ${store.characters.length + 1}`,
				plan
			)
		);
		preview = false;
		name = '';
	}
</script>

<noscript><p>The planner needs JavaScript to edit a build.</p></noscript>
<p class="notice" role="status" aria-live="polite">{notice}</p>

<div class="planner" data-ready={ready || undefined}>
	<div class="side">
		<Section id="character" title="Character">
			{#if preview}<p>This is a shared build. Save it as a character to keep your changes.</p>{/if}
			<div class="characters">
				<label
					><span class="field-label">Saved Character</span><select
						value={active?.id ?? ''}
						onchange={(event) => {
							const id = event.currentTarget.value || null;
							stored(selectCharacter(backend, store, id));
							const current = activeCharacter(store);
							plan = current ? current.state : newCharacter(data);
							preview = false;
						}}
						><option value="">Not saved</option
						>{#each store.characters as character (character.id)}<option value={character.id}
								>{character.name}</option
							>{/each}</select
					></label
				>
				<label
					><span class="field-label">Name</span><input
						type="search"
						bind:value={name}
						placeholder={active?.name ?? 'New character'}
					/></label
				>
				<div class="buttons">
					<button type="button" onclick={saveAsNew}>Save as New</button>
					{#if active && !preview}<button
							type="button"
							disabled={!name.trim()}
							onclick={() => {
								stored(renameCharacter(backend, store, active.id, name.trim()));
								name = '';
							}}>Rename</button
						><button
							type="button"
							onclick={() =>
								stored(
									duplicateCharacter(
										backend,
										store,
										active.id,
										name.trim() || `${active.name} copy`
									)
								)}>Duplicate</button
						><button
							type="button"
							onclick={() => {
								stored(deleteCharacter(backend, store, active.id));
								plan = newCharacter(data);
							}}>Delete</button
						>{/if}
				</div>
			</div>
			<label class="level"
				><span class="field-label">Character Level</span><input
					type="number"
					min="1"
					value={plan.level}
					onchange={(event) => apply(setLevel(plan, Number(event.currentTarget.value)))}
				/></label
			>
			<p class="balances">
				{formatNumber(evaluation.skillPoints.remaining)} of {formatNumber(
					evaluation.skillPoints.earned
				)} skill points left<br />{formatNumber(evaluation.levelUpPoints.remaining)} of {formatNumber(
					evaluation.levelUpPoints.earned
				)} level up points left
			</p>
			{#if bloodMagic}<label class="level"
					><span class="field-label">Blood Magic Points</span><input
						type="number"
						min="0"
						value={plan.bloodMagicPoints ?? ''}
						onchange={(event) =>
							apply(
								setBloodMagicPoints(
									plan,
									event.currentTarget.value === '' ? null : Number(event.currentTarget.value)
								)
							)}
					/></label
				>
				<p class="muted">
					<LinkedText
						text="[Blood Levels](mechanic:blood-levels) sets these points, not your Blood Magic level, so enter what your skill tree shows."
					/>
				</p>{/if}
		</Section>
		<Section id="attributes" title="Attributes">
			<AttributePanel
				levels={plan.attributes}
				{evaluation}
				change={(attribute: Attribute, level: number) =>
					apply(setAttribute(plan, attribute, level))}
			/>
		</Section>
		<Section id="share" title="Share">
			<div class="buttons">
				<button type="button" onclick={() => copy('link')}>Copy Link</button>
				<button type="button" onclick={() => copy('text')}>Copy as Text</button>
			</div>
			{#if text}<textarea readonly rows="8" aria-label="Build as text">{text}</textarea
				>{:else if link}<input type="search" readonly value={link} aria-label="Build link" />{/if}
		</Section>
	</div>
	<div class="main">
		{#if evaluation.diagnostics.length}<Section
				id="problems"
				title="What the Game Would Refuse"
				count={evaluation.diagnostics.length}
			>
				<ul>
					{#each evaluation.diagnostics as diagnostic, index (index)}<li>
							{describe(diagnostic)}
						</li>{/each}
				</ul>
			</Section>{/if}
		<Section id="skill-bar" title="Skill Bar">
			<Hotbar
				state={plan}
				{data}
				place={(slot, skill) => apply(placeOnHotbar(plan, slot, skill))}
			/>
		</Section>
		<Section id="disciplines" title="Disciplines">
			<label
				><span class="field-label">Discipline</span><select bind:value={chosen}
					>{#each [...categories] as [category, entries] (category)}<optgroup label={category}
							>{#each entries as entry (entry.key)}<option value={entry.key}
									>{entry.name}{(plan.disciplines[entry.key] ?? 1) > 1
										? ` (level ${plan.disciplines[entry.key]})`
										: ''}</option
								>{/each}</optgroup
						>{/each}</select
				></label
			>
			{#if discipline}<TreePanel
					{discipline}
					{data}
					state={plan}
					{evaluation}
					setLevel={(level) => apply(setDisciplineLevel(plan, discipline.key, level))}
					toggleSkill={(skill) => apply(toggleSkill(plan, data, skill))}
					buyRank={(passive) => apply(buyRank(plan, data, passive))}
					removeRank={(passive) => apply(removeRank(plan, passive))}
				/>{/if}
		</Section>
		<Section
			id="learned"
			title="Skills From Books and Events"
			count={learnable.length}
			line="A book, a conversation or an event teaches these skills. They cost no skill point."
		>
			<ul class="learned">
				{#each learnable as skill (skill.key)}<li>
						<label class="check"
							><input
								type="checkbox"
								checked={plan.learned.includes(skill.key)}
								onchange={(event) => {
									apply(toggleLearned(plan, data, skill.key));
									event.currentTarget.checked = plan.learned.includes(skill.key);
								}}
							/><span data-game>{skill.name}</span></label
						>
					</li>{/each}
			</ul>
		</Section>
	</div>
</div>

<style>
	.planner {
		display: grid;
		gap: var(--gap-section);
	}
	@media (width >= 64rem) {
		.planner {
			grid-template-columns: var(--size-item-side) minmax(0, 1fr);
			align-items: start;
		}
	}
	.side,
	.main {
		display: grid;
		gap: var(--gap-section);
		min-width: 0;
	}
	.characters {
		display: grid;
		gap: var(--gap-block);
	}
	.buttons {
		display: flex;
		flex-wrap: wrap;
		gap: var(--gap-inline);
	}
	.level input {
		width: var(--size-filter-control);
	}
	.balances {
		margin: 0;
	}
	textarea {
		width: 100%;
		padding: var(--inset-cell) var(--inset);
		border: var(--border-width) solid var(--border-control);
		border-radius: var(--radius-1);
		background: var(--surface-panel);
		font: inherit;
		color: inherit;
	}
	.notice:empty {
		display: none;
	}
	.learned {
		display: grid;
		grid-template-columns: repeat(auto-fill, minmax(var(--size-card-narrow), 1fr));
		gap: var(--gap-inline) var(--gap-groups);
		margin: 0;
		padding: 0;
		list-style: none;
	}
	.check {
		display: flex;
		align-items: center;
		gap: var(--gap-inline);
	}
</style>

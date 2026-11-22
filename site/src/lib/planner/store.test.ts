import { describe, expect, it } from 'vitest';
import type { PlannerData } from '../site-data.js';
import { newCharacter } from './state.js';
import {
	activeCharacter,
	createCharacter,
	deleteCharacter,
	duplicateCharacter,
	emptyStore,
	loadStore,
	renameCharacter,
	type StoreBackend
} from './store.js';

const data: PlannerData = {
	build: 7,
	format: 1,
	disciplines: [
		{
			key: 'Fire',
			slug: 'fire',
			name: 'Fire',
			icon: null,
			category: 'Magic',
			levelName: 'Pyromancy',
			slots: [],
			passives: []
		}
	],
	skills: ['Ember', 'Blaze', 'Spark'].map((name) => ({
		key: name,
		slug: name.toLowerCase(),
		name,
		icon: null,
		learned: true
	}))
};

function memory(): StoreBackend & { value: string | null; fail: boolean } {
	const backend = {
		value: null as string | null,
		fail: false,
		read: () => backend.value,
		write: (value: string) => {
			if (backend.fail) throw new Error('quota');
			backend.value = value;
		}
	};
	return backend;
}

describe('character store', () => {
	it('creates, renames, duplicates and deletes characters explicitly', () => {
		const backend = memory();
		let { store } = createCharacter(backend, emptyStore(), 'Mage', newCharacter(data));
		const mage = activeCharacter(store)!;
		store = renameCharacter(backend, store, mage.id, 'Pyromancer').store;
		store = duplicateCharacter(backend, store, mage.id, 'Pyromancer copy').store;
		expect(store.characters.map((character) => character.name)).toEqual([
			'Pyromancer',
			'Pyromancer copy'
		]);
		store = deleteCharacter(backend, store, activeCharacter(store)!.id).store;
		expect(store.active).toBeNull();
		expect(loadStore(backend).store).toEqual(store);
	});

	it('keeps the plan in memory and says so when a write fails', () => {
		const backend = memory();
		backend.fail = true;
		const result = createCharacter(backend, emptyStore(), 'Mage', newCharacter(data));
		expect(result.saved).toBe(false);
		expect(result.problem).toContain('could not save');
		expect(result.store.characters).toHaveLength(1);
		expect(backend.value).toBeNull();
	});

	it('reads a store of another version as empty and says why', () => {
		const backend = memory();
		backend.value = JSON.stringify({ version: 99, active: null, characters: [] });
		expect(loadStore(backend)).toEqual({
			store: emptyStore(),
			problem: 'The saved characters are from another planner version.'
		});
	});
});

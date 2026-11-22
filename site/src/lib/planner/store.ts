import type { PlannerState } from './state.js';

/** The store's format in local storage. Another version is read as no characters rather than misread. */
export const storeVersion = 1;
export const storeKey = 'bloodline-planner';

export interface Character {
	id: string;
	name: string;
	state: PlannerState;
}

export interface CharacterStore {
	version: number;
	active: string | null;
	characters: Character[];
}

/** Where the store reads and writes, so tests and a browser without storage can stand in for local storage. */
export interface StoreBackend {
	read(): string | null;
	write(value: string): void;
}

/** What an action did: the new store, and whether it was saved, with the reason when it was not. */
export interface StoreResult {
	store: CharacterStore;
	saved: boolean;
	problem?: string;
}

export const emptyStore = (): CharacterStore => ({
	version: storeVersion,
	active: null,
	characters: []
});

/** Reads the store. A missing, unreadable or other-version store reads as empty and says why. */
export function loadStore(backend: StoreBackend): { store: CharacterStore; problem?: string } {
	let text: string | null;
	try {
		text = backend.read();
	} catch {
		return {
			store: emptyStore(),
			problem: 'This browser does not let the planner read its saved characters.'
		};
	}
	if (text === null) return { store: emptyStore() };
	try {
		const parsed = JSON.parse(text) as CharacterStore;
		if (parsed.version !== storeVersion || !Array.isArray(parsed.characters))
			return {
				store: emptyStore(),
				problem: 'The saved characters are from another planner version.'
			};
		return { store: parsed };
	} catch {
		return { store: emptyStore(), problem: 'The saved characters could not be read.' };
	}
}

function save(backend: StoreBackend, store: CharacterStore): StoreResult {
	try {
		backend.write(JSON.stringify(store));
		return { store, saved: true };
	} catch {
		return {
			store,
			saved: false,
			problem: 'The planner could not save. The plan stays open here, and its link still works.'
		};
	}
}

let counter = 0;
const newId = () => `${Date.now().toString(36)}-${(counter++).toString(36)}`;

/** Saves a build as a new character and makes it the active one. */
export function createCharacter(
	backend: StoreBackend,
	store: CharacterStore,
	name: string,
	state: PlannerState
): StoreResult {
	const character = { id: newId(), name, state };
	return save(backend, {
		...store,
		active: character.id,
		characters: [...store.characters, character]
	});
}

export function renameCharacter(
	backend: StoreBackend,
	store: CharacterStore,
	id: string,
	name: string
): StoreResult {
	return save(backend, {
		...store,
		characters: store.characters.map((character) =>
			character.id === id ? { ...character, name } : character
		)
	});
}

/** Copies a character under a new name and makes the copy active. */
export function duplicateCharacter(
	backend: StoreBackend,
	store: CharacterStore,
	id: string,
	name: string
): StoreResult {
	const source = store.characters.find((character) => character.id === id);
	if (!source) return { store, saved: false, problem: 'That character no longer exists.' };
	return createCharacter(backend, store, name, structuredClone(source.state));
}

export function selectCharacter(
	backend: StoreBackend,
	store: CharacterStore,
	id: string | null
): StoreResult {
	if (id !== null && !store.characters.some((character) => character.id === id))
		return { store, saved: false, problem: 'That character no longer exists.' };
	return save(backend, { ...store, active: id });
}

export function deleteCharacter(
	backend: StoreBackend,
	store: CharacterStore,
	id: string
): StoreResult {
	return save(backend, {
		...store,
		active: store.active === id ? null : store.active,
		characters: store.characters.filter((character) => character.id !== id)
	});
}

/** Writes a changed build to its character. A preview from a link changes no character until it is saved. */
export function updateCharacter(
	backend: StoreBackend,
	store: CharacterStore,
	id: string,
	state: PlannerState
): StoreResult {
	return save(backend, {
		...store,
		characters: store.characters.map((character) =>
			character.id === id ? { ...character, state } : character
		)
	});
}

/** The active character, or null when none is chosen or it was deleted. */
export function activeCharacter(store: CharacterStore): Character | null {
	return store.characters.find((character) => character.id === store.active) ?? null;
}

/** Local storage as a backend, or one that always fails where the browser has none. */
export function localBackend(): StoreBackend {
	return {
		read: () => globalThis.localStorage.getItem(storeKey),
		write: (value) => globalThis.localStorage.setItem(storeKey, value)
	};
}

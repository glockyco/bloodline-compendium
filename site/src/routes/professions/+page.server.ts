import { readProfessions } from '#lib/server/data.js';

export async function load() {
	return { index: await readProfessions() };
}

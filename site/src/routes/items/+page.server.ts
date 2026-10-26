import { readIndex } from '#lib/server/data.js';
export async function load() {
	return { index: await readIndex() };
}

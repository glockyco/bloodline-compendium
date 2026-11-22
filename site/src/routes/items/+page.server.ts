import { readIndex } from '#lib/server/data.js';
export async function load() {
	// The list has its own name search, which the layout reads to leave out the header's on small screens.
	return { index: await readIndex(), ownSearch: true };
}

import { readBuild } from '#lib/server/data.js';
export async function load() {
	const build = await readBuild();
	return { release: build.release };
}

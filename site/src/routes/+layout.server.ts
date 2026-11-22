import { readBuild, readKinds, readLinks } from '#lib/server/data.js';
export async function load() {
	const [build, kinds, links] = await Promise.all([readBuild(), readKinds(), readLinks()]);
	return { release: build.release, kinds, links };
}

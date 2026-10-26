import { readBuild, readKinds } from '#lib/server/data.js';
export async function load() {
	const [build, kinds] = await Promise.all([readBuild(), readKinds()]);
	return { release: build.release, kinds };
}

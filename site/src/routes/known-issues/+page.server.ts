import { readBuild, readIssues } from '#lib/server/data.js';
export async function load() {
	const [issues, build] = await Promise.all([readIssues(), readBuild()]);
	return { issues, buildId: build.buildId };
}

import { readBuild } from '#lib/server/data.js';
export const prerender = true;
export async function GET() {
	const { commit, dirty, appId, buildId, depots } = await readBuild();
	return new Response(JSON.stringify({ commit, dirty, appId, buildId, depots }, null, 2) + '\n', {
		headers: { 'content-type': 'application/json' }
	});
}

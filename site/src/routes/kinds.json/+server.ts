import { readKinds } from '#lib/server/data.js';
import { json } from '@sveltejs/kit';
export const prerender = true;
export async function GET() {
	return json(await readKinds());
}

import { attributes } from '../rules/attributes.js';
import type { PlannerData } from '../site-data.js';
import { hotbarSlots, type PlannerState } from './state.js';

/** The link format. A link of another version decodes to an error rather than a wrong build. */
export const linkVersion = 'v1';
/** A whole link stays within this many characters, inside Discord's 2,000 with room for a comment. */
export const maxLinkLength = 1500;
/** The decoder refuses longer data and data that expands past this many bytes before it parses anything. */
export const maxEncodedLength = 2000;
export const maxExpandedBytes = 16 * 1024;

/**
 * A key's digest in a link: FNV-1a over its UTF-16 units, in base 36. Short digests keep links short, and
 * `digestIndex` fails publication when two keys of the payload share one.
 */
export function digest(key: string): string {
	let hash = 0x811c9dc5;
	for (let index = 0; index < key.length; index++) {
		hash ^= key.charCodeAt(index);
		hash = Math.imul(hash, 0x01000193) >>> 0;
	}
	return hash.toString(36);
}

/** A key the current payload lacks, kept by its digest so it stays visible and survives another save. */
export const unresolvedPrefix = 'unresolved:';

/** Every key of the payload by its digest. Throws when two keys share a digest, which would make links ambiguous. */
export function digestIndex(data: PlannerData): Map<string, string> {
	const keys = new Set([
		...data.disciplines.map((discipline) => discipline.key),
		...data.disciplines.flatMap((discipline) => discipline.passives.map((passive) => passive.key)),
		...data.skills.map((skill) => skill.key),
		...data.disciplines.flatMap((discipline) => discipline.slots.map((slot) => slot.skill))
	]);
	const index = new Map<string, string>();
	for (const key of keys) {
		const short = digest(key);
		const other = index.get(short);
		if (other !== undefined && other !== key)
			throw new Error(`Planner keys ${other} and ${key} share the link digest ${short}.`);
		index.set(short, key);
	}
	return index;
}

/** The compact form a link carries, with keys as digests and defaults left out. */
interface Compact {
	b: number;
	l: number;
	d?: [string, number][];
	a: number[];
	s?: string[];
	k?: string[];
	p?: [string, number][];
	m?: number;
	h?: (string | 0)[];
}

const short = (key: string) =>
	key.startsWith(unresolvedPrefix) ? key.slice(unresolvedPrefix.length) : digest(key);

function compact(state: PlannerState): Compact {
	const result: Compact = {
		b: state.build,
		l: state.level,
		a: attributes.map((attribute) => state.attributes[attribute])
	};
	const disciplines = Object.entries(state.disciplines).filter(([, level]) => level !== 1);
	if (disciplines.length) result.d = disciplines.map(([key, level]) => [short(key), level]);
	if (state.skills.length) result.s = state.skills.map(short);
	if (state.learned.length) result.k = state.learned.map(short);
	const passives = Object.entries(state.passives).filter(([, ranks]) => ranks > 0);
	if (passives.length) result.p = passives.map(([key, ranks]) => [short(key), ranks]);
	if (state.bloodMagicPoints !== null) result.m = state.bloodMagicPoints;
	if (state.hotbar.some((slot) => slot !== null))
		result.h = state.hotbar.map((slot) => (slot === null ? 0 : short(slot)));
	return result;
}

async function pipe(
	bytes: Uint8Array<ArrayBuffer>,
	stream: CompressionStream | DecompressionStream,
	limit: number
) {
	const reader = new Blob([bytes]).stream().pipeThrough(stream).getReader();
	const chunks: Uint8Array[] = [];
	let total = 0;
	for (;;) {
		const { done, value } = await reader.read();
		if (done) break;
		total += value.length;
		if (total > limit) {
			await reader.cancel();
			throw new LinkError('size', 'The build expands past the size a link may hold.');
		}
		chunks.push(value);
	}
	const out = new Uint8Array(total);
	let offset = 0;
	for (const chunk of chunks) {
		out.set(chunk, offset);
		offset += chunk.length;
	}
	return out;
}

function toBase64Url(bytes: Uint8Array): string {
	let binary = '';
	for (const byte of bytes) binary += String.fromCharCode(byte);
	return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function fromBase64Url(text: string): Uint8Array<ArrayBuffer> {
	if (!/^[A-Za-z0-9_-]*$/.test(text))
		throw new LinkError('malformed', 'The link holds characters a build never has.');
	const binary = atob(text.replace(/-/g, '+').replace(/_/g, '/'));
	return Uint8Array.from(binary, (char) => char.charCodeAt(0));
}

/** Why a link did not decode: another format version, too much data, or data no planner wrote. */
export class LinkError extends Error {
	constructor(
		readonly kind: 'version' | 'size' | 'malformed',
		message: string
	) {
		super(message);
	}
}

/** The value of a build link's `build` parameter, such as `v1.<data>`. Throws when the link would be too long. */
export async function encodeBuild(state: PlannerState, origin = ''): Promise<string> {
	const json = new TextEncoder().encode(JSON.stringify(compact(state)));
	const value = `${linkVersion}.${toBase64Url(await pipe(json, new CompressionStream('deflate-raw'), Infinity))}`;
	if (`${origin}/planner/?build=${value}`.length > maxLinkLength)
		throw new LinkError('size', `The build link would be longer than ${maxLinkLength} characters.`);
	return value;
}

const isCount = (value: unknown, min = 0): value is number =>
	typeof value === 'number' && Number.isInteger(value) && value >= min;
const isPairs = (value: unknown): value is [string, number][] =>
	Array.isArray(value) &&
	value.every(
		(pair) =>
			Array.isArray(pair) && pair.length === 2 && typeof pair[0] === 'string' && isCount(pair[1])
	);
const isDigests = (value: unknown): value is string[] =>
	Array.isArray(value) && value.every((entry) => typeof entry === 'string');

/**
 * Decodes a build link's value against the current payload. A digest the payload lacks stays in the build as an
 * unresolved entry rather than being dropped or matched to another key, and the result lists each one.
 */
export async function decodeBuild(
	value: string,
	data: PlannerData
): Promise<{ state: PlannerState; unresolved: string[] }> {
	if (value.length > maxEncodedLength)
		throw new LinkError('size', 'The build link is longer than a planner writes.');
	const [version, payload, ...rest] = value.split('.');
	if (version !== linkVersion)
		throw new LinkError(
			'version',
			`This planner reads ${linkVersion} links, not ${version || 'this one'}.`
		);
	if (payload === undefined || rest.length)
		throw new LinkError('malformed', 'The link is not a build.');
	let parsed: unknown;
	try {
		const bytes = await pipe(
			fromBase64Url(payload),
			new DecompressionStream('deflate-raw'),
			maxExpandedBytes
		);
		parsed = JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(bytes));
	} catch (error) {
		if (error instanceof LinkError) throw error;
		throw new LinkError('malformed', 'The link is not a build.');
	}
	const c = parsed as Partial<Compact>;
	if (
		typeof parsed !== 'object' ||
		parsed === null ||
		!isCount(c.b) ||
		!isCount(c.l, 1) ||
		!Array.isArray(c.a) ||
		c.a.length !== attributes.length ||
		!c.a.every((level) => isCount(level, 1)) ||
		(c.d !== undefined && !isPairs(c.d)) ||
		(c.p !== undefined && !isPairs(c.p)) ||
		(c.s !== undefined && !isDigests(c.s)) ||
		(c.k !== undefined && !isDigests(c.k)) ||
		(c.m !== undefined && !isCount(c.m)) ||
		(c.h !== undefined &&
			(!Array.isArray(c.h) ||
				c.h.length !== hotbarSlots ||
				!c.h.every((slot) => slot === 0 || typeof slot === 'string')))
	)
		throw new LinkError('malformed', 'The link is not a build.');
	const index = digestIndex(data);
	const unresolved: string[] = [];
	const key = (short: string) => {
		const found = index.get(short);
		if (found !== undefined) return found;
		unresolved.push(short);
		return unresolvedPrefix + short;
	};
	const state: PlannerState = {
		build: c.b,
		level: c.l,
		disciplines: Object.fromEntries((c.d ?? []).map(([short, level]) => [key(short), level])),
		attributes: Object.fromEntries(
			attributes.map((attribute, position) => [attribute, c.a![position]])
		) as PlannerState['attributes'],
		skills: (c.s ?? []).map(key),
		learned: (c.k ?? []).map(key),
		passives: Object.fromEntries((c.p ?? []).map(([short, ranks]) => [key(short), ranks])),
		bloodMagicPoints: c.m ?? null,
		hotbar: c.h ? c.h.map((slot) => (slot === 0 ? null : key(slot))) : Array(hotbarSlots).fill(null)
	};
	return { state, unresolved: [...new Set(unresolved)] };
}

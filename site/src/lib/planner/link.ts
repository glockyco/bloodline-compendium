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

/**
 * The compact form a link carries, with keys as digests and defaults left out. Each section of the state is one member,
 * and a member this planner does not know, written by a later planner, is kept in `later` and written back unchanged.
 */
interface Compact {
	b: number;
	/** Attributes: the character level and each attribute's points, in the order of `attributes`. */
	a: { l: number; p: number[] };
	/** Skills: discipline levels, unlocked and learned skills, passive ranks and Blood Magic points. */
	s?: {
		d?: [string, number][];
		u?: string[];
		k?: string[];
		p?: [string, number][];
		m?: number;
	};
	/** Skill Bars: the skill bar's and the Offhand Spells Bar's ten slots, 0 for an empty one. */
	r?: { m?: (string | 0)[]; o?: (string | 0)[] };
}

const knownSections = new Set(['b', 'a', 's', 'r']);

const short = (key: string) =>
	key.startsWith(unresolvedPrefix) ? key.slice(unresolvedPrefix.length) : digest(key);

function compact(state: PlannerState): Compact & Record<string, unknown> {
	const result: Compact & Record<string, unknown> = {
		...state.later,
		b: state.build,
		a: {
			l: state.attributes.level,
			p: attributes.map((attribute) => state.attributes.points[attribute])
		}
	};
	const skills: NonNullable<Compact['s']> = {};
	const disciplines = Object.entries(state.skills.disciplines).filter(([, level]) => level !== 1);
	if (disciplines.length) skills.d = disciplines.map(([key, level]) => [short(key), level]);
	if (state.skills.unlocked.length) skills.u = state.skills.unlocked.map(short);
	if (state.skills.learned.length) skills.k = state.skills.learned.map(short);
	const passives = Object.entries(state.skills.passives).filter(([, ranks]) => ranks > 0);
	if (passives.length) skills.p = passives.map(([key, ranks]) => [short(key), ranks]);
	if (state.skills.bloodMagicPoints !== null) skills.m = state.skills.bloodMagicPoints;
	if (Object.keys(skills).length) result.s = skills;
	const bars: NonNullable<Compact['r']> = {};
	const written = (bar: (string | null)[]) => bar.map((slot) => (slot === null ? 0 : short(slot)));
	if (state.skillBars.main.some((slot) => slot !== null)) bars.m = written(state.skillBars.main);
	if (state.skillBars.offhand.some((slot) => slot !== null))
		bars.o = written(state.skillBars.offhand);
	if (Object.keys(bars).length) result.r = bars;
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
	const c = parsed as Partial<Compact> & Record<string, unknown>;
	const isObject = (value: unknown): value is Record<string, unknown> =>
		typeof value === 'object' && value !== null && !Array.isArray(value);
	const skills = c.s ?? {};
	const bars = c.r ?? {};
	if (
		!isObject(parsed) ||
		!isCount(c.b) ||
		!isObject(c.a) ||
		!isCount(c.a.l, 1) ||
		!Array.isArray(c.a.p) ||
		c.a.p.length !== attributes.length ||
		!c.a.p.every((level) => isCount(level, 1)) ||
		!isObject(skills) ||
		(skills.d !== undefined && !isPairs(skills.d)) ||
		(skills.p !== undefined && !isPairs(skills.p)) ||
		(skills.u !== undefined && !isDigests(skills.u)) ||
		(skills.k !== undefined && !isDigests(skills.k)) ||
		(skills.m !== undefined && !isCount(skills.m)) ||
		!isObject(bars) ||
		![bars.m, bars.o].every(
			(bar) =>
				bar === undefined ||
				(Array.isArray(bar) &&
					bar.length === hotbarSlots &&
					bar.every((slot) => slot === 0 || typeof slot === 'string'))
		)
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
	const points = c.a.p;
	const read = (slot: string | 0) => (slot === 0 ? null : key(slot));
	const state: PlannerState = {
		build: c.b,
		attributes: {
			level: c.a.l,
			points: Object.fromEntries(
				attributes.map((attribute, position) => [attribute, points[position]])
			) as PlannerState['attributes']['points']
		},
		skills: {
			disciplines: Object.fromEntries(
				(skills.d ?? []).map(([short, level]) => [key(short), level])
			),
			unlocked: (skills.u ?? []).map(key),
			learned: (skills.k ?? []).map(key),
			passives: Object.fromEntries((skills.p ?? []).map(([short, ranks]) => [key(short), ranks])),
			bloodMagicPoints: skills.m ?? null
		},
		skillBars: {
			main: bars.m ? bars.m.map(read) : Array(hotbarSlots).fill(null),
			offhand: bars.o ? bars.o.map(read) : Array(hotbarSlots).fill(null)
		},
		later: Object.fromEntries(Object.entries(c).filter(([name]) => !knownSections.has(name)))
	};
	return { state, unresolved: [...new Set(unresolved)] };
}

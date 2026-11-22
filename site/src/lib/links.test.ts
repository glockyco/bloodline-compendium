import { expect, it } from 'vitest';
import { linkParts, plainText } from './links.js';

const coat = {
	kind: 'item' as const,
	key: '/Invented/Items/Coat',
	name: 'Mosswoven Coat',
	slug: 'mosswoven-coat',
	icon: null
};

it('splits text into runs and links each target the catalog lists', () => {
	expect(
		linkParts('The [coat](item:mosswoven-coat) is warm.', { 'item:mosswoven-coat': coat })
	).toEqual([
		{ text: 'The ', entity: null },
		{ text: 'coat', entity: coat },
		{ text: ' is warm.', entity: null }
	]);
	expect(plainText('Lower the [Blood Levels](mechanic:blood-levels).')).toBe(
		'Lower the Blood Levels.'
	);
});

it('fails on a target the catalog does not list', () => {
	expect(() => linkParts('Lower the [Blood Levels](mechanic:blod-levels).', {})).toThrow(
		'mechanic:blod-levels'
	);
});

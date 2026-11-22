import { expect, it } from 'vitest';
import { linkParts } from './links.js';

const coat = {
	kind: 'item' as const,
	key: '/Invented/Items/Coat',
	name: 'Mosswoven Coat',
	slug: 'mosswoven-coat',
	icon: null
};

it('splits text into runs and links each resolved target', () => {
	expect(
		linkParts('The [coat](item:mosswoven-coat) and [Blood Levels](mechanic:blood-levels) differ.', {
			'item:mosswoven-coat': coat
		})
	).toEqual([
		{ text: 'The ', entity: null },
		{ text: 'coat', entity: coat },
		{ text: ' and ', entity: null },
		{ text: 'Blood Levels', entity: null },
		{ text: ' differ.', entity: null }
	]);
});

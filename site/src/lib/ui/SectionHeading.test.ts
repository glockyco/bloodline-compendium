import { render } from 'svelte/server';
import { createRawSnippet } from 'svelte';
import { expect, it } from 'vitest';
import SectionHeading from './SectionHeading.svelte';
import Section from './Section.svelte';

it('keeps the count with the last heading word when the title wraps', () => {
	const body = render(SectionHeading, {
		props: { id: 'bounties', title: 'Obtained from Bounty Notes', count: 1 }
	}).body.replace(/<!--[\s\S]*?-->/g, '');
	expect(body).toMatch(
		/Obtained from Bounty <span class="heading-end[^"]*">Notes(?:&nbsp;|\u00a0)<span class="count/
	);
});

it('keeps a one-word title with its count and omits grouping without a count', () => {
	const counted = render(SectionHeading, { props: { id: 'bugs', title: 'Bugs', count: 2 } }).body;
	expect(counted).toMatch(/heading-end[^>]*>Bugs/);
	const plain = render(SectionHeading, { props: { id: 'details', title: 'Details' } }).body;
	expect(plain).not.toContain('heading-end');
});

it('keeps an item section count beside its final heading word', () => {
	const body = render(Section, {
		props: {
			id: 'bounties',
			title: 'Obtained from Bounty Notes',
			count: 1,
			children: createRawSnippet(() => ({ render: () => '<p>Content</p>' }))
		}
	}).body.replace(/<!--[\s\S]*?-->/g, '');
	expect(body).toMatch(
		/Obtained from Bounty <span class="heading-end[^"]*">Notes(?:&nbsp;|\u00a0)<span class="count/
	);
});

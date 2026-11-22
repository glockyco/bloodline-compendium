import { render } from 'svelte/server';
import { createRawSnippet } from 'svelte';
import { expect, it } from 'vitest';
import Section from './Section.svelte';

const content = createRawSnippet(() => ({ render: () => '<p>Content</p>' }));

it('keeps an item section count beside its final heading word', () => {
	const body = render(Section, {
		props: {
			id: 'bounties',
			title: 'Obtained from Bounty Notes',
			count: 1,
			children: content
		}
	}).body.replace(/<!--[\s\S]*?-->/g, '');
	expect(body).toMatch(
		/Obtained from Bounty <span class="heading-end[^"]*">Notes(?:&nbsp;|\u00a0)<span class="count/
	);
});

it('keeps a one-word title with its count and omits grouping without a count', () => {
	const counted = render(Section, {
		props: { id: 'bugs', title: 'Bugs', count: 2, children: content }
	}).body;
	expect(counted).toMatch(/heading-end[^>]*>Bugs/);
	const plain = render(Section, {
		props: { id: 'details', title: 'Details', children: content }
	}).body;
	expect(plain).not.toContain('heading-end');
});

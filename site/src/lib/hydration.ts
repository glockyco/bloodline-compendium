let hydrated = false;

/**
 * Whether the app has hydrated its first page. Components that defer work after hydration render the same HTML as
 * the server on the first page, and skip that work at once on later client navigations, where nothing hydrates.
 */
export function isHydrated(): boolean {
	return hydrated;
}

export function markHydrated(): void {
	hydrated = true;
}

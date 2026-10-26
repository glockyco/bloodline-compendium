<script lang="ts">
	import type { SiteRelease } from './site-data.js';
	let { release }: { release: SiteRelease } = $props();
	const dateFormat = new Intl.DateTimeFormat('en', {
		year: 'numeric',
		month: 'long',
		day: 'numeric',
		timeZone: 'UTC'
	});
	function date(value: string) {
		return dateFormat.format(new Date(value));
	}
</script>

<div class="release-info">
	<span>{release.title}</span>
	<span>Patched <time datetime={release.patchDate}>{date(release.patchDate)}</time></span>
	<span>Data from <time datetime={release.dataDate}>{date(release.dataDate)}</time></span>
	<a href={release.patchNotesUrl} rel="external">Patch Notes</a>
</div>

<script lang="ts">
	import Section from '../ui/Section.svelte';
	/**
	 * How to spawn the item with the game's console. The command sits behind a reveal, so a player who does not want
	 * the shortcut never reads it, and a copy button spares typing the loot table's name.
	 */
	let {
		command,
		id
	}: {
		/** The console line, such as "AddItem Bread", or null when the console cannot give the item. */
		command: string | null;
		id: string;
	} = $props();
	let notice = $state('');
	let copied = $state(false);
	async function copy() {
		try {
			await navigator.clipboard.writeText(command!);
			copied = true;
			notice = '';
		} catch {
			notice = 'The browser did not allow copying. Select the command instead.';
		}
	}
</script>

{#snippet body()}
	<p>
		In a loaded game with no menu open, press F1, or the key you set for the console. Type or paste
		this command and press Enter:
	</p>
	<div class="command">
		<code>{command}</code><button type="button" onclick={copy}
			>{copied ? 'Copied' : 'Copy'}<span class="sr-only" aria-live="polite"
				>{copied ? ' to the clipboard' : ''}</span
			></button
		>
	</div>
	<p class="muted">
		Add a number for more, up to 100, such as <code>{command} 5</code>. The console closes after
		every command, so if nothing reaches your inventory, open it again and check the name.
	</p>
	<p class="muted" aria-live="polite">{notice}</p>
{/snippet}

<Section {id} title="Console Command">
	<div class="surface console">
		{#if !command}<p>The console's <code>AddItem</code> command cannot give this item.</p>
		{:else}<p>Spawning an item with the console is a cheat that skips how the game hands it out.</p>
			<details>
				<summary>Show the command</summary>
				{@render body()}
			</details>{/if}
	</div>
</Section>

<style>
	.console {
		display: grid;
		gap: var(--gap-block);
	}
	.console p {
		margin: 0;
	}
	details[open] {
		display: grid;
		gap: var(--gap-block);
	}
	summary {
		cursor: pointer;
		color: var(--accent);
	}
	.muted:empty {
		display: none;
	}
	.command {
		display: flex;
		align-items: center;
		gap: var(--gap-inline);
		margin-block: var(--gap-line);
	}
	/* The command scrolls inside its box rather than pushing the button onto a line of its own. */
	.command code {
		flex: 0 1 auto;
		min-width: 0;
		overflow-x: auto;
		padding: var(--inset-cell) var(--inset);
		border: var(--border-width) solid var(--border-control);
		border-radius: var(--radius-1);
		background: var(--surface-slot);
		white-space: nowrap;
	}
	.command button {
		flex: none;
	}
	code {
		white-space: nowrap;
	}
	.muted {
		color: var(--text-muted);
	}
</style>

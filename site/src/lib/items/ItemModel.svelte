<script lang="ts">
	import type { ItemRender, RenderBody } from '../site-data.js';
	let { name, renders }: { name: string; renders: ItemRender[] } = $props();
	const bodyNames: Record<RenderBody, string> = { male: 'Male', female: 'Female' };
	let shown = $state<RenderBody>('male');
	// The male body comes first, as character creation starts with it, and armor without body meshes has one render.
	const current = $derived(renders.find((render) => render.body === shown) ?? renders[0]);
</script>

{#if current}<figure class="item-model">
		<img
			src={`/renders/${current.hash}-320.webp`}
			srcset={`/renders/${current.hash}-320.webp 320w, /renders/${current.hash}-640.webp 640w`}
			sizes="(width < 40rem) 100vw, 40rem"
			alt={renders.length > 1 ? `${name} on the ${current.body} body` : name}
			width={current.width}
			height={current.height}
		/>
		{#if renders.length > 1}<div class="bodies" role="group" aria-label="Body">
				{#each renders as render (render.body)}<button
						type="button"
						aria-pressed={render.body === current.body}
						onclick={() => (shown = render.body)}>{bodyNames[render.body]}</button
					>{/each}
			</div>{/if}
	</figure>{/if}

<style>
	.item-model {
		display: grid;
		gap: var(--gap-block);
		justify-items: center;
		margin: 0;
		padding: var(--inset);
		border: var(--border-width) solid var(--border);
		border-radius: var(--radius-1);
		background: var(--surface-panel);
	}
	img {
		width: auto;
		max-width: 100%;
		height: auto;
		max-height: var(--size-render);
		object-fit: contain;
	}
	.bodies {
		display: flex;
		gap: var(--gap-inline);
	}
	button {
		padding: var(--inset-cell);
		border: var(--border-width) solid var(--border);
		border-radius: var(--radius-1);
		background: transparent;
		color: var(--text-muted);
		font: inherit;
		cursor: pointer;
	}
	button[aria-pressed='true'] {
		border-color: var(--accent);
		color: var(--text);
	}
</style>

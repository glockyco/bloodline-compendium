import { defineConfig } from '@playwright/test';

// Worktrees that run end-to-end tests at the same time each set their own E2E_PORT, because a run reuses any
// server already listening on its port and would otherwise test another worktree's build.
const port = Number(process.env.E2E_PORT ?? 8796);

export default defineConfig({
	testDir: './tests',
	testMatch: '**/*.e2e.ts',
	use: { baseURL: `http://127.0.0.1:${port}`, browserName: 'chromium' },
	webServer: {
		command: `bun run wrangler dev --assets build --port ${port} --local`,
		url: `http://127.0.0.1:${port}`,
		reuseExistingServer: !process.env.CI
	},
	reporter: 'list'
});

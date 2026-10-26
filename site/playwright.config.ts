import { defineConfig } from '@playwright/test';
export default defineConfig({
	testDir: './tests',
	testMatch: '**/*.e2e.ts',
	use: { baseURL: 'http://127.0.0.1:8796', browserName: 'chromium' },
	webServer: {
		command: 'bun run wrangler dev --port 8796 --local',
		url: 'http://127.0.0.1:8796',
		reuseExistingServer: !process.env.CI
	},
	reporter: 'list'
});

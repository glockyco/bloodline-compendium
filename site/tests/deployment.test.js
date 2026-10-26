import { expect, it } from 'vitest';
import { execFileSync } from 'node:child_process';
import { createServer } from 'node:http';
import { mkdtemp, mkdir, writeFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import {
	assertDeployment,
	requiredFiles,
	securityHeaders,
	smokeDeployment
} from '../../scripts/deploy.mjs';

async function stage(action) {
	const repo = await mkdtemp(join(tmpdir(), 'bloodline-deploy-'));
	try {
		execFileSync('git', ['init', '-q', repo]);
		await writeFile(join(repo, '.gitignore'), 'stage/\n');
		execFileSync('git', ['add', '.'], { cwd: repo });
		execFileSync(
			'git',
			['-c', 'user.name=Test', '-c', 'user.email=test@example.invalid', 'commit', '-qm', 'fixture'],
			{ cwd: repo }
		);
		const commit = execFileSync('git', ['rev-parse', 'HEAD'], {
			cwd: repo,
			encoding: 'utf8'
		}).trim();
		const directory = join(repo, 'stage');
		for (const file of [...requiredFiles, 'items/invented/index.html']) {
			await mkdir(dirname(join(directory, file)), { recursive: true });
			await writeFile(
				join(directory, file),
				'<title>Invented reference</title><h1>Invented item</h1>'
			);
		}
		const build = { commit, dirty: false, buildId: 90000001 };
		await writeFile(join(directory, 'build-info.json'), JSON.stringify(build));
		await action({ repo, directory, build });
	} finally {
		await rm(repo, { recursive: true, force: true });
	}
}

it('accepts a complete stage and rejects absent assets, both hosting limits and stale or dirty provenance', async () => {
	await stage(async ({ repo, directory, build }) => {
		expect((await assertDeployment(directory, repo)).pages.map((page) => page.path)).toEqual([
			'/',
			'/items/',
			'/items/invented/',
			'/coverage/'
		]);
		await expect(assertDeployment(directory, repo, { maxFiles: 1 })).rejects.toThrow(
			'Asset limits exceeded'
		);
		await expect(assertDeployment(directory, repo, { maxBytes: 1 })).rejects.toThrow(
			'Asset limits exceeded'
		);
		await rm(join(directory, 'pagefind/pagefind.js'));
		await expect(assertDeployment(directory, repo)).rejects.toThrow('pagefind/pagefind.js');
		await writeFile(join(directory, 'pagefind/pagefind.js'), 'search');
		await writeFile(
			join(directory, 'build-info.json'),
			JSON.stringify({ ...build, commit: 'old' })
		);
		await expect(assertDeployment(directory, repo)).rejects.toThrow('differs from HEAD');
		await writeFile(join(directory, 'build-info.json'), JSON.stringify({ ...build, dirty: true }));
		await expect(assertDeployment(directory, repo)).rejects.toThrow('dirty working tree');
		await writeFile(join(directory, 'build-info.json'), JSON.stringify(build));
		await writeFile(join(repo, 'uncommitted.txt'), 'changed');
		await expect(assertDeployment(directory, repo)).rejects.toThrow('clean working tree');
	});
});

it('smokes real HTTP responses, retries old builds and rejects broken content, search, status and security headers', async () => {
	const build = { commit: 'invented-commit', dirty: false, buildId: 90000001 };
	const pages = ['/', '/items/', '/items/invented/', '/coverage/'].map((path) => ({
		path,
		title: '<title>Invented reference</title>',
		heading: '<h1>Invented item</h1>'
	}));
	let fault = '';
	let stale = 1;
	const requests = [];
	const server = createServer((request, response) => {
		const url = new URL(request.url, 'http://localhost');
		requests.push({
			path: url.pathname,
			fresh: url.searchParams.has('smoke'),
			cache: request.headers['cache-control']
		});
		for (const [name, value] of Object.entries(securityHeaders))
			if (fault !== name) response.setHeader(name, value);
		if (url.pathname === '/build-info.json') {
			response.end(
				JSON.stringify(stale-- > 0 || fault === 'identity' ? { ...build, buildId: 1 } : build)
			);
		} else if (url.pathname === '/pagefind/pagefind.js') {
			response.setHeader(
				'content-type',
				fault === 'search' ? 'text/html' : 'application/javascript'
			);
			response.end('export const search = true;');
		} else if (url.pathname.startsWith('/missing-')) {
			response.statusCode = fault === '404' ? 200 : 404;
			response.end('Page not found');
		} else {
			response.statusCode = fault === 'status' ? 503 : 200;
			response.end(fault === 'content' ? '<h1>Wrong page</h1>' : pages[0].title + pages[0].heading);
		}
	});
	await new Promise((done) => server.listen(0, '127.0.0.1', done));
	const origin = 'http://127.0.0.1:' + server.address().port;
	try {
		await smokeDeployment(origin, { build, pages }, { timeout: 1000, interval: 1 });
		expect(
			requests.filter((request) => request.path === '/build-info.json').length
		).toBeGreaterThan(1);
		expect(requests.every((request) => request.fresh && request.cache === 'no-cache')).toBe(true);
		for (fault of [
			'identity',
			'content',
			'status',
			'search',
			'404',
			...Object.keys(securityHeaders)
		]) {
			await expect(
				smokeDeployment(origin, { build, pages }, { timeout: 40, interval: 1 })
			).rejects.toThrow();
		}
	} finally {
		await new Promise((done) => server.close(done));
	}
});

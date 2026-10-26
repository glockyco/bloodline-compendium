import { expect, it } from 'vitest';
import { execFileSync } from 'node:child_process';
import { mkdtemp, mkdir, writeFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { trackedGameFiles } from '../../scripts/guard.mjs';
import { assetGate } from '../../scripts/asset-gate.mjs';
import { assertProductionBuild } from '../../scripts/provenance.mjs';
async function temporary(action) {
	const dir = await mkdtemp(join(tmpdir(), 'bloodline-test-'));
	try {
		await action(dir);
	} finally {
		await rm(dir, { recursive: true, force: true });
	}
}
it('rejects planted tracked files and names each one, including case variants', async () => {
	await temporary(async (dir) => {
		execFileSync('git', ['init', '-q', dir]);
		await mkdir(join(dir, 'artifacts'));
		await writeFile(join(dir, 'artifacts/cache.json'), '{}');
		await writeFile(join(dir, 'planted.PAK'), 'not game data');
		await writeFile(join(dir, 'bloodline.local.json'), '{}');
		await writeFile(join(dir, 'allowed.json'), '{}');
		execFileSync('git', ['add', '.'], { cwd: dir });
		expect(trackedGameFiles(dir)).toEqual([
			'artifacts/cache.json',
			'bloodline.local.json',
			'planted.PAK'
		]);
		expect(() =>
			execFileSync(process.execPath, ['../scripts/guard.mjs', dir], { stdio: 'pipe' })
		).toThrow();
	});
});
it('counts nested files after indexing and rejects both hosting limits', async () => {
	await temporary(async (dir) => {
		await mkdir(join(dir, 'pagefind'));
		await writeFile(join(dir, 'index.html'), 'abc');
		await writeFile(join(dir, 'pagefind/index.js'), '12345');
		expect((await assetGate(dir)).counts).toEqual({ '(root)': 1, pagefind: 1 });
		await expect(assetGate(dir, { maxFiles: 1 })).rejects.toThrow('pagefind: 1');
		await expect(assetGate(dir, { maxBytes: 4 })).rejects.toThrow('pagefind/index.js: 5 bytes');
	});
});
it('refuses dirty trees, dirty data and mismatched extraction commits', async () => {
	await temporary(async (dir) => {
		execFileSync('git', ['init', '-q', dir]);
		await writeFile(join(dir, 'source.txt'), 'clean');
		execFileSync('git', ['add', '.'], { cwd: dir });
		execFileSync(
			'git',
			['-c', 'user.name=Test', '-c', 'user.email=test@example.invalid', 'commit', '-qm', 'fixture'],
			{ cwd: dir }
		);
		const commit = execFileSync('git', ['rev-parse', 'HEAD'], {
			cwd: dir,
			encoding: 'utf8'
		}).trim();
		expect(() => assertProductionBuild({ commit, dirty: false }, dir)).not.toThrow();
		expect(() => assertProductionBuild({ commit, dirty: true }, dir)).toThrow('dirty working tree');
		expect(() => assertProductionBuild({ commit: 'different', dirty: false }, dir)).toThrow(
			'differs from HEAD'
		);
		await writeFile(join(dir, 'source.txt'), 'changed');
		expect(() => assertProductionBuild({ commit, dirty: false }, dir)).toThrow('source.txt');
	});
});

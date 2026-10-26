import { spawnSync } from "node:child_process";
import { readFile, readdir, stat } from "node:fs/promises";
import { join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { isDeepStrictEqual } from "node:util";
import { assetGate } from "./asset-gate.mjs";
import { assertProductionBuild } from "./provenance.mjs";
import { publicationGate } from "./publication-gate.mjs";

export const securityHeaders = {
  "x-content-type-options": "nosniff",
  "referrer-policy": "strict-origin-when-cross-origin",
  "content-security-policy":
    "frame-ancestors 'self' https://glockyco.com https://*.glockyco.com http://127.0.0.1:*",
  "permissions-policy": "camera=(), geolocation=(), microphone=()",
};
export const requiredFiles = [
  "index.html",
  "404.html",
  "items/index.html",
  "known-issues/index.html",
  "about/index.html",
  "build-info.json",
  "_headers",
  "brand/emblem-48.webp",
  "brand/emblem-96.webp",
  "favicon-16x16.png",
  "favicon-32x32.png",
  "favicon-192x192.png",
  "favicon-512x512.png",
  "apple-touch-icon.png",
  "og-default.png",
  "sitemap.xml",
  "robots.txt",
  "pagefind/pagefind.js",
];

export async function assertDeployment(directory, repo, limits) {
  const build = JSON.parse(
    await readFile(join(directory, "build-info.json"), "utf8"),
  );
  assertProductionBuild(build, repo);
  for (const file of requiredFiles) {
    const info = await stat(join(directory, file)).catch(() => null);
    if (!info?.isFile() || !info.size)
      throw new Error("Deployment is missing " + file);
  }
  const report = await assetGate(directory, limits);
  const graph = await publicationGate(directory);
  const item = (
    await readdir(join(directory, "items"), { withFileTypes: true })
  )
    .filter((entry) => entry.isDirectory())
    .sort((a, b) => a.name.localeCompare(b.name))[0];
  if (!item) throw new Error("Deployment has no item page to probe.");
  const pages = [];
  for (const [path, file] of [
    ["/", "index.html"],
    ["/items/", "items/index.html"],
    ["/items/" + item.name + "/", "items/" + item.name + "/index.html"],
    ["/known-issues/", "known-issues/index.html"],
    ["/about/", "about/index.html"],
  ]) {
    const html = await readFile(join(directory, file), "utf8");
    const title = html.match(/<title>([^<]+)<\/title>/)?.[0];
    const heading = html.match(/<h1(?:\s[^>]*)?>[\s\S]*?<\/h1>/)?.[0];
    if (!title || !heading)
      throw new Error(file + " has no title or main heading.");
    pages.push({ path, title, heading });
  }
  return { build, pages, report, graph };
}

export async function smokeDeployment(
  origin,
  expected,
  { timeout = 120000, interval = 5000 } = {},
) {
  const deadline = Date.now() + timeout;
  let lastError;
  do {
    try {
      await smokeOnce(origin, expected, deadline);
      return;
    } catch (error) {
      lastError = error;
      if (Date.now() >= deadline) break;
      await new Promise((done) =>
        setTimeout(done, Math.min(interval, deadline - Date.now())),
      );
    }
  } while (Date.now() < deadline);
  throw lastError;
}

async function smokeOnce(origin, expected, deadline) {
  async function fresh(path, status = 200) {
    const url = new URL(path, origin);
    url.searchParams.set("smoke", Date.now() + "-" + Math.random());
    const response = await fetch(url, {
      headers: { "cache-control": "no-cache" },
      signal: AbortSignal.timeout(
        Math.max(1, Math.min(10000, deadline - Date.now())),
      ),
    });
    if (response.status !== status)
      throw new Error(
        path + " returned " + response.status + ", expected " + status,
      );
    for (const [name, value] of Object.entries(securityHeaders)) {
      if (response.headers.get(name) !== value)
        throw new Error(path + " has incorrect " + name);
    }
    return response;
  }
  const actual = await (await fresh("/build-info.json")).json();
  if (!isDeepStrictEqual(actual, expected.build))
    throw new Error("Origin still serves a different build-info.json.");
  for (const page of expected.pages) {
    const html = await (await fresh(page.path)).text();
    if (!html.includes(page.title) || !html.includes(page.heading))
      throw new Error(page.path + " has unexpected content.");
  }
  const missing = await fresh(
    "/missing-deployment-probe-" + expected.build.commit,
    404,
  );
  if (!(await missing.text()).includes("Page not found"))
    throw new Error("Missing route lacks the custom 404 page.");
  const search = await fresh("/pagefind/pagefind.js");
  if (
    !(search.headers.get("content-type") ?? "").includes("javascript") ||
    !(await search.text()).trim()
  ) {
    throw new Error("Pagefind JavaScript is unavailable.");
  }
}

async function main() {
  const [mode, option] = process.argv.slice(2);
  if (
    !["preview", "production", "smoke"].includes(mode) ||
    (mode !== "smoke" && option && option !== "--dry-run") ||
    process.argv.length > 4 ||
    (mode === "smoke" && !option)
  ) {
    throw new Error(
      "Usage: deploy.mjs preview|production [--dry-run], or deploy.mjs smoke <origin>",
    );
  }
  const root = resolve(import.meta.dirname, "..");
  const site = join(root, "site");
  const expected = await assertDeployment(
    join(site, ".stage/production"),
    root,
  );
  console.log(expected.report.report);
  console.log(expected.graph.report);
  if (mode === "smoke") {
    await smokeDeployment(option, expected);
    console.log("Deployment smoke passed at " + option);
    return;
  }
  const args =
    mode === "preview"
      ? ["versions", "upload", "--preview-alias", "staging"]
      : ["deploy"];
  if (option) args.push(option);
  // Capture Wrangler's Version URL rather than assuming an account subdomain.
  const result = spawnSync("bun", ["run", "wrangler", ...args], {
    cwd: site,
    encoding: "utf8",
    env: process.env,
  });
  process.stdout.write(result.stdout ?? "");
  process.stderr.write(result.stderr ?? "");
  if (result.error) throw result.error;
  if (result.status !== 0)
    throw new Error("Wrangler failed with exit " + result.status);
  if (option === "--dry-run") return;
  const origin =
    mode === "production"
      ? "https://bloodline.compendiums.org"
      : result.stdout.match(
          /https:\/\/staging-[a-z0-9.-]+\.workers\.dev\b/i,
        )?.[0];
  if (!origin)
    throw new Error(
      "Wrangler did not report a staging Version URL. Inspect the upload before publishing.",
    );
  await smokeDeployment(origin, expected);
  console.log("Deployment smoke passed at " + origin);
}
if (process.argv[1] === fileURLToPath(import.meta.url)) await main();

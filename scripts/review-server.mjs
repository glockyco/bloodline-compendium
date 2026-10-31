// Serves a built site for local review as Cloudflare serves its assets: a directory address without its slash
// redirects to it, a directory serves its index.html, and a missing address gets 404.html with status 404.
// `wrangler dev` opens every asset at once, so on macOS it fails to start its build helper once a build holds
// about 10,000 files, as the real-data build does. Response headers from `_headers` are checked on a preview
// deployment instead.
// Usage: bun scripts/review-server.mjs [directory] [port]
import { stat } from "node:fs/promises";
import { join, normalize, resolve } from "node:path";

const root = resolve(process.argv[2] ?? "site/build");
const port = Number(process.argv[3] ?? 8787);

async function isFile(path) {
  return (await stat(path).catch(() => null))?.isFile() ?? false;
}

const server = Bun.serve({
  port,
  hostname: "127.0.0.1",
  async fetch(request) {
    const url = new URL(request.url);
    const path = normalize(decodeURIComponent(url.pathname));
    if (path.includes(".."))
      return new Response("Bad request", { status: 400 });
    const target = join(root, path);
    if (path.endsWith("/")) {
      if (await isFile(join(target, "index.html")))
        return new Response(Bun.file(join(target, "index.html")));
    } else if (await isFile(target)) {
      return new Response(Bun.file(target));
    } else if (await isFile(join(target, "index.html"))) {
      return Response.redirect(url.pathname + "/" + url.search, 307);
    }
    return new Response(Bun.file(join(root, "404.html")), { status: 404 });
  },
});
console.log(`Reviewing ${root} at http://${server.hostname}:${server.port}/`);

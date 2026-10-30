import { readFile, readdir, stat } from "node:fs/promises";
import { join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { parse } from "parse5";
import { entityGraphGate } from "./entity-graph-gate.mjs";

const origin = "https://the-bloodline.compendiums.org";

function walk(node, visit) {
  visit(node);
  for (const child of node.childNodes ?? []) walk(child, visit);
}

function text(node) {
  if (node.nodeName === "#text") return node.value;
  return (node.childNodes ?? []).map(text).join("");
}

function normalized(value) {
  return value.normalize("NFKC").replace(/\s+/g, " ").trim().toLowerCase();
}

function pagePath(file) {
  return file === "index.html" ? "/" : "/" + file.replace(/index\.html$/, "");
}

export async function publicationGate(directory) {
  const files = new Set();
  async function collect(relative = "") {
    for (const entry of await readdir(join(directory, relative), {
      withFileTypes: true,
    })) {
      const file = relative ? relative + "/" + entry.name : entry.name;
      if (entry.isDirectory()) await collect(file);
      else if (entry.isFile()) files.add(file);
    }
  }
  await collect();
  const pages = new Map();
  const metadata = { title: new Map(), description: new Map() };
  for (const file of [...files].sort()) {
    if (!file.endsWith(".html")) continue;
    const page = {
      file,
      ids: new Set(),
      references: [],
      relations: new Set(),
      noindex: false,
    };
    const titles = [];
    const descriptions = [];
    walk(parse(await readFile(join(directory, file), "utf8")), (node) => {
      const attrs = Object.fromEntries(
        (node.attrs ?? []).map(({ name, value }) => [name, value]),
      );
      if (attrs.id) page.ids.add(attrs.id);
      if (
        attrs["data-relation-id"] &&
        !["script", "style", "template"].includes(node.tagName)
      )
        page.relations.add(
          JSON.stringify([
            attrs["data-relation-id"],
            attrs["data-relation-direction"],
          ]),
        );
      if (node.tagName === "title") titles.push(text(node));
      if (
        node.tagName === "meta" &&
        attrs.name?.toLowerCase() === "description"
      )
        descriptions.push(attrs.content ?? "");
      if (
        node.tagName === "meta" &&
        attrs.name?.toLowerCase() === "robots" &&
        /noindex/i.test(attrs.content ?? "")
      )
        page.noindex = true;
      for (const attribute of ["href", "src"])
        if (attrs[attribute]) page.references.push(attrs[attribute]);
      if (attrs.srcset && !attrs.srcset.startsWith("data:"))
        for (const candidate of attrs.srcset.split(","))
          page.references.push(candidate.trim().split(/\s+/)[0]);
      if (
        node.tagName === "meta" &&
        (attrs.property === "og:image" || attrs.name === "twitter:image") &&
        attrs.content
      )
        page.references.push(attrs.content);
    });
    for (const [name, values] of [
      ["title", titles],
      ["description", descriptions],
    ]) {
      if (values.length !== 1 || !normalized(values[0]))
        throw new Error(file + " must have one nonempty " + name + ".");
      const value = normalized(values[0]);
      const previous = metadata[name].get(value);
      if (previous)
        throw new Error(
          "Duplicate " + name + " in " + previous + " and " + file,
        );
      metadata[name].set(value, file);
    }
    pages.set(pagePath(file), page);
  }
  const kinds = JSON.parse(
    await readFile(join(directory, "kinds.json"), "utf8"),
  );
  for (const path of [
    "/",
    ...kinds
      .filter((kind) => kind.hasPages)
      .map((kind) => "/" + kind.route.replace(/^\/+|\/+$/g, "") + "/"),
  ])
    if (!pages.has(path)) throw new Error("Publication is missing " + path);
  for (const kind of kinds.filter((kind) => kind.hasPages)) {
    const prefix = "/" + kind.route.replace(/^\/+|\/+$/g, "") + "/";
    if (
      ![...pages.keys()].some(
        (path) => path.startsWith(prefix) && path !== prefix,
      )
    )
      throw new Error("Publication has no pages for " + kind.kind);
  }

  const checkedAssets = new Set();
  let references = 0;
  async function resolveReference(reference, from, localOnly = false) {
    const url = new URL(reference, new URL(from, origin));
    if (url.origin !== origin) {
      if (localOnly)
        throw new Error(
          "Sitemap entry has a non-production origin: " + reference,
        );
      return;
    }
    const path = decodeURIComponent(url.pathname);
    let target = pages.get(path);
    let file = target?.file ?? path.slice(1);
    if (!files.has(file) && !path.endsWith("/")) {
      target = pages.get(path + "/") ?? pages.get(path + ".html");
      if (target) file = target.file;
    }
    if (!files.has(file))
      throw new Error(from + " references missing target " + reference);
    if (
      target &&
      url.hash &&
      !target.ids.has(decodeURIComponent(url.hash.slice(1)))
    )
      throw new Error(from + " references missing fragment " + reference);
    if (!checkedAssets.has(file)) {
      if (!(await stat(join(directory, file))).size)
        throw new Error(from + " references empty target " + reference);
      checkedAssets.add(file);
    }
    references++;
    return { path, target, url };
  }
  for (const [path, page] of pages)
    for (const reference of page.references)
      await resolveReference(reference, path);

  const locations = [];
  walk(
    parse(await readFile(join(directory, "sitemap.xml"), "utf8")),
    (node) => {
      if (node.tagName === "loc") locations.push(text(node).trim());
    },
  );
  const sitemapPaths = new Set();
  for (const location of locations) {
    const result = await resolveReference(location, "/sitemap.xml", true);
    if (
      !location.startsWith(origin + "/") ||
      !result.target ||
      result.url.search ||
      result.url.hash ||
      result.target.noindex
    )
      throw new Error("Invalid sitemap page: " + location);
    if (sitemapPaths.has(result.path))
      throw new Error("Duplicate sitemap page: " + location);
    sitemapPaths.add(result.path);
  }
  // Pages marked noindex, such as the 404 page and the presentation fixtures, stay out of the sitemap.
  for (const [path, page] of pages)
    if (!page.noindex && !sitemapPaths.has(path))
      throw new Error("Sitemap is missing " + path);
  const relations = await entityGraphGate(directory, pages);
  return {
    pages: pages.size,
    references,
    relations,
    sitemapEntries: sitemapPaths.size,
    report:
      "Publication graph: " +
      pages.size +
      " pages, " +
      references +
      " local references, " +
      sitemapPaths.size +
      " sitemap entries.",
  };
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  const directory = resolve(process.argv[2] ?? "site/build");
  console.log((await publicationGate(directory)).report);
}

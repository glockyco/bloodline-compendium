import { readFile } from "node:fs/promises";
import { join } from "node:path";

export async function entityGraphGate(directory, pages) {
  const kinds = JSON.parse(
    await readFile(join(directory, "kinds.json"), "utf8"),
  );
  const graph = JSON.parse(
    await readFile(join(directory, "graph.json"), "utf8"),
  );
  const registry = new Map();
  for (const kind of kinds) {
    if (registry.has(kind.kind))
      throw new Error("Duplicate entity kind " + kind.kind);
    if (kind.hasPages !== Boolean(kind.route))
      throw new Error("Invalid route decision for " + kind.kind);
    registry.set(kind.kind, kind);
  }
  const identity = (entity) => JSON.stringify([entity.kind, entity.key]);
  const documents = new Map();
  const slugs = new Set();
  function reference(entity) {
    const kind = registry.get(entity.kind);
    if (!kind) throw new Error("Unregistered entity kind " + entity.kind);
    if (!entity.key) {
      if (entity.slug)
        throw new Error("Unresolved reference has a slug: " + entity.name);
      return;
    }
    if (kind.hasPages !== Boolean(entity.slug))
      throw new Error("Invalid slug decision for " + entity.name);
    if (!documents.has(identity(entity)))
      throw new Error("Missing reference key " + entity.key);
    const canonical = documents.get(identity(entity)).entity;
    if (canonical.slug !== entity.slug)
      throw new Error("Reference slug differs for " + entity.key);
  }
  for (const document of graph.entities) {
    const entity = document.entity;
    if (!entity.key)
      throw new Error("Entity document is missing a key: " + entity.name);
    const key = identity(entity);
    if (documents.has(key))
      throw new Error("Duplicate entity key " + entity.key);
    documents.set(key, document);
    if (entity.slug) {
      const slug = JSON.stringify([entity.kind, entity.slug]);
      if (slugs.has(slug))
        throw new Error("Duplicate entity slug " + entity.slug);
      slugs.add(slug);
    }
  }
  const rows = new Map();
  for (const row of graph.relations) {
    if (!row.id || rows.has(row.id))
      throw new Error("Duplicate or empty relation ID " + row.id);
    rows.set(row.id, row);
    reference(row.source);
    reference(row.target);
    for (const condition of row.conditions)
      if (condition.entity) reference(condition.entity);
    if (row.attributes) {
      reference(row.attributes.entity);
      for (const place of row.attributes.places) reference(place);
      for (const input of row.attributes.inputs) reference(input.item);
      if (row.attributes.price?.currency)
        reference(row.attributes.price.currency);
    }
    for (const [endpoint, direction, other] of [
      [row.source, "outgoing", row.target],
      [row.target, "incoming", row.source],
    ]) {
      if (!endpoint.key) continue;
      const matches = documents
        .get(identity(endpoint))
        .relations.filter(
          (relation) =>
            relation.id === row.id && relation.direction === direction,
        );
      if (
        matches.length !== 1 ||
        identity(matches[0].entity) !== identity(other)
      )
        throw new Error(
          "Missing or invalid " + direction + " relation " + row.id,
        );
    }
  }
  for (const document of graph.entities) {
    const entity = document.entity;
    reference(entity);
    const kind = registry.get(entity.kind);
    const path = kind.hasPages
      ? "/" +
        kind.route.replace(/^\/+|\/+$/g, "") +
        "/" +
        encodeURIComponent(entity.slug) +
        "/"
      : null;
    const page = path ? pages.get(path) : null;
    if (path && !page) throw new Error("Missing entity page " + path);
    for (const relation of document.relations) {
      reference(relation.entity);
      const row = rows.get(relation.id);
      if (!row) throw new Error("Missing relation row " + relation.id);
      const endpoint =
        relation.direction === "outgoing" ? row.source : row.target;
      if (identity(endpoint) !== identity(entity))
        throw new Error("Invalid relation endpoint " + relation.id);
      for (const condition of relation.conditions)
        if (condition.entity) reference(condition.entity);
      if (
        page &&
        !page.relations.has(JSON.stringify([relation.id, relation.direction]))
      )
        throw new Error(
          path +
            " is missing static relation " +
            relation.id +
            " (" +
            relation.direction +
            ")",
        );
    }
  }
  return graph.relations.length;
}

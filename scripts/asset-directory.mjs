import { join, resolve } from "node:path";

/**
 * Icons, art and brand images are generated from site data, so fixture builds and game builds each get their
 * own directory. A fixture build then never replaces the images a development server shows for game data.
 *
 * @param {string} site The site package directory.
 * @param {string | undefined} dataDirectory The site data the images come from.
 * @returns {string}
 */
export function assetDirectory(site, dataDirectory) {
  const fixtures = resolve(site, "fixtures/site-data");
  return join(
    site,
    ".generated",
    resolve(dataDirectory ?? "") === fixtures ? "fixtures" : "game",
  );
}

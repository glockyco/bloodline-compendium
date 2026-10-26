import { existsSync } from "node:fs";
import { mkdir, writeFile } from "node:fs/promises";
import { join } from "node:path";

// The home page opens with the key art from The Bloodline's Steam page. It is the publisher's art, so
// it stays out of Git like the game's own files: game-data builds fetch it once into an ignored cache.
const heroUrl =
  "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1159290/library_hero_2x.jpg";

export async function storeHeroArt(cache) {
  const file = join(cache, "library_hero_2x.jpg");
  if (existsSync(file)) return file;
  const response = await fetch(heroUrl);
  if (!response.ok)
    throw new Error(
      `Steam returned ${response.status} for ${heroUrl}. The home page art cannot be built.`,
    );
  await mkdir(cache, { recursive: true });
  await writeFile(file, Buffer.from(await response.arrayBuffer()));
  return file;
}

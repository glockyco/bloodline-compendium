-- observation only: reads the live dungeon and open-world populators of the loaded level and changes nothing.
-- Lists each dungeon populator's ore and tree tables with their spawn chances, and each open-world populator's
-- resource counts, as the game holds them after Begin Play, so a run can compare them with the level records.
-- Open the level first with placed-pickups.lua's `open`, and wait for it to load.
local function readMap(map)
  local out = {}
  map:ForEach(function(key, value)
    local k = key:get()
    out[#out + 1] = { key = type(k) == "number" and k or tostring(k), weight = value:get() }
  end)
  return out
end

local dungeons = {}
for _, populator in ipairs(liveAll("RepeatableDungeonPopulator_C")) do
  local entry = { name = populator:GetFName():ToString() }
  pcall(function() entry.ores = readMap(populator["Ore Chances"]) end)
  pcall(function() entry.trees = readMap(populator["Tree Chances"]) end)
  pcall(function() entry.outcropChance = populator["Outcrop Spawn Chances"] end)
  pcall(function() entry.treeChance = populator["Tree Spawn Chances"] end)
  dungeons[#dungeons + 1] = entry
end

local worlds = {}
for _, populator in ipairs(liveAll("OpenWorld_Populator_C")) do
  local entry = { name = populator:GetFName():ToString() }
  for _, field in ipairs({ "Tree (Pine) Count", "Ore (Iron) Count", "Ore (Copper) Count", "Ore (Stone) Count", "Berry Bush Count", "Mushroom Count", "Spawn Berry Bushes?", "Spawn Mushrooms?" }) do
    pcall(function() entry[field] = populator[field] end)
  end
  worlds[#worlds + 1] = entry
end

return { dungeons = dungeons, worlds = worlds }

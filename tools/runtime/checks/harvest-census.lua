-- observation only: reads every live harvestable and spawner box in the loaded level and changes nothing. Lists the
-- level's name, and for each harvestable its actor name, whether it uses a custom preset, the preset it applied,
-- whether a table drew it, its preset enum, the loot table it gives from, its location and whether loading restored it
-- from the saved harvestables, so the catalog can compare the level's own harvestables in play, placed or spawned,
-- with what it says the level gives. For each spawner box lists the table it holds once Begin Play has run.
-- tools/runtime/harvest-sweep.mjs reads it for every level. To read one level by hand, read `{"saved": true}`, open
-- the level with placed-pickups.lua's `open`, wait for it to load, then read with `{"restoring": <the saved spots>}`.
local function className(object)
  local ok, name = pcall(function() return object:GetFullName() end)
  return ok and name and (name:gsub("^%S+ ", "")) or nil
end

local level = UEHelpers.GetWorld():GetFName():ToString()

-- Loading a level spawns the game instance's Saved Harvestables again at their saved spots, whichever level loads,
-- so a harvestable at such a spot with that preset is restored rather than the level's own.
-- Loading saves again, so the sweep reads the saved spots with args.saved before it opens the level and passes them in
-- as args.restoring.
local saved = {}
live("BP_PersistantGameInstance_C")["Saved Harvestables"]:ForEach(function(key, value)
  local spot = key:get()
  saved[#saved + 1] = { x = spot.X, y = spot.Y, preset = value:get():ToString() }
end)
if args.saved then return { saved = saved } end
for _, spot in ipairs(args.restoring or {}) do saved[#saved + 1] = spot end
local function restored(entry)
  for _, spot in ipairs(saved) do
    if spot.preset == entry.preset and math.abs(spot.x - entry.x) < 1 and math.abs(spot.y - entry.y) < 1 then
      return true
    end
  end
  return false
end

local harvestables = {}
for _, actor in ipairs(liveAll("HarvestableInteractionObject_C")) do
  local entry = { name = actor:GetFName():ToString(), loot = {} }
  pcall(function() entry.custom = actor["Custom Preset"] end)
  pcall(function() entry.preset = actor["Custom Preset Name"]:ToString() end)
  pcall(function() entry.drawn = actor["Table Based Preset?"] end)
  pcall(function() entry.enum = actor["Preset"] end)
  pcall(function()
    actor["Selected Loot Table"]:ForEach(function(key, value)
      entry.loot[#entry.loot + 1] = { item = className(key:get()), weight = value:get() }
    end)
  end)
  pcall(function()
    local location = actor:K2_GetActorLocation()
    entry.x, entry.y, entry.z = math.floor(location.X), math.floor(location.Y), math.floor(location.Z)
  end)
  if entry.x and entry.preset then entry.restored = restored(entry) end
  harvestables[#harvestables + 1] = entry
end

local boxes = {}
for _, box in ipairs(liveAll("Harvestable_Item_Spawner_Box_C")) do
  local entry = { name = box:GetFName():ToString(), table = {} }
  pcall(function() entry.mushrooms = box["General Mushroom Spawn Rates"] end)
  pcall(function() entry.ores = box["General Ore Spawn Rates"] end)
  pcall(function() entry.amount = box["Spawn Amount"] end)
  pcall(function()
    box["Custom Preset Table"]:ForEach(function(key, value)
      entry.table[#entry.table + 1] = { preset = key:get():ToString(), weight = value:get() }
    end)
  end)
  boxes[#boxes + 1] = entry
end

return { level = level, harvestables = harvestables, boxes = boxes }

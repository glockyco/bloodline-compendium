-- lab setup: calls the loaded dungeon populator's own Spawn Mining Outcrops, Spawn Trees and Choose Outcrop, which a
-- player cannot do. Calls each spawn function args.times times and lists the harvestables that appear, with the preset
-- each applied and its loot, then counts the ore type Choose Outcrop returns in args.draws calls, so a run can say
-- which presets the populator's outcrops and trees take. Open the level first with placed-pickups.lua's `open`.
local populator = liveAll("RepeatableDungeonPopulator_C")[1]
if not populator then error("No dungeon populator is loaded") end

local function className(object)
  local ok, name = pcall(function() return object:GetFullName() end)
  return ok and name and (name:gsub("^%S+ ", "")) or nil
end

local before = {}
for _, actor in ipairs(liveAll("HarvestableInteractionObject_C")) do before[actor:GetFName():ToString()] = true end

local spawned = {}
for _, fn in ipairs({ "Spawn Mining Outcrops", "Spawn Trees" }) do
  for _ = 1, args.times do populator[fn](populator) end
  for _, actor in ipairs(liveAll("HarvestableInteractionObject_C")) do
    local name = actor:GetFName():ToString()
    if not before[name] then
      before[name] = true
      local entry = { by = fn, loot = {} }
      pcall(function() entry.custom = actor["Custom Preset"] end)
      pcall(function() entry.preset = actor["Custom Preset Name"]:ToString() end)
      pcall(function() entry.enum = actor["Preset"] end)
      pcall(function()
        actor["Selected Loot Table"]:ForEach(function(key, value)
          entry.loot[#entry.loot + 1] = { item = className(key:get()), weight = value:get() }
        end)
      end)
      spawned[#spawned + 1] = entry
    end
  end
end

local draws = {}
for _ = 1, args.draws do
  local out = {}
  populator["Choose Outcrop"](populator, out)
  local key = tostring(out["Ore Type"])
  draws[key] = (draws[key] or 0) + 1
end

return { spawned = spawned, draws = draws }

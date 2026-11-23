-- observation only: reads one map property of every live actor of a class in the loaded level and changes nothing.
-- args.class names the class, such as "CustomEnemySpawner_C", and args.property the map, such as "Spawns". Lists each
-- actor's name and the map's keys and values as the game holds them, so a run can compare them with the level
-- records. Open the level first with placed-pickups.lua's `open`, and wait for it to load.
local function show(value)
  if type(value) ~= "userdata" then return value end
  local ok, text = pcall(function() return value:ToString() end)
  if ok and text then return text end
  ok, text = pcall(function() return value:GetFullName() end)
  if ok and text then return (text:gsub("^%S+ ", "")) end
  return tostring(value)
end

local out = {}
for _, actor in ipairs(liveAll(args.class)) do
  local entry = { name = actor:GetFName():ToString(), entries = {} }
  pcall(function()
    actor[args.property]:ForEach(function(key, value)
      entry.entries[#entry.entries + 1] = { key = show(key:get()), value = show(value:get()) }
    end)
  end)
  out[#out + 1] = entry
end
return out

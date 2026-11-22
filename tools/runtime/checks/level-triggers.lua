-- Lists where the loaded level's trigger volumes stand, by actor name, with the nearest place name popups, so a run
-- can say where a level event that a volume starts happens. args.names narrows to actor names that contain one of
-- the given strings. Open the level first with placed-pickups.lua's args.open.
local classes = { "TriggerBox", "TriggerSphere", "TriggerCapsule", "MinorLocationPopupVolume_C", "LocationPopupVolume_C" }
local function wanted(name)
  if not args.names then return true end
  for _, needle in ipairs(args.names) do
    if name:find(needle, 1, true) then return true end
  end
  return false
end
local function place(actor)
  local name = nil
  for _, field in ipairs({ "Location Name", "LocationName", "Name of Location", "Popup Text" }) do
    pcall(function()
      local value = actor[field]
      if value ~= nil and name == nil then name = value:ToString() end
    end)
  end
  return name
end
local triggers, places = {}, {}
for _, class in ipairs(classes) do
  for _, actor in ipairs(liveAll(class)) do
    local location = actor:K2_GetActorLocation()
    local entry = {
      name = actor:GetFName():ToString(),
      class = actor:GetClass():GetFName():ToString(),
      at = { x = math.floor(location.X), y = math.floor(location.Y), z = math.floor(location.Z) },
    }
    if class:find("Popup", 1, true) then
      entry.place = place(actor)
      places[#places + 1] = entry
    elseif wanted(entry.name) then
      triggers[#triggers + 1] = entry
    end
  end
end
for _, trigger in ipairs(triggers) do
  local best, distance = nil, math.huge
  for _, popup in ipairs(places) do
    local dx, dy, dz = popup.at.x - trigger.at.x, popup.at.y - trigger.at.y, popup.at.z - trigger.at.z
    local d = math.sqrt(dx * dx + dy * dy + dz * dz)
    if d < distance then best, distance = popup, d end
  end
  trigger.nearest = best and { name = best.name, place = best.place, distance = math.floor(distance) } or nil
end
return { triggers = triggers, places = #places }

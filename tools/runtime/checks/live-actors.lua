-- observation only: counts the live actors of each class in args.classes and names the level each stands in, so a
-- run can tell whether a loaded level holds actors that its record does not list.
-- Open the level first with placed-pickups.lua's `open`, and wait for it to load.
local found = {}
for _, class in ipairs(args.classes) do
    local levels = {}
    local count = 0
    for _, actor in ipairs(liveAll(class)) do
        count = count + 1
        local ok, level = pcall(function() return actor:GetOuter():GetOuter():GetFName():ToString() end)
        local name = ok and level or 'unknown'
        levels[name] = (levels[name] or 0) + 1
    end
    found[class] = { count = count, levels = levels }
end
return found

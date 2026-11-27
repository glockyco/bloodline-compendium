-- observation only: reads the theme and the enemy table of each live dungeon populator of the loaded level, as the
-- game holds them after Begin Play, so a run can tell whether a level's theme is fixed or drawn anew.
-- Open the level first with placed-pickups.lua's `open`, and wait for it to load.
local found = {}
for _, populator in ipairs(liveAll('RepeatableDungeonPopulator_C')) do
    local entry = { name = populator:GetFName():ToString(), enemies = {} }
    pcall(function() entry.theme = populator.Theme end)
    pcall(function() entry.spawns = populator['Spawn Roaming Enemies?'] end)
    pcall(function()
        populator.Enemies:ForEach(function(key, value)
            entry.enemies[#entry.enemies + 1] = key:get():GetFName():ToString()
        end)
    end)
    found[#found + 1] = entry
end
return { populators = found }

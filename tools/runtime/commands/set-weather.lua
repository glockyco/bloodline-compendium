-- lab setup: changes the weather at once, which a player can only wait for
-- Sets the weather through Ultra Dynamic Weather's own setter and holds it for args.hold seconds. args.type is the
-- weather type, where 0 is clear skies.
local weather = live("Ultra_Dynamic_Weather_C")
if not weather then error("The level has no Ultra Dynamic Weather") end
weather["Change Weather Using Type"](weather, args.type, 0.0, args.hold or 3600.0, 0.0, true)
local current = {}
weather["Get Current Weather Type"](weather, current)
return { weatherType = current["Current Weather Type"] }

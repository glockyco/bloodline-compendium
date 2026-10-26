-- lab setup: moves the clock to a chosen hour, which a player can only wait for
-- Sets the time of day through Ultra Dynamic Sky's own setter, so the sun, sky light and fog follow as at that hour.
-- args: hours, minutes.
local sky = live("Ultra_Dynamic_Sky_C")
if not sky then error("The level has no Ultra Dynamic Sky") end
sky["Set Time of Day using Time Code"](sky, { Hours = args.hours, Minutes = args.minutes or 0, Seconds = 0, Frames = 0, bDropFrameFormat = false })
return { timeOfDay = sky["Time of Day"] }

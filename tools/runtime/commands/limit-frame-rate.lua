-- lab setup: holds the engine's frame rate limit, which the game resets when a level loads
-- Checks the limit every second for the rest of the session, so the lab game uses no more of the machine than
-- the checks need.
local limit = args.fps or 60
console("t.MaxFPS " .. limit)
LoopInGameThreadWithDelay(1000, function()
  if system:GetConsoleVariableFloatValue("t.MaxFPS") ~= limit then console("t.MaxFPS " .. limit) end
end)
return { fps = limit }

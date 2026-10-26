-- Samples player values every frame for args.seconds of game time and runs commands at set times, so a change
-- can be measured against the frames before it. Replies when sampling ends.
-- Example: {"seconds": 30, "values": ["Health"], "actions": [{"at": 5, "command": "use", "args": {...}}]}
local finish = defer()
if not EngineTickAvailable then error("UE4SS has no engine tick hook, so frames cannot be sampled") end
local pawn = requirePlayer()
local world = UEHelpers.GetWorld()
local start = statics:GetTimeSeconds(world)
local rows, done, handle = {}, {}, nil
handle = LoopInGameThreadAfterFrames(1, function()
  local ok, problem = pcall(function()
    local t = statics:GetTimeSeconds(world) - start
    for index, action in ipairs(args.actions or {}) do
      if done[index] == nil and t >= action.at then
        done[index] = { at = t, command = action.command, result = commands[action.command](action.args or {}) }
      end
    end
    local row = { t = t }
    for _, path in ipairs(args.values) do row[path] = getPath(pawn, path) end
    rows[#rows + 1] = row
    if t >= args.seconds then
      CancelDelayedAction(handle)
      finish({ rows = rows, actions = done })
    end
  end)
  if not ok then
    CancelDelayedAction(handle)
    finish(nil, problem)
  end
end)

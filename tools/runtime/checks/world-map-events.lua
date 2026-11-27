-- Samples the world map once per game second for args.seconds (at least 90).
-- Open /Game/Eudros/Overworld_Eudros_Improved with placed-pickups.lua's open and verify the pawn with probe.
-- Pass {"seconds": 90} to sample while standing still.
-- lab setup: with args.move, feeds movement input to the pawn to walk a small loop without a keyboard.
-- Each row names the live events and records their terrain, chosen duration, remaining timeline time and distance.
local world = UEHelpers.GetWorld()
local pawn = requirePlayer()
assert(pawn:GetClass():GetFName():ToString() == 'TopDownCharacter_Working_C', 'Not on the world map')
assert(EngineTickAvailable, 'UE4SS has no engine tick hook')
local seconds = assert(args.seconds, 'Pass {"seconds": 90} or a longer duration')
assert(seconds >= 90, 'Sample for at least 90 game seconds')
local start = statics:GetTimeSeconds(world)
local realStart = statics:GetRealTimeSeconds(world)
local startPosition = pawn:K2_GetActorLocation()
local rows, nextSample, highest, handle = {}, 0, 0, nil
local finish = defer()
local function distance(a, b)
  return math.sqrt((a.X - b.X)^2 + (a.Y - b.Y)^2 + (a.Z - b.Z)^2)
end
local function sample(t)
  local position = pawn:K2_GetActorLocation()
  local events = {}
  pawn['Spawned Events']:ForEach(function(_, entry)
    local actor = entry:get()
    if actor:IsValid() then
      local timeline = actor.Countdown
      local rate = timeline:GetPlayRate()
      events[#events + 1] = {
        actor = actor:GetFName():ToString(),
        event = actor['Selected Event']:ToString(),
        terrain = actor['Nearest Location Type']:ToString(),
        chosenSeconds = actor['Chosen Event Countdown Time'],
        remainingSeconds = rate > 0 and (timeline:GetTimelineLength() - timeline:GetPlaybackPosition()) / rate or nil,
        ageSeconds = actor:GetGameTimeSinceCreation(),
        distance = distance(position, actor:K2_GetActorLocation()),
      }
    end
  end)
  table.sort(events, function(a, b) return a.actor < b.actor end)
  highest = math.max(highest, #events)
  return {
    t = t, count = #events, events = events,
    paused = statics:IsGamePaused(world), worldPaused = pawn['World Paused'],
    nearestTerrain = pawn['Nearest Camp Tag']:ToString(),
    moved = distance(startPosition, position),
  }
end
handle = LoopInGameThreadAfterFrames(1, function()
  local ok, problem = pcall(function()
    assert(world:IsValid() and pawn:IsValid(), 'Left the world map during sampling')
    local t = statics:GetTimeSeconds(world) - start
    if args.move then
      local angle = t / 2
      local position = pawn:K2_GetActorLocation()
      local x = startPosition.X + 200 * math.cos(angle) - position.X
      local y = startPosition.Y + 200 * math.sin(angle) - position.Y
      local length = math.sqrt(x*x + y*y)
      if length > 0 then
        pawn:AddMovementInput({ X = x / length, Y = y / length, Z = 0 }, math.min(length / 100, 1), false)
      end
    end
    if t >= nextSample then
      rows[#rows + 1] = sample(t)
      nextSample = nextSample + 1
    end
    if t >= seconds or statics:GetRealTimeSeconds(world) - realStart > seconds + 45 then
      CancelDelayedAction(handle)
      finish({ world = world:GetFullName(), seconds = t, complete = t >= seconds, highest = highest, rows = rows,
        paused = statics:IsGamePaused(world), worldPaused = pawn['World Paused'] })
    end
  end)
  if not ok then
    CancelDelayedAction(handle)
    finish(nil, problem)
  end
end)

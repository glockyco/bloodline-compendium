-- Reports the loaded world, the player pawn, the game clock and which of the given classes have live instances,
-- so the host can wait for a menu or a level and see that the game ticks.
local world = UEHelpers.GetWorld()
local pawn = player()
local present = {}
for _, class in ipairs(args.classes or {}) do present[class] = live(class) ~= nil end
local frame = nil
pcall(function() frame = system:GetFrameCount() end)
return {
  world = world:IsValid() and world:GetFullName() or nil,
  pawn = pawn and pawn:GetClass():GetFName():ToString() or nil,
  time = statics:GetTimeSeconds(world),
  realTime = statics:GetRealTimeSeconds(world),
  frame = frame,
  present = present,
}

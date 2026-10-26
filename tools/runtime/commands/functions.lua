-- Lists the functions and properties of a live object whose names contain a pattern, for finding the game's own
-- function for an action. The class Player names the player pawn.
local target = args.class == "Player" and requirePlayer() or live(args.class, args.needle)
if not target then error("No live " .. args.class) end
local functions, properties = {}, {}
local pattern = (args.pattern or ""):lower()
local class = target:GetClass()
while class and class:IsValid() do
  class:ForEachFunction(function(f)
    local name = f:GetFName():ToString()
    if name:lower():find(pattern, 1, true) then functions[#functions + 1] = name end
  end)
  class:ForEachProperty(function(p)
    local name = p:GetFName():ToString()
    if name:lower():find(pattern, 1, true) then
      properties[#properties + 1] = name .. ":" .. p:GetClass():GetFName():ToString()
    end
  end)
  class = class:GetSuperStruct()
end
return { target = target:GetFullName(), functions = functions, properties = properties }

-- Lists a function's parameters, for calling a game function with the right arguments. The class Player names
-- the player pawn.
local target = args.class == "Player" and requirePlayer() or live(args.class, args.needle)
if not target then error("No live " .. args.class) end
local class = target:GetClass()
while class and class:IsValid() do
  local found
  class:ForEachFunction(function(f)
    if f:GetFName():ToString() == args["function"] then found = f end
  end)
  if found then
    local parameters = {}
    found:ForEachProperty(function(p)
      local name = p:GetFName():ToString()
      if not name:find("^CallFunc_") and not name:find("^Temp_") and not name:find("^K2Node_") then
        parameters[#parameters + 1] = name .. ":" .. p:GetClass():GetFName():ToString()
      end
    end)
    return { ["function"] = args["function"], parameters = parameters }
  end
  class = class:GetSuperStruct()
end
error(args["function"] .. " not found")

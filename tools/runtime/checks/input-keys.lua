-- Lists the keys of the game's Enhanced Input mapping contexts, each with the input action it triggers, so a page can
-- name the key a player presses, such as the one that opens the radial menu with the skill window. args.action
-- narrows to actions whose name contains it.
local found = {}
for _, context in ipairs(FindAllOf("InputMappingContext") or {}) do
  local ok, mappings = pcall(function() return context.Mappings end)
  if ok and mappings then
    mappings:ForEach(function(_, element)
      local mapping = element:get()
      local action = mapping.Action
      local name = action and action:IsValid() and action:GetFName():ToString() or "?"
      if not args.action or name:find(args.action, 1, true) then
        found[#found + 1] = {
          context = context:GetFName():ToString(),
          action = name,
          key = mapping.Key.KeyName:ToString(),
        }
      end
    end)
  end
end
return { count = #found, mappings = found }

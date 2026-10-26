-- lab setup: adds items to the inventory without finding, buying or crafting them
-- Adds items through the player's own AddItemsToInventory with each item class's default item info.
local pawn = requirePlayer()
local results = {}
for _, path in ipairs(args.items) do
  local class = LoadAsset(path)
  if class and class:IsValid() then
    local ok, problem = pcall(function() pawn:AddItemsToInventory(class:GetCDO().ItemInfo, args.count or 1) end)
    results[#results + 1] = { item = path, added = ok, error = ok and nil or tostring(problem) }
  else
    results[#results + 1] = { item = path, added = false, error = "class did not load" }
  end
end
return { results = results, inventory = #pawn.Inventory }

-- lab setup: adds a batch of items to the inventory and removes them again after reading their tooltips
-- Reads the tooltips of many items in one request: gives args.items as `give` does, waits args.frames frames for the
-- character sheet to rebuild its grid, reads every shown slot's tooltip as `read-tooltips` does, and removes the
-- whole inventory through the player's own RemoveItemsFromInventory. The character sheet must be open and the
-- inventory should start empty. The inventory holds 100 items, so batches stay below that.
local finish = defer()
local pawn = requirePlayer()
local given = commands["give"]({ items = args.items })
ExecuteInGameThreadAfterFrames(args.frames or 10, function()
  local ok, result = pcall(function()
    local rows = commands["read-tooltips"]({})
    -- Removal shifts the items behind the removed one, so the first held item is removed until none is left.
    for _ = 1, 2 * #pawn.Inventory do
      if #pawn.Inventory == 0 then break end
      local info = pawn.Inventory[1]
      pawn:RemoveItemsFromInventory(info, itemInfo(info, "Quantity"), {})
    end
    local rejected = {}
    for _, entry in ipairs(given.results) do
      if not entry.added then rejected[#rejected + 1] = entry end
    end
    return { tooltips = rows, rejected = rejected, left = #pawn.Inventory }
  end)
  if ok then finish(result) else finish(nil, result) end
end)

-- lab setup: removes the tooltips that place-tooltips showed and empties the inventory
-- Takes the placed tooltips off the viewport, ends each slot's hover through its own mouse leave handler and removes
-- every held item through the player's own RemoveItemsFromInventory.
local pawn = requirePlayer()
for _, slot in ipairs(inventorySlots()) do
  local tooltip = slot.ToolTipWidget
  if tooltip and tooltip:IsValid() then tooltip:RemoveFromParent() end
  slot:OnMouseLeave({})
end
-- Removal shifts the items behind the removed one, so the first held item is removed until none is left.
for _ = 1, 2 * #pawn.Inventory do
  if #pawn.Inventory == 0 then break end
  local info = pawn.Inventory[1]
  pawn:RemoveItemsFromInventory(info, itemInfo(info, "Quantity"), {})
end
return { left = #pawn.Inventory }

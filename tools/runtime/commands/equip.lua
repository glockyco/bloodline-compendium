-- Equips an item from the inventory as a player does, by clicking its inventory slot, and fails unless the item
-- is equipped afterwards. The character sheet must be open.
local pawn = requirePlayer()
local pressed = press(inventorySlot(args.item), "ItemButton")
for _, entry in ipairs(items(pawn.Equipment)) do
  if entry.item == args.item then return pressed end
end
error(args.item .. " was clicked but is not equipped")

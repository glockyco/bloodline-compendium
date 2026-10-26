-- Uses an item from the inventory as a player does, by clicking its inventory slot, such as eating food. The
-- character sheet must be open. Reports how many of the item the inventory held before and after.
local pawn = requirePlayer()
local function held()
  local count = 0
  for _, entry in ipairs(items(pawn.Inventory)) do
    if entry.item == args.item then count = count + entry.quantity end
  end
  return count
end
local before = held()
local pressed = press(inventorySlot(args.item), "ItemButton")
return { pressed = pressed, before = before, after = held() }

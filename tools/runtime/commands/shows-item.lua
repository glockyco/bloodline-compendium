-- Whether the open character sheet shows an inventory slot for args.item, which it creates some frames after opening.
local ok = pcall(inventorySlot, args.item)
return { shown = ok }

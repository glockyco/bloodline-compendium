-- Opens the character sheet with the inventory, as its key does, so inventory slots and their tooltips exist.
local pawn = requirePlayer()
if not pawn["Is Character Sheet Open"] then pawn["Open/Close Character Sheet"](pawn) end
return { open = pawn["Is Character Sheet Open"] }

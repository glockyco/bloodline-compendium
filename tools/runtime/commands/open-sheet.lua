-- Opens the character sheet with the inventory, as its key does, so inventory slots and their tooltips exist. An
-- open sheet does not show items given since it opened, so args.refresh closes and opens it again, as pressing its
-- key twice does.
local pawn = requirePlayer()
if args.refresh and pawn["Is Character Sheet Open"] then pawn["Open/Close Character Sheet"](pawn) end
if not pawn["Is Character Sheet Open"] then pawn["Open/Close Character Sheet"](pawn) end
return { open = pawn["Is Character Sheet Open"] }

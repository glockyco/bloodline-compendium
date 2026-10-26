-- Reads the player values that checks compare, and the items held and equipped. args.values names other values.
local pawn = requirePlayer()
local values = {}
local names = args.values or {
  "Health", "Max Health", "Health Regen Speed", "ArmorTotal", "WalkSpeed", "RunSpeed", "Stamina", "MaxStamina",
  "Current Mana", "Maximum Mana", "Level", "EXP", "Required EXP",
}
for _, name in ipairs(names) do values[name] = getPath(pawn, name) end
return { values = values, inventory = items(pawn.Inventory), equipment = items(pawn.Equipment) }

-- lab setup: deals damage through the player's own TakeDamage, as an enemy hit does
-- args.amount deals that much, or args.toHealth deals what brings Health down to that value without armor.
-- Reports Health before and after, so the damage that armor let through can be compared.
local pawn = requirePlayer()
local before = pawn.Health
local amount = args.amount or math.max(0, before - args.toHealth)
pawn:TakeDamage(amount)
return { amount = amount, before = before, after = pawn.Health, taken = before - pawn.Health }

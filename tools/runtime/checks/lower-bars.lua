-- lab setup: lowers the player's mana and stamina, so what a food restores shows in both bars. Health is lowered
-- through take-damage, as an enemy hit does.
local p = requirePlayer()
local before = { mana = p['Current Mana'], stamina = p['Stamina'] }
if args.mana then p['Current Mana'] = args.mana end
if args.stamina then p['Stamina'] = args.stamina end
return { before = before, after = { mana = p['Current Mana'], stamina = p['Stamina'] } }

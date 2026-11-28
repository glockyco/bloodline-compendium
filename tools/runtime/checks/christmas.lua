-- lab setup: args.cookies sets the TotalCookiesHeld progress on the game instance instead of collecting cookies,
-- and args.unwrap calls the given present's own Unwrap without walking to it.
-- Lists the live Christmas presents with their gift and required cookies, the live cookies, the cookies held, and
-- the inventory's Christmas items, to compare with the presents and cookies the levels place.
local pawn = requirePlayer()
-- The present reads the cookies from the player's Persistant Ref, as the cookie pickups write them.
local progress = pawn['Persistant Ref']['Achievement Progress']
local function held()
  local found = nil
  progress:ForEach(function(key, value)
    if key:get():ToString() == 'TotalCookiesHeld' then found = value:get() end
  end)
  return found
end
if args.cookies then
  progress:Add(FName('TotalCookiesHeld'), args.cookies)
end
local presents = {}
for index, present in ipairs(liveAll('ChristmasPresent_01_C')) do
  presents[#presents + 1] = {
    index = index,
    gift = present['Gift']:ToString(),
    item = present['Gift Class']:IsValid() and classPath(present['Gift Class']) or nil,
    required = present['Required Cookies'],
  }
  if args.unwrap == index then
    present['Player Ref'] = pawn
    present['Unwrap'](present)
  end
end
local christmas = {}
for _, entry in ipairs(items(pawn.Inventory)) do
  if entry.item:find('Christmas') then christmas[#christmas + 1] = entry end
end
return {
  presents = presents,
  cookies = #liveAll('Cookie_Pickup_C'),
  held = held(),
  inventory = christmas,
}

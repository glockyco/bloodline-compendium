-- lab setup: args.begin calls the station's own Begin Chisel Minigame without walking to it, args.grade calls its own
-- Grant Item with that grade ("S" to "D") instead of chiseling. Give Chiselstone first with `give`.
-- Without arguments, lists the live stations, the fossil each holds with its artifact type and health, and the
-- inventory's chiseling items, so a run can compare what a grade gives with the Chiseling Station rules.
local stations = liveAll('ChiselingStation_C')
assert(#stations > 0, 'No live Chiseling Station; open TheMagesCollege_MainHall first')
local station = stations[1]
local pawn = requirePlayer()
local function inventory()
  local found = {}
  for _, entry in ipairs(items(pawn.Inventory)) do
    if entry.item:find('Fossil') or entry.item:find('Artifact') or entry.item:find('ArmorRune') or entry.item:find('Hollowstone') then
      found[#found + 1] = entry
    end
  end
  return found
end
-- args.destroy calls the station's own Destroy Fossil, as breaking it does, so the next begin takes a new Chiselstone.
if args.destroy then
  station['Destroy Fossil'](station)
end
if args.begin then
  station['Begin Chisel Minigame'](station)
end
-- args.artifact sets the held fossil's Artifact Type before Grant Item, to see what each type gives at a grade.
if args.artifact then
  station['Fossil Ref']['Artifact Type'] = args.artifact
end
if args.grade then
  station['Grant Item'](station, args.grade)
end
local fossil = station['Fossil Ref']
local result = { stations = #stations, inventory = inventory() }
if fossil and fossil:IsValid() then
  result.artifact = fossil['Artifact Type']:ToString()
  result.health = station['Fossil Health']
  result.maxHealth = station['Fossil Max Health']
end
return result

-- lab setup: calls the stable's own Open Stable Window without walking to it, and presses its next-mount button by
-- calling the bound handler, to read the price each mount shows.
-- Lists the live stables with their mounts for sale, and the stable window's mount and price texts.
local stables = liveAll('StableShop_Actor_C')
assert(#stables > 0, 'No live stable; open a level with one first')
local stable = stables[1]
local result = { stables = #stables, mounts = {} }
stable['Mounts for Sale']:ForEach(function(_, entry)
  local class = entry:get()
  result.mounts[#result.mounts + 1] = class:IsValid() and classPath(class) or 'none'
end)
if args.open then
  stable['Open Stable Window'](stable)
end
local window = live('StableShop_C')
if window then
  result.shown = {}
  local count = args.next or 0
  for index = 0, count do
    if index > 0 then
      press(window, 'NextMount')
    end
    result.shown[#result.shown + 1] = texts(window)
  end
end
return result

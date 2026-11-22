-- lab setup: rewrites one loaded harvestable's preset name, which a player cannot do.
-- Gives one loaded harvestable each name of its class's Woodcutting Items, Mining Items and Foraging EXP Items lists
-- as its Custom Preset Name, calls Check Player Level and lists the level the game then requires and the level it
-- checks, for src/Bloodline/Content/gathering-levels.json. args.gate instead calls Check Player Level on the first
-- loaded harvestable of each named preset and returns whether it lets the character harvest.
local harvestables = liveAll("HarvestableInteractionObject_C")
if #harvestables == 0 then error("No harvestable is loaded") end
if args.gate then
  local wanted, out = {}, {}
  for _, name in ipairs(args.gate) do wanted[name] = true end
  for _, actor in ipairs(harvestables) do
    local name = actor["Custom Preset Name"]:ToString()
    if wanted[name] then
      wanted[name] = nil
      out[#out + 1] = {
        preset = name,
        allowed = actor["Check Player Level"](actor),
        level = actor["Required level"],
        checks = actor["Level to Check"]:ToString(),
      }
    end
  end
  return out
end
local actor = harvestables[1]
local out = {}
for _, list in ipairs({ "Woodcutting Items", "Mining Items", "Foraging EXP Items" }) do
  local names = {}
  actor[list]:ForEach(function(_, element) names[#names + 1] = element:get():ToString() end)
  for _, name in ipairs(names) do
    actor["Custom Preset Name"] = name
    actor["Required level"] = 1
    actor["Level to Check"] = ""
    actor["Check Player Level"](actor)
    out[#out + 1] = { list = list, preset = name, level = actor["Required level"], checks = actor["Level to Check"]:ToString() }
  end
end
return out

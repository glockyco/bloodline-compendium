-- lab setup: rewrites one loaded harvestable's preset, which a player cannot do.
-- Gives one loaded harvestable each name of its class's Woodcutting Items, Mining Items and Foraging EXP Items lists
-- as a custom preset, calls NewFunction_0, which its construction script and Begin Play call to apply a preset, and
-- Check Player Level, and lists the loot table, the level the game then requires and the level it checks, for
-- src/Bloodline/Content/harvest-presets.json.
-- args.gate instead calls Check Player Level on the first loaded harvestable of each named preset and returns whether
-- it lets the character harvest.
-- args.draw instead calls Choose Preset args.draw times on the first loaded harvestable whose preset a table draws and
-- counts the names it returns, with the table.
local harvestables = liveAll("HarvestableInteractionObject_C")
if #harvestables == 0 then error("No harvestable is loaded") end

local function className(object)
  return (object:GetFullName():gsub("^%S+ ", ""))
end

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

if args.draw then
  for _, actor in ipairs(harvestables) do
    local entries = {}
    if actor["Table Based Preset?"] then
      actor["Custom Preset Table"]:ForEach(function(key, value)
        entries[#entries + 1] = { preset = key:get():ToString(), weight = value:get() }
      end)
    end
    if #entries >= 2 then
      local counts = {}
      for _ = 1, args.draw do
        local out = {}
        actor["Choose Preset"](actor, out)
        local name = out["Array Element"]:ToString()
        counts[name] = (counts[name] or 0) + 1
      end
      return { table = entries, counts = counts }
    end
  end
  error("No loaded harvestable draws its preset from a table")
end

local actor = harvestables[1]
local out = {}
for _, list in ipairs({ "Woodcutting Items", "Mining Items", "Foraging EXP Items" }) do
  local names = {}
  actor[list]:ForEach(function(_, element) names[#names + 1] = element:get():ToString() end)
  for _, name in ipairs(names) do
    actor["Custom Preset"] = true
    actor["Table Based Preset?"] = false
    actor["Custom Preset Name"] = name
    actor["NewFunction_0"](actor)
    local loot = {}
    actor["Selected Loot Table"]:ForEach(function(key, value)
      loot[#loot + 1] = { item = className(key:get()), weight = value:get() }
    end)
    actor["Required level"] = 1
    actor["Level to Check"] = ""
    actor["Check Player Level"](actor)
    out[#out + 1] = {
      list = list,
      preset = name,
      level = actor["Required level"],
      checks = actor["Level to Check"]:ToString(),
      loot = loot,
    }
  end
end
return out
